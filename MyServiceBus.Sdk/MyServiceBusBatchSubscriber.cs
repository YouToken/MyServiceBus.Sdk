using MyServiceBus.Abstractions;
using MyServiceBus.Sdk.Abstractions;
using MyServiceBus.Sdk.Mappers;
using MyServiceBus.TcpClient;

namespace MyServiceBus.Sdk;

public class MyServiceBusSubscriberBatchWithoutVersion<T> : IMyServiceBusSubscriberWithoutVersion<T>, IMyServiceBusSubscriberWithoutVersion<IReadOnlyList<T>>
{
    private readonly int _chunkSize;
    private readonly List<Func<T, ValueTask>> _list = new();
    private readonly List<Func<IReadOnlyList<T>, ValueTask>> _listBatch = new();
    
    public MyServiceBusSubscriberBatchWithoutVersion(MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType, bool batchSubscribe, int chunkSize = 100)
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
        var message = data.Data.ByteArrayToServiceBusContractWithoutVersion<T>();

        if (!_list.Any())
        {
            return;
        }
        
        foreach (var subscriber in _list)
            await subscriber(message);
    }

    private async ValueTask HandlerBatch(IConfirmationContext ctx, IReadOnlyList<IMyServiceBusMessage> data)
    {
        if (data.Count <= _chunkSize)
        {
            var items = data.Select(e =>
            {
                var message = e.Data.ByteArrayToServiceBusContractWithoutVersion<T>();
                return message;
            }).ToList();

            if (!items.Any())
                return;
            
            foreach (var subscriber in _listBatch)
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
                
                var items = chunk.Select(e =>
                {
                    var message = e.Data.ByteArrayToServiceBusContractWithoutVersion<T>();
                    return message;
                }).ToList();

                if (!items.Any())
                    return;
                
                foreach (var callback in _listBatch)
                {
                    await callback(items);
                }
                    
                ctx.ConfirmMessages(chunk.Select(e => e.Id));

                index += _chunkSize;
                chunk = data.Skip(index).Take(_chunkSize).ToList();
            }
        }
    }

    public void Subscribe(Func<T, ValueTask> callback)
    {
       _list.Add(callback);
    }

    public void Subscribe(Func<IReadOnlyList<T>, ValueTask> callback)
    {
        _listBatch.Add(callback);
    }
}