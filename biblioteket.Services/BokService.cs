using biblioteket.Services.Interfaces;
using biblioteket.Services.Models;
using Biblioteket.Data;
using Biblioteket.Data.Messages;
using Microsoft.EntityFrameworkCore;

namespace biblioteket.Services
{
    public class BokService(
        IDbContextFactory<BiblioteketDbContext> dbContextFactory,
        IBokEventPublisher eventPublisher) : IBokService
    {
        public async Task<Models.Bok?> GetBokByIdAsync(int id)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            var bok = await db.Böcker
                .Include(b => b.Författare)
                .Include(b => b.Inläsare)
                .Include(b => b.Nedladdningar)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bok == null)
                return null;

            return new Models.Bok
            {
                Id = bok.Id,
                LegimusUrl = bok.LegimusUrl ?? string.Empty,
                HämtatLegimusInformationTidpunkt = bok.HämtatLegimusInformationTidpunkt,
                Titel = bok.Titel ?? "Okänd",
                Författare = bok.Författare.Select(f => f.Namn).ToList(),
                Inläsare = bok.Inläsare?.Namn ?? string.Empty,
                SenastNedladdad = bok.Nedladdningar
                    .OrderByDescending(n => n.Tidpunkt)
                    .FirstOrDefault()?.Tidpunkt,
                
            };
        }

        public async Task<List<Models.Bok>> GetBöckerAsync()
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            var boklista = await db.Böcker
                .Include(b => b.Författare)
                .Include(b => b.Inläsare)
                .Include(b => b.Nedladdningar)
                .ToListAsync();

            return boklista.Select(b => new Models.Bok
            {
                Id = b.Id,
                LegimusUrl = b.LegimusUrl,
                Titel = b.Titel ?? "Okänd",
                Författare = b.Författare.Select(f => f.Namn).ToList(),
                Inläsare = b.Inläsare?.Namn ?? string.Empty,
                SenastNedladdad = b.Nedladdningar
                    .OrderByDescending(n => n.Tidpunkt)
                    .FirstOrDefault()?.Tidpunkt
            }).ToList();
        }

        public async Task UppdateraBokAsync(Models.Bok bok)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            var dbBok = await db.Böcker
                .Include(b => b.Författare)
                .Include(b => b.Inläsare)
                .FirstOrDefaultAsync(b => b.Id == bok.Id)
                ?? throw new Exception("Bok inte hittad");

            dbBok.Titel = bok.Titel;
            dbBok.LegimusUrl = bok.LegimusUrl;

            dbBok.Författare.Clear();
            foreach (var namn in bok.Författare)
            {
                var befintlig = await db.Författare.FirstOrDefaultAsync(f => f.Namn == namn);
                dbBok.Författare.Add(befintlig ?? new Biblioteket.Data.Författare { Namn = namn });
            }

            if (!string.IsNullOrWhiteSpace(bok.Inläsare))
            {
                var befintligInläsare = await db.Inläsare.FirstOrDefaultAsync(i => i.Namn == bok.Inläsare);
                dbBok.Inläsare = befintligInläsare ?? new Biblioteket.Data.Inläsare { Namn = bok.Inläsare };
            }
            else
            {
                dbBok.Inläsare = null;
            }

            dbBok.HämtatLegimusInformationTidpunkt = bok.HämtatLegimusInformationTidpunkt;

            await db.SaveChangesAsync();
        }

        public async Task<Models.Bok> LäggTillBokAsync(Models.Bok bok)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            var nyBok = new Biblioteket.Data.Bok
            {
                Titel = bok.Titel,
                LegimusUrl = bok.LegimusUrl,
            };

            foreach (var namn in bok.Författare)
            {
                var befintlig = await db.Författare.FirstOrDefaultAsync(f => f.Namn == namn);
                nyBok.Författare.Add(befintlig ?? new Biblioteket.Data.Författare { Namn = namn });
            }

            if (!string.IsNullOrWhiteSpace(bok.Inläsare))
            {
                var befintligInläsare = await db.Inläsare.FirstOrDefaultAsync(i => i.Namn == bok.Inläsare);
                nyBok.Inläsare = befintligInläsare ?? new Biblioteket.Data.Inläsare { Namn = bok.Inläsare };
            }

            db.Böcker.Add(nyBok);
            await db.SaveChangesAsync();

            await eventPublisher.PublishAsync(
                new BokRegistreradEvent(nyBok.Id, nyBok.LegimusUrl ?? string.Empty));

            return new Models.Bok
            {
                Id = nyBok.Id,
                Titel = nyBok.Titel ?? string.Empty,
                Författare = nyBok.Författare.Select(f => f.Namn).ToList(),
                Inläsare = nyBok.Inläsare?.Namn ?? string.Empty
            };
        }

        public async Task TaBortBokAsync(int id)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            var bok = await db.Böcker
                .Include(b => b.Författare)
                .Include(b => b.Inläsare)
                .FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new Exception("Bok inte hittad");

            var bokFörfattare = bok.Författare.ToList();
            foreach (var författare in bokFörfattare)
            {
                if (db.Författare.Count(f => f.Böcker.Any(b => b.Id != id && b.Författare.Any(f2 => f2.Id == författare.Id))) == 0)
                {
                    db.Författare.Remove(författare);
                }
            }

            var inläsare = bok.Inläsare;
            if (inläsare != null && db.Inläsare.Count(i => i.Böcker.Any(b => b.Id != id && b.Inläsare != null && b.Inläsare.Id == inläsare.Id)) == 0)
            {
                db.Inläsare.Remove(inläsare);
            }

            db.Böcker.Remove(bok);
            await db.SaveChangesAsync();
        }

        public async Task LäggTillBokAsync(string legimusUrl)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            var bok = await db.Böcker.FirstOrDefaultAsync(b => b.LegimusUrl == legimusUrl);
            if (bok == null)
            {
                bok = new Biblioteket.Data.Bok
                {
                    LegimusUrl = legimusUrl                    
                };
                db.Böcker.Add(bok);
                await db.SaveChangesAsync();
            }

            await eventPublisher.PublishAsync(
                new BokRegistreradEvent(bok.Id, bok.LegimusUrl ?? string.Empty));
        }
    }
}
