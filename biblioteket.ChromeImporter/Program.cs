using Microsoft.Extensions.Configuration;

namespace biblioteket.ChromeImporter;

static class Program
{
    public static string ApiAdress { get; private set; } = "http://localhost:5332";

    [STAThread]
    static void Main()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        ApiAdress = config["ApiAdress"] ?? ApiAdress;

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}