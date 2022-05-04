namespace MyServiceBus.Sdk.Mappers;

public static class ContractToDomainMapper
{
    public static (byte, T) ByteArrayToServiceBusContract<T>(this ReadOnlyMemory<byte> data)
    {
        try
        {
            var span = data.Slice(1, data.Length - 1).Span;
            var mem = new MemoryStream(data.Length);
            mem.Write(span);
            mem.Position = 0;
            return (data.Span[0], ProtoBuf.Serializer.Deserialize<T>(mem));
        }
        catch (Exception ex)
        {
            var dataBase64 = Convert.ToBase64String(data.ToArray());
            Console.WriteLine($"Cannot deserialize message {typeof(T).Name}. Data: '{dataBase64}'");

            throw new Exception($"Cannot deserialize message {typeof(T).Name}: {ex.Message}", ex);
        }
    }
    
    public static byte[] ServiceBusContractToByteArray(this object src, byte contractVersion)
    {
        try
        {
            var stream = new MemoryStream();

            stream.WriteByte(contractVersion); // First byte is a version contract;

            ProtoBuf.Serializer.Serialize(stream, src);

            var result = stream.ToArray();
            
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Cannot serialize {src.GetType().Name}: {ex.Message}", ex);
        }
    }
}