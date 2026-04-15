using biblioteket.Services.Models;

namespace biblioteket.Services.Interfaces
{
    public interface IBokService
    {
        Task<List<Bok>> GetBöckerAsync();
        Task<Bok?> GetBokByIdAsync(int id);
        Task UppdateraBokAsync(Bok bok);
        Task<Bok> LäggTillBokAsync(Bok bok);
        Task LäggTillBokAsync(string legimusUrl);
        Task TaBortBokAsync(int id);
    }
}
