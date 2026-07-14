using MqManager.CliMock.Models;

namespace MqManager.CliMock.Services;

public class MockMqService : IDisposable
{
    private readonly MqConnectionSettings _settings;
    private readonly MockDataStore _store;

    public MockMqService(MqConnectionSettings settings, MockDataStore store)
    {
        _settings = settings;
        _store = store;
    }

    public bool IsConnected => true;

    public void Connect()
    {
        // No-op: mock is always connected
    }

    public string GetConnectedQueueManager()
    {
        return _settings.QueueManager;
    }

    public void PutMessage(string queueName, string messageBody)
    {
        var queue = _store.GetOrCreateQueue(queueName);
        queue.Messages.Enqueue(new MockMessage
        {
            Id = MockDataStore.GenerateMessageId(),
            CorrelationId = "",
            PutDateTime = DateTime.UtcNow,
            Body = messageBody
        });
    }

    public List<TextMessageInfo> GetMessages(string queueName, int count = 1, int timeoutMs = 3000)
    {
        var queue = _store.GetOrCreateQueue(queueName);
        var messages = new List<TextMessageInfo>();

        for (int i = 0; i < count; i++)
        {
            if (!queue.Messages.TryDequeue(out var msg)) break;
            messages.Add(ToTextMessageInfo(msg));
        }
        return messages;
    }

    public List<TextMessageInfo> BrowseMessages(string queueName, int count = 10, int timeoutMs = 3000)
    {
        var queue = _store.GetOrCreateQueue(queueName);
        return queue.Messages.Take(count).Select(ToTextMessageInfo).ToList();
    }

    public int GetQueueDepth(string queueName)
    {
        var queue = _store.GetQueue(queueName);
        return queue?.Messages.Count ?? 0;
    }

    public int PurgeQueue(string queueName)
    {
        var queue = _store.GetOrCreateQueue(queueName);
        int purged = 0;
        while (queue.Messages.TryDequeue(out _))
        {
            purged++;
        }
        return purged;
    }

    private static TextMessageInfo ToTextMessageInfo(MockMessage msg)
    {
        return new TextMessageInfo
        {
            MessageId = msg.Id,
            CorrelationId = msg.CorrelationId,
            PutDateTime = msg.PutDateTime,
            Body = msg.Body
        };
    }

    public void Dispose()
    {
        // No-op: no real connections to close
    }
}

public class TextMessageInfo
{
    public string MessageId { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public DateTime? PutDateTime { get; set; }
    public string Body { get; set; } = "";
}
