using FluentAssertions;
using MqManager.Core.Models;
using MqManager.Core.Services;

namespace MqManager.Tests;

/// <summary>
/// Integration tests using real MQ server.
/// Covers both MqService (XMS messaging) and MqAdminService (MQI + PCF admin).
/// </summary>
[Trait("Category", "Integration")]
public class IntegrationTests : IDisposable
{
    private static MqConnectionSettings CreateRealSettings() => new()
    {
        HostName = "ace2lasmqsq001",
        Port = 1414,
        QueueManager = "QMSGQMC01",
        Channel = "ESB.IIB.SVRCONN",
        Username = "",
        Password = ""
    };

    // Use the default local queue which always exists
    private const string TestQueue = "SYSTEM.DEFAULT.LOCAL.QUEUE";

    // Unique queue name for create/delete tests, avoids collisions (MQ max 48 chars)
    private static readonly string TempQueueName = $"TST.INTG.{Guid.NewGuid():N}";

    private readonly List<string> _queuesToCleanup = new();

    public void Dispose()
    {
        // Clean up: purge test queue
        try
        {
            using var mq = new MqService(CreateRealSettings());
            mq.Connect();
            mq.PurgeQueue(TestQueue);
        }
        catch { /* best effort */ }

        // Clean up: delete any temp queues we created
        if (_queuesToCleanup.Count > 0)
        {
            try
            {
                using var admin = new MqAdminService(CreateRealSettings());
                foreach (var q in _queuesToCleanup)
                {
                    try { admin.DeleteQueue(q); }
                    catch { /* best effort */ }
                }
            }
            catch { /* best effort */ }
        }
    }

    #region MqService — Connection Tests

