using System.Net.Http.Json;

namespace biblioteket.ChromeImporter;

public class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(string basAdress)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(basAdress.TrimEnd('/') + "/")
        };
    }

    /// <summary>
    /// Skickar en Legimus-URL till API:et. Returnerar true om det lyckades.
    /// </summary>
    public async Task<bool> LäggTillLegimusUrlAsync(string legimusUrl)
    {
        try
        {
            var svar = await _httpClient.PostAsJsonAsync(
                "api/legimusUrl",
                new { LegimusUrl = legimusUrl });

            return svar.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}