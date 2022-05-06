using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyServiceBus.Abstractions;
using MyServiceBus.Sdk.Abstractions;
using MyServiceBus.TcpClient;

namespace MyServiceBus.Sdk;

public static class MyServiceBusStartupUtils
{
    public static MyServiceBusTcpClient Create(Func<string> getHostPort, ILogger logger, string appName)
    {
        var serviceBusClient = new MyServiceBusTcpClient(getHostPort, appName);
        serviceBusClient.Log.AddLogException(ex => logger.LogError(ex, "Exception in MyServiceBusTcpClient"));
        serviceBusClient.Log.AddLogInfo(info => logger.LogDebug($"MyServiceBusTcpClient[info]: {info}"));
        serviceBusClient.SocketLogs.AddLogInfo((context, msg) =>
            logger.LogInformation(
                $"MyServiceBusTcpClient[Socket {context?.Id}|{context?.ContextName}|{context?.Inited}][Info] {msg}"));
        serviceBusClient.SocketLogs.AddLogException((context, exception) => logger.LogInformation(exception,
            $"MyServiceBusTcpClient[Socket {context?.Id}|{context?.ContextName}|{context?.Inited}][Exception] {exception.Message}"));

        return serviceBusClient;
    }

    public static MyServiceBusTcpClient RegisterMyServiceBusTcpClient(this IServiceCollection builder,
        Func<string> getHostPort, ILoggerFactory loggerFactory, string appName)
    {
        var logger = loggerFactory.CreateLogger<MyServiceBusTcpClient>();
        var client = Create(getHostPort, logger, appName);

        var manager = new ServiceBusManager(client, getHostPort.Invoke());
        builder.AddSingleton<IServiceBusManager>(manager);

        return client;
    }

    public static IServiceCollection RegisterMyServiceBusPublisher<T>(this IServiceCollection builder,
        MyServiceBusTcpClient client, string topicName, bool immediatelyPersist, byte contractVersion = 0)
    {
        client.CreateTopicIfNotExists(topicName);
        builder
            .AddSingleton<IServiceBusPublisher<T>>(new MyServiceBusPublisher<T>(client, topicName,
                immediatelyPersist, contractVersion));

        return builder;
    }

    public static IServiceCollection RegisterMyServiceBusSubscriberSingle<T>(this IServiceCollection builder,
        MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType)
    {
        builder
            .AddSingleton<IMyServiceBusSubscriber<T>>(new MyServiceBusSubscriber<T>(client, topicName, queueName,
                queryType, false));

        return builder;
    }

    public static IServiceCollection RegisterMyServiceBusSubscriberBatch<T>(this IServiceCollection builder,
        MyServiceBusTcpClient client, string topicName, string queueName, TopicQueueType queryType, int batchSize = 100)
    {
        // batch subscriber
        builder
            .AddSingleton<IMyServiceBusSubscriber<IReadOnlyList<T>>>(
                new MyServiceBusSubscriber<T>(client, topicName, queueName, queryType, true, batchSize));

        return builder;
    }
}