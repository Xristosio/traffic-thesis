namespace Traffic.Application.Messaging;

public interface IMessagePublisher<in TMessage>
{
    Task PublishAsync(TMessage message, CancellationToken cancellationToken);
}
