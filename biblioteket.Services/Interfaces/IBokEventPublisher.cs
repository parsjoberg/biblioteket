using Biblioteket.Data.Messages;

namespace biblioteket.Services.Interfaces
{
    public interface IBokEventPublisher
    {
        Task PublishAsync(BokRegistreradEvent @event, CancellationToken cancellationToken = default);
    }
}