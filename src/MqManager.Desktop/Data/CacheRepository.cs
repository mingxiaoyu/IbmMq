using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using MqManager.Core.Models;
using MqManager.Desktop.Models;

namespace MqManager.Desktop.Data;

public class CacheRepository
{
    private readonly AppDbContext _context;

    public CacheRepository(AppDbContext context) => _context = context;

    private string GetServerKey(ServerConfig config)
        => $"{config.Host}:{config.Port}:{config.QueueManager}";

    public List<QueueInfo> GetCachedQueues(ServerConfig config)
    {
        using var conn = _context.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Name FROM CacheQueues WHERE ServerKey = @key ORDER BY Name";
        cmd.Parameters.AddWithValue("@key", GetServerKey(config));

        var queues = new List<QueueInfo>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            queues.Add(new QueueInfo { Name = reader.GetString(0) });
        return queues;
    }

    public void SaveQueues(ServerConfig config, List<QueueInfo> queues)
    {
        using var conn = _context.CreateConnection();
        var key = GetServerKey(config);

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM CacheQueues WHERE ServerKey = @key";
            cmd.Parameters.AddWithValue("@key", key);
            cmd.ExecuteNonQuery();
        }

        foreach (var q in queues)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO CacheQueues (Name, ServerKey) VALUES (@name, @key)";
            cmd.Parameters.AddWithValue("@name", q.Name);
            cmd.Parameters.AddWithValue("@key", key);
            cmd.ExecuteNonQuery();
        }
    }

    public void ClearCache(ServerConfig config)
    {
        using var conn = _context.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM CacheQueues WHERE ServerKey = @key";
        cmd.Parameters.AddWithValue("@key", GetServerKey(config));
        cmd.ExecuteNonQuery();
    }
}
