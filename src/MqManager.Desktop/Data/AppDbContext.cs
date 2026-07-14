using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace MqManager.Desktop.Data;

public class AppDbContext
{
    private readonly string _dbPath;

    public AppDbContext()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MqManager.Desktop", "app.db"))
    {
    }

    public AppDbContext(string dbPath)
    {
        _dbPath = dbPath;
        Initialize();
    }

    public void Initialize()
    {
        var dir = Path.GetDirectoryName(_dbPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var connection = CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS ServerConfigs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT,
                Host TEXT,
                Port INTEGER DEFAULT 1414,
                Channel TEXT,
                QueueManager TEXT,
                Username TEXT DEFAULT '',
                Password TEXT DEFAULT ''
            )
            """;
        cmd.ExecuteNonQuery();

        using var cmd2 = connection.CreateCommand();
        cmd2.CommandText = """
            CREATE TABLE IF NOT EXISTS CacheQueues (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                ServerKey TEXT NOT NULL
            )
            """;
        cmd2.ExecuteNonQuery();

        using var cmd3 = connection.CreateCommand();
        cmd3.CommandText = "CREATE INDEX IF NOT EXISTS IX_CacheQueues_ServerKey ON CacheQueues(ServerKey)";
        cmd3.ExecuteNonQuery();

        using var cmd4 = connection.CreateCommand();
        cmd4.CommandText = """
            CREATE TABLE IF NOT EXISTS AppSettings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL DEFAULT ''
            )
            """;
        cmd4.ExecuteNonQuery();
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }
}
