using biblioteket.ChromeImporter;
using Microsoft.Data.Sqlite;

namespace biblioteket.External.Services.Test;

public class ChromeHistoryServiceTests : IDisposable
{
    private readonly string _dbSökväg;

    public ChromeHistoryServiceTests()
    {
        _dbSökväg = Path.Combine(Path.GetTempPath(), $"test_chrome_{Guid.NewGuid():N}.db");
        SkapaTestDatabas(_dbSökväg);
    }

    private static void SkapaTestDatabas(string sökväg)
    {
        using var anslutning = new SqliteConnection($"Data Source={sökväg}");
        anslutning.Open();
        using var kommando = anslutning.CreateCommand();
        kommando.CommandText = @"
            CREATE TABLE downloads (
                id          INTEGER PRIMARY KEY,
                tab_url     TEXT,
                target_path TEXT,
                start_time  INTEGER
            )";
        kommando.ExecuteNonQuery();
    }

    private void LäggTillNedladdning(string tabUrl, string? targetPath, long? startTime)
    {
        using var anslutning = new SqliteConnection($"Data Source={_dbSökväg}");
        anslutning.Open();
        using var kommando = anslutning.CreateCommand();
        kommando.CommandText =
            "INSERT INTO downloads (tab_url, target_path, start_time) VALUES ($url, $path, $tid)";
        kommando.Parameters.AddWithValue("$url", tabUrl);
        kommando.Parameters.AddWithValue("$path", (object?)targetPath ?? DBNull.Value);
        kommando.Parameters.AddWithValue("$tid", (object?)startTime ?? DBNull.Value);
        kommando.ExecuteNonQuery();
    }

    [Fact]
    public void SökLegimusUrls_LegimusUrl_ReturnersPost()
    {
        LäggTillNedladdning("https://legimus.se/bok/12345", "En bok.mp3", 13333000000000000L);

        var resultat = ChromeHistoryService.SökLegimusUrls(_dbSökväg);

        Assert.Single(resultat);
        Assert.Equal("https://legimus.se/bok/12345", resultat[0].Url);
    }

    [Fact]
    public void SökLegimusUrls_InteLegimusUrl_FiltreasUt()
    {
        LäggTillNedladdning("https://example.com/bok/12345", "Annat.mp3", 13333000000000000L);

        var resultat = ChromeHistoryService.SökLegimusUrls(_dbSökväg);

        Assert.Empty(resultat);
    }

    [Fact]
    public void SökLegimusUrls_NullTargetPath_AnvänderUrlSomTitel()
    {
        LäggTillNedladdning("https://legimus.se/bok/12345", null, 13333000000000000L);

        var resultat = ChromeHistoryService.SökLegimusUrls(_dbSökväg);

        Assert.Equal("https://legimus.se/bok/12345", resultat[0].Titel);
    }

    [Fact]
    public void SökLegimusUrls_NullStartTime_AnvänderNuSomDatum()
    {
        LäggTillNedladdning("https://legimus.se/bok/12345", "Bok.mp3", null);
        var före = DateTime.Now;

        var resultat = ChromeHistoryService.SökLegimusUrls(_dbSökväg);

        Assert.True(resultat[0].SenastBesökt >= före);
    }

    [Fact]
    public void SökLegimusUrls_Fleraposter_SorterasNyastFörst()
    {
        LäggTillNedladdning("https://legimus.se/bok/1", "Äldre.mp3", 13300000000000000L);
        LäggTillNedladdning("https://legimus.se/bok/2", "Nyare.mp3", 13400000000000000L);

        var resultat = ChromeHistoryService.SökLegimusUrls(_dbSökväg);

        Assert.Equal("https://legimus.se/bok/2", resultat[0].Url);
        Assert.Equal("https://legimus.se/bok/1", resultat[1].Url);
    }

    [Fact]
    public void ChromeTidTillDateTime_NollMikrosekunder_Ger1601()
    {
        var förväntat = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();

        var resultat = ChromeHistoryService.ChromeTidTillDateTime(0);

        Assert.Equal(förväntat, resultat);
    }

    [Fact]
    public void ChromeTidTillDateTime_KändVärde_ReturnerarKorrektDatum()
    {
        // 1 sekund = 1 000 000 mikrosekunder
        var epok = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var förväntat = epok.AddSeconds(1).ToLocalTime();

        var resultat = ChromeHistoryService.ChromeTidTillDateTime(1_000_000L);

        Assert.Equal(förväntat, resultat);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbSökväg)) File.Delete(_dbSökväg);
    }
}
