namespace biblioteket.Services.Models
{
    public class Bok
    {
        public int Id { get; set; }
        public string LegimusUrl { get; set; } = string.Empty;
        public  DateTime? HämtatLegimusInformationTidpunkt { get; set; }
        public string Titel { get; set; } = string.Empty;
        public DateTime? SenastNedladdad { get; set; } = null;
        public List<string> Författare { get; set; } = [];
        public string Inläsare { get; set; } = string.Empty;
    }
}
