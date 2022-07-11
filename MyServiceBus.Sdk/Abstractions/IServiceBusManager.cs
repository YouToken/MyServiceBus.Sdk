namespace MyServiceBus.Sdk.Abstractions;

public interface IServiceBusManager
{
    void Start();
    void Stop();
    bool IsStarted { get; }
    bool IsConnected { get; }
    string HostPort { get; }
}