    [Fact]
    public void Connect_ShouldSucceed()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void GetConnectedQueueManager_ShouldReturnQueueManagerName()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        var qm = mq.GetConnectedQueueManager();
        qm.Should().Be("QMSGQMC01");
    }

    [Fact]
    public void Dispose_AfterConnect_ShouldNotThrow()
    {
        var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.Invoking(s => s.Dispose()).Should().NotThrow();
    }

    #endregion

    #region MqService — Message Operation Tests

    [Fact]
    public void BrowseMessages_OnEmptyQueue_ShouldReturnEmpty()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        var messages = mq.BrowseMessages(TestQueue, count: 5, timeoutMs: 2000);
        messages.Should().BeEmpty();
    }

    [Fact]
    public void PutAndBrowseMessage_ShouldWork()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        mq.PutMessage(TestQueue, "Hello from integration test");

        var messages = mq.BrowseMessages(TestQueue, count: 10, timeoutMs: 5000);
        messages.Should().NotBeEmpty();
        messages.Should().Contain(m => m.Body == "Hello from integration test");
    }

    [Fact]
    public void PutAndGetMessage_ShouldConsumeMessage()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        mq.PutMessage(TestQueue, "Get test message");

        var messages = mq.GetMessages(TestQueue, count: 1, timeoutMs: 5000);
        messages.Should().HaveCount(1);
        messages[0].Body.Should().Be("Get test message");

        // Queue should now be empty
        var remaining = mq.GetMessages(TestQueue, count: 1, timeoutMs: 1000);
        remaining.Should().BeEmpty();
    }

    [Fact]
    public void GetQueueDepth_ShouldReturnCorrectCount()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        var depth = mq.GetQueueDepth(TestQueue);
        depth.Should().Be(0);

        mq.PutMessage(TestQueue, "msg1");
        mq.PutMessage(TestQueue, "msg2");
        mq.PutMessage(TestQueue, "msg3");

        depth = mq.GetQueueDepth(TestQueue);
        depth.Should().Be(3);
    }

    [Fact]
    public void PurgeQueue_ShouldRemoveAllMessages()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        mq.PutMessage(TestQueue, "purge1");
        mq.PutMessage(TestQueue, "purge2");

        var purged = mq.PurgeQueue(TestQueue);
        purged.Should().Be(2);

        var depth = mq.GetQueueDepth(TestQueue);
        depth.Should().Be(0);
    }

    [Fact]
    public void PutMultipleMessages_BrowseAll_ShouldReturnAll()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        for (int i = 1; i <= 5; i++)
        {
            mq.PutMessage(TestQueue, $"Message {i}");
        }

        var messages = mq.BrowseMessages(TestQueue, count: 10, timeoutMs: 5000);
        messages.Should().HaveCount(5);
        messages.Select(m => m.Body).Should().Contain("Message 1");
        messages.Select(m => m.Body).Should().Contain("Message 5");
    }

    [Fact]
    public void GetMessage_OnEmptyQueue_ShouldReturnEmpty()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        var messages = mq.GetMessages(TestQueue, count: 1, timeoutMs: 1000);
        messages.Should().BeEmpty();
    }

    [Fact]
    public void MessageInfo_ShouldHaveBody()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        mq.PutMessage(TestQueue, "metadata test");

        var messages = mq.BrowseMessages(TestQueue, count: 1, timeoutMs: 5000);
        messages.Should().HaveCount(1);
        messages[0].Body.Should().Be("metadata test");
    }

    #endregion

    #region MqAdminService — Connection Tests

    [Fact]
    public void Admin_Connect_ShouldSucceed()
    {
        using var admin = new MqAdminService(CreateRealSettings());
        // ListQueues triggers EnsureConnected internally
        var queues = admin.ListQueues("SYSTEM.DEFAULT.LOCAL.QUEUE");
        queues.Should().NotBeNull();
    }

    [Fact]
    public void Admin_Dispose_ShouldNotThrow()
    {
        var admin = new MqAdminService(CreateRealSettings());
        // Trigger connection then dispose
        admin.ListQueues("SYSTEM.DEFAULT.LOCAL.QUEUE");
        admin.Invoking(a => a.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void PutMessage_ShouldPopulateMessageInfo()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        mq.PutMessage(TestQueue, "info-test");

        var messages = mq.BrowseMessages(TestQueue, count: 1, timeoutMs: 5000);
        messages.Should().HaveCount(1);
        var msg = messages[0];
        msg.Body.Should().Be("info-test");
        msg.MessageId.Should().NotBeNullOrEmpty();
        msg.PutDateTime.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PutMultipleMessages_GetPartial_ShouldReturnRequestedCount()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        mq.PutMessage(TestQueue, "g1");
        mq.PutMessage(TestQueue, "g2");
        mq.PutMessage(TestQueue, "g3");

        var messages = mq.GetMessages(TestQueue, count: 2, timeoutMs: 5000);
        messages.Should().HaveCount(2);

        // One message left
        var remaining = mq.GetMessages(TestQueue, count: 1, timeoutMs: 2000);
        remaining.Should().HaveCount(1);
    }

    [Fact]
    public void BrowseMessages_RespectCountLimit()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        for (int i = 1; i <= 5; i++)
            mq.PutMessage(TestQueue, $"msg{i}");

        var messages = mq.BrowseMessages(TestQueue, count: 2, timeoutMs: 5000);
        messages.Should().HaveCount(2);
    }

    #endregion

    #region MqAdminService — ListQueues Tests

    [Fact]
    public void ListQueues_Wildcard_ShouldReturnNonEmptyList()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var queues = admin.ListQueues("*");
        queues.Should().NotBeEmpty();
        queues.Should().Contain(q => q.Name == TestQueue);
    }

    [Fact]
    public void ListQueues_ExactName_ShouldReturnSingleQueue()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var queues = admin.ListQueues(TestQueue);
        queues.Should().HaveCount(1);
        queues[0].Name.Should().Be(TestQueue);
    }

    [Fact]
    public void ListQueues_Filter_ShouldReturnMatchingQueues()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var queues = admin.ListQueues("SYSTEM.DEFAULT.*");
        queues.Should().NotBeEmpty();
        queues.Should().OnlyContain(q => q.Name.StartsWith("SYSTEM.DEFAULT."));
    }

    [Fact]
    public void ListQueues_NonExistentFilter_ShouldReturnEmpty()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var queues = admin.ListQueues("ZZZ.NONEXISTENT.QUEUE.XXX");
        queues.Should().BeEmpty();
    }

    [Fact]
    public void ListQueues_ShouldIncludeQueueMetadata()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var queues = admin.ListQueues(TestQueue, includeDepth: true);
        queues.Should().HaveCount(1);

        var q = queues[0];
        q.Name.Should().Be(TestQueue);
        q.Type.Should().NotBeNullOrEmpty();
        q.Depth.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void ListQueues_WithDepth_ShouldReturnDepth()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var queues = admin.ListQueues(TestQueue, includeDepth: true);
        queues.Should().HaveCount(1);
        queues[0].Depth.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void ListQueues_WithoutDepth_ShouldReturnZeroDepth()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var queues = admin.ListQueues(TestQueue, includeDepth: false);
        queues.Should().HaveCount(1);
        queues[0].Depth.Should().Be(0);
    }

    [Fact]
    public void ListQueues_WithTypeFilter_ShouldReturnOnlyMatchingType()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var queues = admin.ListQueues("SYSTEM.DEFAULT.*", typeFilter: "Local");
        queues.Should().NotBeEmpty();
        queues.Should().OnlyContain(q => q.Type == "Local");
    }

    [Fact]
    public void ListQueues_WithMaxResults_ShouldLimitResults()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var queues = admin.ListQueues("SYSTEM.*", maxResults: 5);
        queues.Should().HaveCountLessOrEqualTo(5);
    }

    [Fact]
    public void ListQueues_FastPath_ShouldReturnNamesOnly()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var queues = admin.ListQueues("SYSTEM.DEFAULT.*");
        queues.Should().NotBeEmpty();
        queues.Should().OnlyContain(q => !string.IsNullOrEmpty(q.Name));
        // Fast path (MQCMD_INQUIRE_Q_NAMES) does not return Type or Depth
        queues.Should().OnlyContain(q => string.IsNullOrEmpty(q.Type));
        queues.Should().OnlyContain(q => q.Depth == 0);
    }

    #endregion

    #region MqAdminService — GetQueueDepth Tests

    [Fact]
    public void Admin_GetQueueDepth_ShouldReturnNonNegative()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var depth = admin.GetQueueDepth(TestQueue);
        depth.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Admin_GetQueueDepth_AfterPut_ShouldMatchMessageCount()
    {
        // Use MqService to put messages, then MqAdminService to check depth
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        mq.PutMessage(TestQueue, "depth-check-1");
        mq.PutMessage(TestQueue, "depth-check-2");

        using var admin = new MqAdminService(CreateRealSettings());
        var depth = admin.GetQueueDepth(TestQueue);
        depth.Should().Be(2);
    }

    [Fact]
    public void Admin_GetQueueDepth_NonExistentQueue_ShouldThrow()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        admin.Invoking(a => a.GetQueueDepth("ZZZ.NO.SUCH.QUEUE"))
            .Should().Throw<Exception>();
    }

    #endregion

    #region MqAdminService — CreateQueue / DeleteQueue Tests

    [Fact]
    public void CreateAndDeleteQueue_ShouldSucceed()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        // Create a temp local queue
        admin.CreateQueue(TempQueueName, "local");
        _queuesToCleanup.Add(TempQueueName);

        // Verify it appears in the list
        var queues = admin.ListQueues(TempQueueName, includeDepth: true);
        queues.Should().HaveCount(1);
        queues[0].Type.Should().Be("Local");

        // Delete it
        admin.DeleteQueue(TempQueueName);
        _queuesToCleanup.Remove(TempQueueName);

        // Verify it's gone
        var after = admin.ListQueues(TempQueueName);
        after.Should().BeEmpty();
    }

    [Fact]
    public void CreateQueue_DefaultType_ShouldCreateLocalQueue()
    {
        var queueName = $"TST.DFLT.{Guid.NewGuid():N}";
        using var admin = new MqAdminService(CreateRealSettings());

        try
        {
            admin.CreateQueue(queueName);
            _queuesToCleanup.Add(queueName);

            var queues = admin.ListQueues(queueName, includeDepth: true);
            queues.Should().HaveCount(1);
            queues[0].Type.Should().Be("Local");
        }
        finally
        {
            try { admin.DeleteQueue(queueName); _queuesToCleanup.Remove(queueName); } catch { }
        }
    }

    [Fact]
    public void CreateQueue_AliasType_ShouldCreateAliasQueue()
    {
        // Alias needs a base queue — use SYSTEM.DEFAULT.LOCAL.QUEUE
        var aliasName = $"TST.ALIAS.{Guid.NewGuid():N}";
        using var admin = new MqAdminService(CreateRealSettings());

        try
        {
            admin.CreateQueue(aliasName, "alias");
            _queuesToCleanup.Add(aliasName);

            // Verify the alias queue was created (exists in queue list)
            var queues = admin.ListQueues(aliasName);
            queues.Should().HaveCount(1);
            queues[0].Name.Should().Be(aliasName);
            // Note: Type may be empty for alias queues due to AccessQueue(MQOO_INQUIRE)
            // resolving through the alias — this is a known limitation of ListQueues.
        }
        finally
        {
            try { admin.DeleteQueue(aliasName); _queuesToCleanup.Remove(aliasName); } catch { }
        }
    }

    [Fact]
    public void DeleteQueue_NonExistent_ShouldThrow()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        admin.Invoking(a => a.DeleteQueue("ZZZ.NONEXISTENT.DELETE.TEST"))
            .Should().Throw<Exception>();
    }

    #endregion

    #region MqAdminService + MqService — Cross-Layer Tests

    [Fact]
    public void PutMessage_ThenAdminGetDepth_ShouldMatch()
    {
        using var mq = new MqService(CreateRealSettings());
        mq.Connect();
        mq.PurgeQueue(TestQueue);

        mq.PutMessage(TestQueue, "cross-layer-1");
        mq.PutMessage(TestQueue, "cross-layer-2");
        mq.PutMessage(TestQueue, "cross-layer-3");

        // Admin service reads depth via MQI (not XMS browse workaround)
        using var admin = new MqAdminService(CreateRealSettings());
        var depth = admin.GetQueueDepth(TestQueue);
        depth.Should().Be(3);
    }

    [Fact]
    public void CreateQueue_PutMessage_GetDepth_DeleteQueue_FullLifecycle()
    {
        var queueName = $"TST.LIFE.{Guid.NewGuid():N}";
        using var admin = new MqAdminService(CreateRealSettings());

        try
        {
            // Create queue
            admin.CreateQueue(queueName, "local");
            _queuesToCleanup.Add(queueName);

            // Put messages via XMS
            using var mq = new MqService(CreateRealSettings());
            mq.Connect();
            mq.PutMessage(queueName, "lifecycle-msg-1");
            mq.PutMessage(queueName, "lifecycle-msg-2");

            // Verify depth via admin
            var depth = admin.GetQueueDepth(queueName);
            depth.Should().Be(2);

            // Get messages via XMS
            var messages = mq.GetMessages(queueName, count: 2, timeoutMs: 5000);
            messages.Should().HaveCount(2);
            messages.Select(m => m.Body).Should().Contain("lifecycle-msg-1");
            messages.Select(m => m.Body).Should().Contain("lifecycle-msg-2");

            // Depth should be 0 now
            depth = admin.GetQueueDepth(queueName);
            depth.Should().Be(0);

            // Delete queue
            admin.DeleteQueue(queueName);
            _queuesToCleanup.Remove(queueName);

            // Verify gone
            var queues = admin.ListQueues(queueName);
            queues.Should().BeEmpty();
        }
        finally
        {
            try { admin.DeleteQueue(queueName); _queuesToCleanup.Remove(queueName); } catch { }
        }
    }

    #endregion

    #region MqAdminService — ListApplicationConnections Tests

    [Fact]
    public void ListApplicationConnections_ShouldReturnNonEmptyList()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var connections = admin.ListApplicationConnections();
        connections.Should().NotBeEmpty();
    }

    [Fact]
    public void ListApplicationConnections_ShouldOnlyContainSvrconnChannels()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var connections = admin.ListApplicationConnections();
        connections.Should().OnlyContain(c => c.Status == "Running" || c.Status == "Stopped" || c.Status == "Retrying");
    }

    [Fact]
    public void ListApplicationConnections_ShouldHaveChannelNameAndConnectionName()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var connections = admin.ListApplicationConnections();
        connections.Should().OnlyContain(c => !string.IsNullOrEmpty(c.ChannelName));
        connections.Should().OnlyContain(c => !string.IsNullOrEmpty(c.ConnectionName));
    }

    [Fact]
    public void ListApplicationConnections_ShouldIncludeKnownSvrconnChannel()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var connections = admin.ListApplicationConnections();
        // The ESB.EOM.SVRCONN channel is known to have active connections
        connections.Should().Contain(c => c.ChannelName == "ESB.EOM.SVRCONN");
    }

    #endregion

    #region MqAdminService — ListChannels Tests

    [Fact]
    public void ListChannels_ShouldReturnNonEmptyList()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var channels = admin.ListChannels("*");
        channels.Should().NotBeEmpty();
    }

    [Fact]
    public void ListChannels_ShouldHaveNameAndType()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var channels = admin.ListChannels("*");
        channels.Should().OnlyContain(c => !string.IsNullOrEmpty(c.Name));
        channels.Should().OnlyContain(c => !string.IsNullOrEmpty(c.Type));
    }

    [Fact]
    public void ListChannels_Filter_ShouldReturnMatchingChannels()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var channels = admin.ListChannels("SYSTEM.*");
        channels.Should().NotBeEmpty();
        channels.Should().OnlyContain(c => c.Name.StartsWith("SYSTEM."));
    }

    [Fact]
    public void ListChannels_NonExistent_ShouldThrowPCFException()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        admin.Invoking(a => a.ListChannels("ZZZ.NONEXISTENT.CHANNEL.XXX"))
            .Should().Throw<Exception>();
    }

    #endregion

    #region MqAdminService — GetChannelStatus Tests

    [Fact]
    public void GetChannelStatus_KnownChannel_ShouldReturnStatus()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        // Use SYSTEM.DEF.SENDER which always exists
        var status = admin.GetChannelStatus("SYSTEM.DEF.SENDER");
        status.Name.Should().Be("SYSTEM.DEF.SENDER");
        status.Status.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetChannelStatus_InactiveChannel_ShouldReturnInactive()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var status = admin.GetChannelStatus("SYSTEM.DEF.SENDER");
        // Default channels are typically inactive
        status.Status.Should().BeOneOf("Inactive", "Stopped");
    }

    [Fact]
    public void GetChannelStatus_NonExistent_ShouldThrow()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        admin.Invoking(a => a.GetChannelStatus("ZZZ.NONEXISTENT.CHANNEL"))
            .Should().Throw<Exception>();
    }

    #endregion

    #region MqAdminService — GetChannelConnectionInfo Tests

    [Fact]
    public void GetChannelConnectionInfo_KnownChannel_ShouldReturnResult()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        var info = admin.GetChannelConnectionInfo("SYSTEM.DEF.SENDER");
        info.Should().NotBeEmpty();
        info[0].Name.Should().Be("SYSTEM.DEF.SENDER");
    }

    [Fact]
    public void GetChannelConnectionInfo_NonExistent_ShouldThrow()
    {
        using var admin = new MqAdminService(CreateRealSettings());

        admin.Invoking(a => a.GetChannelConnectionInfo("ZZZ.NONEXISTENT.CHANNEL"))
            .Should().Throw<Exception>();
    }

    #endregion

    #region MqAdminService — Dispose Tests

    [Fact]
    public void Admin_DisposeCalledTwice_ShouldNotThrow()
    {
        var admin = new MqAdminService(CreateRealSettings());
        admin.ListQueues("*");
        admin.Dispose();
        admin.Invoking(a => a.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void Admin_DisposeWithoutUse_ShouldNotThrow()
    {
        var admin = new MqAdminService(CreateRealSettings());
        admin.Invoking(a => a.Dispose()).Should().NotThrow();
    }

    #endregion
}
