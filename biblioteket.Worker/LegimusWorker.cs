using biblioteket.Services.Interfaces;
using Biblioteket.Data;
using Biblioteket.Data.Messages;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace biblioteket.Worker
{
    public class LegimusWorker(
        IConnectionMultiplexer redis,
        IBokService bokService,
        IHttpClientFactory httpClientFactory,
        ILogger<LegimusWorker> logger) : BackgroundService
    {
        private const string StreamKey = "legimus:bok-registrerad";
        private const string GroupName = "legimus-workers";
        private readonly string _consumerName = $"worker-{Environment.MachineName}";

        private static string Decode(string? raw) =>
            HtmlEntity.DeEntitize(raw?.Trim()) ?? string.Empty;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var db = redis.GetDatabase();
            await EnsureConsumerGroupAsync(db);

            while (!stoppingToken.IsCancellationRequested)
            {
                var messages = await db.StreamReadGroupAsync(
                    StreamKey, GroupName, _consumerName, ">", count: 10);

                if (messages.Length == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                foreach (var message in messages)
                {
                    try
                    {
                        var payload = message.Values
                            .First(v => v.Name == "payload").Value.ToString();

                        var @event = JsonSerializer.Deserialize<BokRegistreradEvent>(payload)
                            ?? throw new InvalidOperationException("Ogiltigt meddelande i strömmen.");

                        await HämtaLegimusInfoAsync(@event, stoppingToken);
                        await db.StreamAcknowledgeAsync(StreamKey, GroupName, message.Id);
                        
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Fel vid bearbetning av meddelande {MessageId}", message.Id);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }

        private async Task EnsureConsumerGroupAsync(IDatabase db)
        {
            try
            {
                await db.StreamCreateConsumerGroupAsync(
                    StreamKey, GroupName, StreamPosition.NewMessages, createStream: true);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
                // Konsumentgruppen finns redan, det är OK
            }
        }

        private async Task HämtaLegimusInfoAsync(BokRegistreradEvent @event, CancellationToken cancellationToken)
        {            
            //var bok = await db.Böcker.FirstOrDefaultAsync(b => b.Id == @event.BokId, cancellationToken);
            var bok = await bokService.GetBokByIdAsync(@event.BokId);
            if (bok is null) return;

            // TODO: Hämta information från Legimus med bok.LegimusUrl eller @event.Titel
            // var client = httpClientFactory.CreateClient("Legimus");
            // ...
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(bok.LegimusUrl, cancellationToken);
                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                bok.Titel = Decode(doc.DocumentNode
                    .SelectSingleNode("//h1")
                ?.InnerText) ?? "Kunde inte hämta titel";
                
                var författareString = Decode(doc.DocumentNode
                    .SelectSingleNode("//p[strong[normalize-space()='Av:']]")
                    ?.InnerText.Replace("Av:", ""));
                var författareNamn = författareString.Split(',');                
                bok.Författare.Clear();                
                foreach (var namn in författareNamn)
                {
                    bok.Författare.Add(namn.Trim());
                }

                var inläsareNamn = Decode(doc.DocumentNode
                .SelectSingleNode("//p[strong[contains(text(),'Inl&#xE4;sare')]]//a")
                    ?.InnerText);

                bok.Inläsare = inläsareNamn;   
                
                bok.HämtatLegimusInformationTidpunkt = DateTime.UtcNow;

                await bokService.UppdateraBokAsync(bok);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fel vid hämtning av information för url: {url}", bok.LegimusUrl);
            }

            logger.LogInformation("Legimus-information hämtad för bok {BokId} ({Titel})",
                        @event.BokId, @event.Titel);
        }




    }


}