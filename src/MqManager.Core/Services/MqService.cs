using IBM.XMS;
using MqManager.Core.Models;

namespace MqManager.Core.Services;

public class MqService : IDisposable
{
    private readonly MqConnectionSettings _settings;
    private IConnectionFactory? _connectionFactory;
    private IConnection? _connection;
    private ISession? _session;
    private bool _disposed;

    public MqService(MqConnectionSettings settings)
    {
        _settings = settings;
    }

    public bool IsConnected => _connection != null;

    public void Connect()
    {
        var factoryFactory = XMSFactoryFactory.GetInstance(XMSC.CT_WMQ);

        _connectionFactory = factoryFactory.CreateConnectionFactory();

        _connectionFactory.SetStringProperty(XMSC.WMQ_HOST_NAME, _settings.HostName);
        _connectionFactory.SetIntProperty(XMSC.WMQ_PORT, _settings.Port);
        _connectionFactory.SetStringProperty(XMSC.WMQ_QUEUE_MANAGER, _settings.QueueManager);
        _connectionFactory.SetStringProperty(XMSC.WMQ_CHANNEL, _settings.Channel);
        _connectionFactory.SetIntProperty(XMSC.WMQ_CONNECTION_MODE, XMSC.WMQ_CM_CLIENT);

        _connection = _connectionFactory.CreateConnection(_settings.Username, _settings.Password);
        _session = _connection.CreateSession(false, AcknowledgeMode.AutoAcknowledge);

        // Start the connection to begin receiving messages
        _connection.Start();
    }

    public string GetConnectedQueueManager()
    {
        if (_connection == null)
            throw new InvalidOperationException("Not connected. Call Connect() first.");

        return _settings.QueueManager;
    }

    public void PutMessage(string queueName, string messageBody)
    {
        EnsureConnected();
        var destination = _session!.CreateQueue(queueName);
        using var producer = _session.CreateProducer(destination);

        var message = _session.CreateTextMessage(messageBody);
        producer.Send(message);
    }

    public List<MessageInfo> GetMessages(string queueName, int count = 1, int timeoutMs = 3000)
    {
        EnsureConnected();
        var destination = _session!.CreateQueue(queueName);
        using var consumer = _session.CreateConsumer(destination);

        var messages = new List<MessageInfo>();
        for (int i = 0; i < count; i++)
        {
            var message = consumer.Receive(timeoutMs);
            if (message == null) break;
            messages.Add(ToMessageInfo(message));
        }
        return messages;
    }

    public List<MessageInfo> BrowseMessages(string queueName, int count = 10, int timeoutMs = 3000)
    {
        EnsureConnected();
        var destination = _session!.CreateQueue(queueName);
        using var browser = _session.CreateBrowser(destination);

        var messages = new List<MessageInfo>();
        var enumerator = browser.GetEnumerator();
        while (enumerator.MoveNext() && messages.Count < count)
        {
            if (enumerator.Current is ITextMessage textMsg)
            {
                messages.Add(ToMessageInfo(textMsg));
            }
        }
        return messages;
    }

    public int PurgeQueue(string queueName)
    {
        EnsureConnected();
        var destination = _session!.CreateQueue(queueName);
        using var consumer = _session.CreateConsumer(destination);

        int purged = 0;
        while (true)
        {
            var message = consumer.Receive(500);
            if (message == null) break;
            purged++;
        }
        return purged;
    }

    public int GetQueueDepth(string queueName)
    {
        EnsureConnected();
        var destination = _session!.CreateQueue(queueName);

        // Use browse to count messages - XMS doesn't directly expose queue depth
        using var browser = _session.CreateBrowser(destination);
        int count = 0;
        var enumerator = browser.GetEnumerator();
        while (enumerator.MoveNext())
        {
            count++;
        }
        return count;
    }

    private static MessageInfo ToMessageInfo(IMessage message)
    {
        string body = "";
        if (message is ITextMessage textMsg)
        {
            body = textMsg.Text;
        }

        return new MessageInfo
        {
            MessageId = message.JMSMessageID ?? "",
            CorrelationId = message.JMSCorrelationID ?? "",
            PutDateTime = TryGetPutDateTime(message) ?? "",
            Body = body
        };
    }

    private static string? TryGetPutDateTime(IMessage message)
    {
        try
        {
            var putDate = message.GetStringProperty("JMS_IBM_PutDate"); // "CCYYMMDD"
            var putTime = message.GetStringProperty("JMS_IBM_PutTime"); // "HHMMSScc"
            if (string.IsNullOrEmpty(putDate) || string.IsNullOrEmpty(putTime) || putDate.Length != 8 || putTime.Length < 6)
                return null;

            int year = int.Parse(putDate[..4]);
            int month = int.Parse(putDate[4..6]);
            int day = int.Parse(putDate[6..8]);
            int hour = int.Parse(putTime[..2]);
            int minute = int.Parse(putTime[2..4]);
            int second = int.Parse(putTime[4..6]);

            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc).ToString("o");
        }
        catch { return null; }
    }

    private void EnsureConnected()
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected. Run 'connect' command first or provide connection options.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _session?.Close();
        _session = null;
        _connection?.Close();
        _connection = null;
    }
}
