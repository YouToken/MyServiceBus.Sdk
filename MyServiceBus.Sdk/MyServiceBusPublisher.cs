using MyServiceBus.Sdk.Abstractions;
using MyServiceBus.Sdk.Mappers;
using MyServiceBus.TcpClient;

namespace MyServiceBus.Sdk;

public class MyServiceBusPublisher<T> : IServiceBusPublisher<T>
{
    private readonly MyServiceBusTcpClient _client;
    private readonly string _topicName;
    private readonly bool _immediatelyPersist;
    private readonly byte? _contractVersion;

    public MyServiceBusPublisher(MyServiceBusTcpClient client, string topicName, bool immediatelyPersist,
        byte? contractVersion)
    {
        _client = client;
        _topicName = topicName;
        _immediatelyPersist = immediatelyPersist;
        _contractVersion = contractVersion;
    }

    public Task PublishAsync(T message)
    {
        byte[] messToPublish = null;

        messToPublish = _contractVersion == null
            ? message.ServiceBusContractToByteArrayWithoutVersion()
            : message.ServiceBusContractToByteArray(_contractVersion.Value);

        return _client.PublishAsync(_topicName, messToPublish, _immediatelyPersist);
    }

    public Task PublishAsync(IEnumerable<T> messageList)
    {
        var batch = messageList.Select(e =>
        {
            if (_contractVersion == null)
                return e.ServiceBusContractToByteArrayWithoutVersion();

            return e.ServiceBusContractToByteArray(_contractVersion.Value);
        }).ToList();
        return _client.PublishAsync(_topicName, batch, _immediatelyPersist);
    }
}