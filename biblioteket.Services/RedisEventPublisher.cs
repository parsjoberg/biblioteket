using System.Text.Json;
using Biblioteket.Data.Messages;
using StackExchange.Redis;
using biblioteket.Services.Interfaces;

namespace biblioteket.Services
{
    public class RedisEventPublisher(IConnectionMultiplexer redis) : IBokEventPublisher
    {
        public const string StreamKey = "legimus:bok-registrerad";

        public async Task PublishAsync(BokRegistreradEvent @event, CancellationToken cancellationToken = default)
        {
            var db = redis.GetDatabase();
            await db.StreamAddAsync(StreamKey,
            [
                new NameValueEntry("payload", JsonSerializer.Serialize(@event))
            ]);
        }
    }
}