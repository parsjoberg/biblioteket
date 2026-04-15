using biblioteket.Services.Interfaces;
using Biblioteket.Data.Messages;

namespace biblioteket.Worker
{
    public class NoOpEventPublisher : IBokEventPublisher
    {
        public Task PublishAsync(BokRegistreradEvent @event, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
    }
}
