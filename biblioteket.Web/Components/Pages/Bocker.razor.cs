using biblioteket.Services.Interfaces;
using biblioteket.Services.Models;
using Microsoft.AspNetCore.Components;

namespace biblioteket.Web.Components.Pages
{
    public partial class Bocker(IBokService bokService)
    {
        [SupplyParameterFromQuery(Name = "forfattare")]
        public string? FilterFörfattare { get; set; }

        [SupplyParameterFromQuery(Name = "inlasare")]
        public string? FilterInläsare { get; set; }

        private List<Bok> BokLista = new List<Bok>();
        private int CurrentPage = 1;
        private int ItemsPerPage = 20;

        private IEnumerable<Bok> FilteradBokLista => BokLista
            .Where(b => string.IsNullOrEmpty(FilterFörfattare) || b.Författare.Contains(FilterFörfattare))
            .Where(b => string.IsNullOrEmpty(FilterInläsare) || b.Inläsare == FilterInläsare);

        private int TotalPages => (int)Math.Ceiling(FilteradBokLista.Count() / (double)ItemsPerPage);

        private IEnumerable<Bok> PagedBokLista =>
            FilteradBokLista.Skip((CurrentPage - 1) * ItemsPerPage).Take(ItemsPerPage);

        private void GoToPage(int page) => CurrentPage = page;
        private void PreviousPage() { if (CurrentPage > 1) CurrentPage--; }
        private void NextPage() { if (CurrentPage < TotalPages) CurrentPage++; }
                
        protected async override Task OnInitializedAsync()
        {        
            var boklista = await bokService.GetBöckerAsync();

            BokLista = boklista
                .Where(b => !string.IsNullOrWhiteSpace(b.Titel))
                .ToList();
        }

        protected override void OnParametersSet()
        {
            // Återställ till sida 1 när filtret ändras
            CurrentPage = 1;
        }
    }
}
