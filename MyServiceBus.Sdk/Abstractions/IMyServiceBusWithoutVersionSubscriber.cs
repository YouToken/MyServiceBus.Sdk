namespace MyServiceBus.Sdk.Abstractions;

public interface IMyServiceBusSubscriberWithoutVersion<out TValue>
{
    void Subscribe(Func<TValue, ValueTask> callback);
}