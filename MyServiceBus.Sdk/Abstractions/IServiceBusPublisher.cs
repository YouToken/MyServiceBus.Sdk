namespace MyServiceBus.Sdk.Abstractions;

public interface IServiceBusPublisher<in T>
{
    Task PublishAsync(T message);
    Task PublishAsync(IEnumerable<T> messageList);
}