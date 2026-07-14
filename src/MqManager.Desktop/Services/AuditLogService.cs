using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MqManager.Desktop.Services;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record AuditLogEntry(
    DateTime Timestamp,
    string Category,
    string Operation,
    string Target,
    bool Success,
    string Details
);

public sealed class AuditLogService
{
    private static readonly AuditLogService _instance = new();
    public static AuditLogService Instance => _instance;

    private readonly List<AuditLogEntry> _entries = [];
    private readonly object _lock = new();

    public event EventHandler<AuditLogEntry>? EntryAdded;

    private AuditLogService() { }

    public void Log(string category, string operation, string target, bool success, string details = "")
    {
        var entry = new AuditLogEntry(DateTime.Now, category, operation, target, success, details);
        lock (_lock)
            _entries.Add(entry);
        EntryAdded?.Invoke(this, entry);
    }

    public IReadOnlyList<AuditLogEntry> GetAll()
    {
        lock (_lock)
            return [.. _entries];
    }
}
