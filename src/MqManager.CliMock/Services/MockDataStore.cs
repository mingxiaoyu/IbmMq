using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace MqManager.CliMock.Services;

public class MockMessage
{
    public string Id { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public DateTime? PutDateTime { get; set; }
    public string Body { get; set; } = "";
}

public class MockQueue
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Local";
    public ConcurrentQueue<MockMessage> Messages { get; } = new();
}

public class MockChannel
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Stopped";
    public string ConnectionName { get; set; } = "";
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public int Messages { get; set; }
    public int BuffersSent { get; set; }
    public int BuffersReceived { get; set; }
    public string QueueManagerName { get; set; } = "";
    public string TransmissionQueue { get; set; } = "";
    public string Substate { get; set; } = "";
    public string IndoubtStatus { get; set; } = "";
    public string MessageCompression { get; set; } = "";
    public string HeaderCompression { get; set; } = "";
    public string McaUserId { get; set; } = "";
    public string AppName { get; set; } = "";
}

public class MockDataStore
{
    private readonly ConcurrentDictionary<string, MockQueue> _queues = new();
    private readonly ConcurrentDictionary<string, MockChannel> _channels = new();

    public MockQueue GetOrCreateQueue(string name)
    {
        return _queues.GetOrAdd(name, n => new MockQueue { Name = n, Type = "Local" });
    }

    public MockQueue? GetQueue(string name)
    {
        _queues.TryGetValue(name, out var queue);
        return queue;
    }

    public bool RemoveQueue(string name)
    {
        return _queues.TryRemove(name, out _);
    }

    public MockChannel? GetChannel(string name)
    {
        _channels.TryGetValue(name, out var channel);
        return channel;
    }

    public List<MockQueue> GetAllQueues(string filter = "*")
    {
        var pattern = WildcardToRegex(filter);
        return _queues.Values
            .Where(q => Regex.IsMatch(q.Name, pattern, RegexOptions.IgnoreCase))
            .OrderBy(q => q.Name)
            .ToList();
    }

    public List<MockChannel> GetAllChannels(string filter = "*")
    {
        var pattern = WildcardToRegex(filter);
        return _channels.Values
            .Where(c => Regex.IsMatch(c.Name, pattern, RegexOptions.IgnoreCase))
            .OrderBy(c => c.Name)
            .ToList();
    }

    public void SeedDefaults()
    {
        // Queues
        AddQueue("SYSTEM.DEFAULT.LOCAL.QUEUE", "Local");
        AddQueue("SYSTEM.DEFAULT.MODEL.QUEUE", "Model");
        AddQueue("TEST.QUEUE.1", "Local");
        AddQueue("TEST.QUEUE.2", "Local");
        AddQueue("APP.REQUEST.QUEUE", "Local");
        AddQueue("APP.REPLY.QUEUE", "Local");
        AddQueue("DEAD.LETTER.QUEUE", "Local");

        // Pre-seeded messages
        var q1 = GetQueue("TEST.QUEUE.1")!;
        q1.Messages.Enqueue(new MockMessage
        {
            Id = GenerateMessageId(),
            CorrelationId = "",
            PutDateTime = DateTime.UtcNow.AddMinutes(-1),
            Body = "Hello from mock MQ"
        });
        q1.Messages.Enqueue(new MockMessage
        {
            Id = GenerateMessageId(),
            CorrelationId = "",
            PutDateTime = DateTime.UtcNow.AddSeconds(-30),
            Body = "Test message 2"
        });

        var reqQ = GetQueue("APP.REQUEST.QUEUE")!;
        reqQ.Messages.Enqueue(new MockMessage
        {
            Id = GenerateMessageId(),
            CorrelationId = "corr-001",
            PutDateTime = DateTime.UtcNow.AddMinutes(-2),
            Body = "{\"action\":\"ping\",\"id\":1}"
        });

        // Channels
        AddChannel("SYSTEM.DEF.SVRCONN", "SVRCONN", "System default server-connection channel");
        AddChannel("SYSTEM.DEF.SENDER", "Sender", "System default sender channel");
        AddChannel("SYSTEM.DEF.RECEIVER", "Receiver", "System default receiver channel");
        AddChannel("APP.SVRCONN", "SVRCONN", "Application server-connection channel",
            status: "Running", connectionName: "10.0.0.1(1414)",
            bytesSent: 1024, bytesReceived: 512, messages: 42,
            buffersSent: 10, buffersReceived: 8,
            substate: "Sending", mcaUserId: "appuser",
            appName: "MyApp");
        AddChannel("APP.SENDER", "Sender", "Application sender channel",
            status: "Running", queueManagerName: "QM2",
            transmissionQueue: "XMIT.QM2",
            messageCompression: "None", headerCompression: "None");
        AddChannel("TO.QM2", "Sender", "Sender to QM2",
            status: "Running", queueManagerName: "QM2",
            transmissionQueue: "XMIT.QM2",
            connectionName: "10.0.0.2(1414)",
            messages: 100, substate: "Sending");
    }

    private void AddQueue(string name, string type)
    {
        _queues[name] = new MockQueue { Name = name, Type = type };
    }

    private void AddChannel(string name, string type, string description,
        string status = "Stopped", string connectionName = "",
        long bytesSent = 0, long bytesReceived = 0, int messages = 0,
        int buffersSent = 0, int buffersReceived = 0,
        string queueManagerName = "", string transmissionQueue = "",
        string substate = "", string indoubtStatus = "",
        string messageCompression = "", string headerCompression = "",
        string mcaUserId = "", string appName = "")
    {
        _channels[name] = new MockChannel
        {
            Name = name,
            Type = type,
            Description = description,
            Status = status,
            ConnectionName = connectionName,
            BytesSent = bytesSent,
            BytesReceived = bytesReceived,
            Messages = messages,
            BuffersSent = buffersSent,
            BuffersReceived = buffersReceived,
            QueueManagerName = queueManagerName,
            TransmissionQueue = transmissionQueue,
            Substate = substate,
            IndoubtStatus = indoubtStatus,
            MessageCompression = messageCompression,
            HeaderCompression = headerCompression,
            McaUserId = mcaUserId,
            AppName = appName
        };
    }

    public static string GenerateMessageId()
    {
        return "ID:" + Guid.NewGuid().ToString("N").ToUpper();
    }

    private static string WildcardToRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern).Replace("\\*", ".*");
        return "^" + escaped + "$";
    }
}
