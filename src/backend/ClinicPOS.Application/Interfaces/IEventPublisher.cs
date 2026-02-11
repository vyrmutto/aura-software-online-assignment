namespace ClinicPOS.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default);
}
