namespace Traffic.Application.Messaging;

public interface IMessageConsumer<TMessage>
{
    Task<TMessage?> ConsumeAsync(CancellationToken cancellationToken);
}
