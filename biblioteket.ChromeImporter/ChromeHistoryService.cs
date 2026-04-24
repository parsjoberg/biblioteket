using Microsoft.Data.Sqlite;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("biblioteket.External.Services.Test")]

namespace biblioteket.ChromeImporter;

public record LegimusHistoriePost(string Url, string Titel, DateTime SenastBesökt);

public class ChromeHistoryService
{
    private static readonly string ChromeHistorikSökväg = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Google", "Chrome", "User Data", "Default", "History");

    public List<LegimusHistoriePost> HämtaLegimusPoster()
    {
        if (!File.Exists(ChromeHistorikSökväg))
            throw new FileNotFoundException(
                $"Chrome-historikfilen hittades inte:\n{ChromeHistorikSökväg}");

        // Chrome håller databasen låst medan den körs – kopiera till en temporär fil.
        var tempFil = Path.Combine(Path.GetTempPath(), $"chrome_history_{Guid.NewGuid():N}.db");
        var tempWal = tempFil + "-wal";
        var tempShm = tempFil + "-shm";

        try
        {
            File.Copy(ChromeHistorikSökväg, tempFil, overwrite: true);

            var walFil = ChromeHistorikSökväg + "-wal";
            if (File.Exists(walFil))
                File.Copy(walFil, tempWal, overwrite: true);

            return SökLegimusUrls(tempFil);
        }
        finally
        {
            if (File.Exists(tempFil)) File.Delete(tempFil);
            if (File.Exists(tempWal)) File.Delete(tempWal);
            if (File.Exists(tempShm)) File.Delete(tempShm);
        }
    }

    internal static List<LegimusHistoriePost> SökLegimusUrls(string dbSökväg)
    {
        var resultat = new List<LegimusHistoriePost>();
        var connectionString = $"Data Source={dbSökväg};Mode=ReadOnly;";

        using var anslutning = new SqliteConnection(connectionString);
        anslutning.Open();

        using var kommando = anslutning.CreateCommand();
        //kommando.CommandText = @"
        //    SELECT url, title, last_visit_time
        //    FROM urls
        //    WHERE url LIKE '%legimus.se/bok%'
        //    ORDER BY last_visit_time DESC";

        kommando.CommandText = @"
            select tab_url url, target_path title, start_time last_visit_time
from downloads
WHERE tab_url LIKE '%legimus.se/bok%'
ORDER BY start_time DESC";

        using var läsare = kommando.ExecuteReader();
        while (läsare.Read())
        {
            var url           = läsare.GetString(0);
            var titel         = läsare.IsDBNull(1) ? url : läsare.GetString(1);
            var senastBesökt  = läsare.IsDBNull(2)
                ? DateTime.Now
                : ChromeTidTillDateTime(läsare.GetInt64(2));

            resultat.Add(new LegimusHistoriePost(url, titel, senastBesökt));
        }

        SqliteConnection.ClearAllPools();

        return resultat;
    }

    // Chrome lagrar tid som mikrosekunder sedan 1 januari 1601 UTC.
    internal static DateTime ChromeTidTillDateTime(long chromeTid)
    {
        var epok = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return epok.AddMicroseconds(chromeTid).ToLocalTime();
    }
}