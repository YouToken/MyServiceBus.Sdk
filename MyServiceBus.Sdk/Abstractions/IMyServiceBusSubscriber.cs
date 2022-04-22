namespace MyServiceBus.Sdk.Abstractions;

public interface IMyServiceBusSubscriber<out TValue>
{
    void Subscribe(Func<TValue, ValueTask> callback, byte version);
}