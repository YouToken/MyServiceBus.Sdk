using MyServiceBus.Abstractions;
using MyServiceBus.Sdk.Abstractions;
using MyServiceBus.Sdk.Mappers;
using MyServiceBus.TcpClient;

namespace MyServiceBus.Sdk;

public class MyServiceBusSubscriber<T> : IMyServiceBusSubscriber<T>, IMyServiceBusSubscriber<IReadOnlyList<T>>
{
    private readonly int _chunkSize;
    private readonly Dictionary<byte, List<Func<T, ValueTask>>> _list = new();
    private readonly Dictionary<byte, List<Func<IReadOnlyList<T>, ValueTask>>> _listBatch = new();
    
    public MyServiceBusSubscriber(MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType, bool batchSubscribe, int chunkSize = 100)
    {
        if (!batchSubscribe)
        {
            client.Subscribe(topicName, queueName, queryType, HandlerSingle);
        }
        else
        {
            client.Subscribe(topicName, queueName, queryType, HandlerBatch);
        }

        _chunkSize = chunkSize;
    }
    private async ValueTask HandlerSingle(IMyServiceBusMessage data)
    {
        var (version, message) = data.Data.ByteArrayToServiceBusContract<T>();
        var a = _list[version];

        var subscribers = _list.ContainsKey(version) ? _list[version] : new List<Func<T, ValueTask>>(); 
        
        foreach (var subscriber in subscribers)
            await subscriber(message);
    }

    private async ValueTask HandlerBatch(IConfirmationContext ctx, IReadOnlyList<IMyServiceBusMessage> data)
    {
        if (data.Count <= _chunkSize)
        {
            byte contractVersion = 0;
            
            var items = data.Select(e =>
            {
                var (version, message) = e.Data.ByteArrayToServiceBusContract<T>();
                contractVersion = version;
                return message;
            }).ToList();

            if (!items.Any())
                return;

            var subscribers = _listBatch.ContainsKey(contractVersion) ? _listBatch[contractVersion] : new List<Func<IReadOnlyList<T>, ValueTask>>(); 

            foreach (var subscriber in subscribers)
            {
                await subscriber(items);
            }
        }
        else
        {
            var index = 0;
            var chunk = data.Skip(index).Take(_chunkSize);
            while (chunk.Any())
            {
                byte contractVersion = 0;
                
                var items = chunk.Select(e =>
                {
                    var (version, message) = e.Data.ByteArrayToServiceBusContract<T>();
                    contractVersion = version;
                    return message;
                }).ToList();

                if (!items.Any())
                    return;
                
                var subscribers = _listBatch.ContainsKey(contractVersion) ? _listBatch[contractVersion] : new List<Func<IReadOnlyList<T>, ValueTask>>();
                
                foreach (var callback in subscribers)
                {
                    await callback(items);
                }
                    
                ctx.ConfirmMessages(chunk.Select(e => e.Id));

                index += _chunkSize;
                chunk = data.Skip(index).Take(_chunkSize).ToList();
            }
        }
    }

    public void Subscribe(Func<T, ValueTask> callback, byte version)
    {
        if (!_list.ContainsKey(version))
            _list.Add(version, new List<Func<T, ValueTask>>());
        
        _list[version].Add(callback);
    }

    public void Subscribe(Func<IReadOnlyList<T>, ValueTask> callback, byte version)
    {
        if (!_listBatch.ContainsKey(version))
            _listBatch.Add(version, new List<Func<IReadOnlyList<T>, ValueTask>>());
        
        _listBatch[version].Add(callback);
    }
}