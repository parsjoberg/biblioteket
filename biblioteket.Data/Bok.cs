namespace Biblioteket.Data
{
    public class Bok
    {
        public int Id { get; set; }
        public string LegimusUrl { get; set; } = string.Empty;
        public DateTime? HämtatLegimusInformationTidpunkt { get; set; } = null;
        public string? Titel { get; set; } = null;
        public List<Nedladdning> Nedladdningar { get; set; } = [];
        public List<Författare> Författare { get; set; } = [];
        public Inläsare? Inläsare { get; set; } = null;
    }
}
