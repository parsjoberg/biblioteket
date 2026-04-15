namespace Biblioteket.Data
{
    public class Författare
    {
        public int Id { get; set; }
        public string Namn { get; set; } = string.Empty;
        public List<Bok> Böcker { get; set; }

    }
}
