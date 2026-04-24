using biblioteket.Services;
using biblioteket.Services.Interfaces;
using biblioteket.Services.Models;
using ServiceBok = biblioteket.Services.Models.Bok;
using Biblioteket.Data;
using Biblioteket.Data.Messages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace biblioteket.External.Services.Test;

/// <summary>
/// Enkel stub för händelsepublicering – lagrar publicerade händelser i minnet.
/// </summary>
class StubEventPublisher : IBokEventPublisher
{
    public List<BokRegistreradEvent> PubliceradHändelser { get; } = [];

    public Task PublishAsync(BokRegistreradEvent @event, CancellationToken cancellationToken = default)
    {
        PubliceradHändelser.Add(@event);
        return Task.CompletedTask;
    }
}

public class BokServiceTests : IDisposable
{
    private readonly SqliteConnection _anslutning;
    private readonly IDbContextFactory<BiblioteketDbContext> _dbFactory;
    private readonly StubEventPublisher _publisher;
    private readonly BokService _tjänst;

    public BokServiceTests()
    {
        // Håll anslutningen öppen så att SQLite in-memory-databasen lever under hela testet
        _anslutning = new SqliteConnection("Data Source=:memory:");
        _anslutning.Open();

        var options = new DbContextOptionsBuilder<BiblioteketDbContext>()
            .UseSqlite(_anslutning)
            .Options;

        using (var db = new BiblioteketDbContext(options))
            db.Database.EnsureCreated();

        _dbFactory = new TestDbContextFactory(options);
        _publisher = new StubEventPublisher();
        _tjänst = new BokService(_dbFactory, _publisher);
    }

    // ── LäggTillBokAsync(string) ─────────────────────────────────────────────

    [Fact]
    public async Task LäggTillBok_NyUrl_LäggsTillIDatabasen()
    {
        await _tjänst.LäggTillBokAsync("https://legimus.se/bok/1");

        var böcker = await _tjänst.GetBöckerAsync();
        Assert.Single(böcker);
        Assert.Equal("https://legimus.se/bok/1", böcker[0].LegimusUrl);
    }

    [Fact]
    public async Task LäggTillBok_DuplicatUrl_LäggsTillInteIgen()
    {
        await _tjänst.LäggTillBokAsync("https://legimus.se/bok/1");
        await _tjänst.LäggTillBokAsync("https://legimus.se/bok/1");

        var böcker = await _tjänst.GetBöckerAsync();
        Assert.Single(böcker);
    }

    [Fact]
    public async Task LäggTillBok_NyUrl_PublicerarHändelse()
    {
        await _tjänst.LäggTillBokAsync("https://legimus.se/bok/42");

        Assert.Single(_publisher.PubliceradHändelser);
        Assert.Equal("https://legimus.se/bok/42", _publisher.PubliceradHändelser[0].Titel);
    }

    // ── GetBokByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetBokById_InteExisterar_ReturnerarNull()
    {
        var resultat = await _tjänst.GetBokByIdAsync(999);

        Assert.Null(resultat);
    }

    [Fact]
    public async Task GetBokById_NullTitel_AnvänderOkänd()
    {
        await _tjänst.LäggTillBokAsync("https://legimus.se/bok/1");
        var böcker = await _tjänst.GetBöckerAsync();

        var bok = await _tjänst.GetBokByIdAsync(böcker[0].Id);

        Assert.Equal("Okänd", bok!.Titel);
    }

    [Fact]
    public async Task GetBokById_FlereNedladdningar_ReturnerarSenastNedladdad()
    {
        await _tjänst.LäggTillBokAsync("https://legimus.se/bok/1");
        var böcker = await _tjänst.GetBöckerAsync();
        var bokId = böcker[0].Id;

        var äldre = DateTime.Now.AddDays(-5);
        var nyare = DateTime.Now.AddDays(-1);

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Nedladdningar.AddRange(
            new Nedladdning { BokId = bokId, Tidpunkt = äldre },
            new Nedladdning { BokId = bokId, Tidpunkt = nyare });
        await db.SaveChangesAsync();

        var bok = await _tjänst.GetBokByIdAsync(bokId);

        Assert.Equal(nyare, bok!.SenastNedladdad!.Value, TimeSpan.FromSeconds(1));
    }

    // ── UppdateraBokAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UppdateraBok_TomInläsare_NollställsInläsare()
    {
        await _tjänst.LäggTillBokAsync(new ServiceBok
        {
            LegimusUrl = "https://legimus.se/bok/1",
            Titel = "Testbok",
            Inläsare = "Kalle"
        });
        var böcker = await _tjänst.GetBöckerAsync();

        await _tjänst.UppdateraBokAsync(new ServiceBok
        {
            Id = böcker[0].Id,
            LegimusUrl = böcker[0].LegimusUrl,
            Titel = "Testbok",
            Inläsare = ""
        });

        var uppdaterad = await _tjänst.GetBokByIdAsync(böcker[0].Id);
        Assert.Equal(string.Empty, uppdaterad!.Inläsare);
    }

    [Fact]
    public async Task UppdateraBok_NyFörfattareLista_ErsätterGamla()
    {
        var bok = await _tjänst.LäggTillBokAsync(new ServiceBok
        {
            LegimusUrl = "https://legimus.se/bok/1",
            Titel = "Testbok",
            Författare = ["Anna", "Bertil"]
        });

        await _tjänst.UppdateraBokAsync(new ServiceBok
        {
            Id = bok.Id,
            LegimusUrl = bok.LegimusUrl,
            Titel = "Testbok",
            Författare = ["Cecilia"]
        });

        var uppdaterad = await _tjänst.GetBokByIdAsync(bok.Id);
        Assert.Equal(["Cecilia"], uppdaterad!.Författare);
    }

    [Fact]
    public async Task UppdateraBok_BefintligFörfattare_ÅteranvändsInteNyPost()
    {
        var bok1 = await _tjänst.LäggTillBokAsync(new ServiceBok
        {
            LegimusUrl = "https://legimus.se/bok/1",
            Titel = "Bok 1",
            Författare = ["Delade Författare"]
        });
        var bok2 = await _tjänst.LäggTillBokAsync(new ServiceBok
        {
            LegimusUrl = "https://legimus.se/bok/2",
            Titel = "Bok 2"
        });

        await _tjänst.UppdateraBokAsync(new ServiceBok
        {
            Id = bok2.Id,
            LegimusUrl = bok2.LegimusUrl,
            Titel = "Bok 2",
            Författare = ["Delade Författare"]
        });

        await using var db = await _dbFactory.CreateDbContextAsync();
        var antalFörfattare = await db.Författare.CountAsync(f => f.Namn == "Delade Författare");
        Assert.Equal(1, antalFörfattare);
    }

    // ── TaBortBokAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task TaBortBok_ExisterarInteIgen()
    {
        await _tjänst.LäggTillBokAsync("https://legimus.se/bok/1");
        var böcker = await _tjänst.GetBöckerAsync();

        await _tjänst.TaBortBokAsync(böcker[0].Id);

        var efterAt = await _tjänst.GetBöckerAsync();
        Assert.Empty(efterAt);
    }

    [Fact]
    public async Task TaBortBok_OrfanInläsare_TasBortUrDatabasen()
    {
        var bok = await _tjänst.LäggTillBokAsync(new ServiceBok
        {
            LegimusUrl = "https://legimus.se/bok/1",
            Titel = "Testbok",
            Inläsare = "Ensam Inläsare"
        });

        await _tjänst.TaBortBokAsync(bok.Id);

        await using var db = await _dbFactory.CreateDbContextAsync();
        Assert.False(await db.Inläsare.AnyAsync(i => i.Namn == "Ensam Inläsare"));
    }

    [Fact]
    public async Task TaBortBok_DeladInläsare_BehållesIDatabasen()
    {
        var bok1 = await _tjänst.LäggTillBokAsync(new ServiceBok
        {
            LegimusUrl = "https://legimus.se/bok/1",
            Titel = "Bok 1",
            Inläsare = "Delad Inläsare"
        });
        await _tjänst.LäggTillBokAsync(new ServiceBok
        {
            LegimusUrl = "https://legimus.se/bok/2",
            Titel = "Bok 2",
            Inläsare = "Delad Inläsare"
        });

        await _tjänst.TaBortBokAsync(bok1.Id);

        await using var db = await _dbFactory.CreateDbContextAsync();
        Assert.True(await db.Inläsare.AnyAsync(i => i.Namn == "Delad Inläsare"));
    }

    public void Dispose() => _anslutning.Dispose();

    private sealed class TestDbContextFactory(DbContextOptions<BiblioteketDbContext> options)
        : IDbContextFactory<BiblioteketDbContext>
    {
        public BiblioteketDbContext CreateDbContext() => new(options);
    }
}
