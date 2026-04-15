using Microsoft.EntityFrameworkCore;

namespace Biblioteket.Data
{
    public class BiblioteketDbContext(DbContextOptions<BiblioteketDbContext> options) : DbContext(options)
    {
        public DbSet<Nedladdning> Nedladdningar { get; set; }
        public DbSet<Författare> Författare { get; set; }
        public DbSet<Inläsare> Inläsare { get; set; }
        public DbSet<Bok> Böcker { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Behöver migration
            //modelBuilder.Entity<Bok>().HasData(new Bok
            //{
            //    Id = 1,
            //    LegimusUrl = "https://legimus.se/bok/123456",
            //    Titel = "Exempelbok",
            //});             
        }
    }
}
