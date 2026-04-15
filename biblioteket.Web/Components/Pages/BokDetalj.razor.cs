using biblioteket.Services.Interfaces;
using biblioteket.Services.Models;
using Microsoft.AspNetCore.Components;

namespace biblioteket.Web.Components.Pages
{
    public partial class BokDetalj(IBokService bokService, NavigationManager navigationManager)
    {
        [Parameter]
        public int Id { get; set; }

        private Bok? bok;
        private Bok? redigeraKopia;
        private bool redigeringsläge = false;
        private string nyFörfattare = string.Empty;
        private bool bekräftaTabort = false;

        private bool skapaLäge => Id == 0;

        protected override async Task OnParametersSetAsync()
        {
            if (skapaLäge)
            {
                bok = null;
                redigeringsläge = false;
                redigeraKopia = new Bok { Titel = string.Empty, Författare = [], Inläsare = string.Empty };
            }
            else
            {
                bok = await bokService.GetBokByIdAsync(Id);
            }
        }

        private void ToggleRedigeringsläge()
        {
            redigeringsläge = !redigeringsläge;

            redigeraKopia = redigeringsläge && bok is not null
                ? new Bok
                {
                    Id = bok.Id,
                    Titel = bok.Titel,
                    Författare = [.. bok.Författare],
                    Inläsare = bok.Inläsare,
                }
                : null;
        }

        private void LäggTillFörfattare()
        {
            if (redigeraKopia is null || string.IsNullOrWhiteSpace(nyFörfattare)) return;
            redigeraKopia.Författare.Add(nyFörfattare.Trim());
            nyFörfattare = string.Empty;
        }

        private void TaBortFörfattare(string namn) => redigeraKopia?.Författare.Remove(namn);

        private async Task Spara()
        {
            if (redigeraKopia is null) return;

            if (skapaLäge)
            {
                var sparad = await bokService.LäggTillBokAsync(redigeraKopia);
                navigationManager.NavigateTo($"/bocker/{sparad.Id}");
            }
            else
            {
                await bokService.UppdateraBokAsync(redigeraKopia);
                bok = redigeraKopia;
                redigeraKopia = null;
                redigeringsläge = false;
            }
        }

        private void VisaBekräftelse() => bekräftaTabort = true;
        private void AvbrytTabort() => bekräftaTabort = false;

        private async Task TaBortBok()
        {
            await bokService.TaBortBokAsync(Id);
            navigationManager.NavigateTo("/bocker");
        }
    }
}
