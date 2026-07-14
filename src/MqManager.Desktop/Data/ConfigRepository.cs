using System;
using System.Collections.Generic;
using MqManager.Core.Models;
using MqManager.Desktop.Models;
using Microsoft.Data.Sqlite;

namespace MqManager.Desktop.Data;

public class ConfigRepository
{
    private readonly AppDbContext _context;

    public ConfigRepository(AppDbContext context)
    {
        _context = context;
    }

    public List<ServerConfig> GetAll()
    {
        var configs = new List<ServerConfig>();
        using var connection = _context.CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Host, Port, Channel, QueueManager, Username, Password FROM ServerConfigs ORDER BY Id";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            configs.Add(new ServerConfig
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Host = reader.GetString(2),
                Port = reader.GetInt32(3),
                Channel = reader.GetString(4),
                QueueManager = reader.GetString(5),
                Username = reader.GetString(6),
                Password = reader.GetString(7)
            });
        }

        return configs;
    }

    public ServerConfig? GetById(int id)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Host, Port, Channel, QueueManager, Username, Password FROM ServerConfigs WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new ServerConfig
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Host = reader.GetString(2),
                Port = reader.GetInt32(3),
                Channel = reader.GetString(4),
                QueueManager = reader.GetString(5),
                Username = reader.GetString(6),
                Password = reader.GetString(7)
            };
        }

        return null;
    }

    public int Save(ServerConfig config)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        if (config.Id == 0)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO ServerConfigs (Name, Host, Port, Channel, QueueManager, Username, Password)
                VALUES (@Name, @Host, @Port, @Channel, @QueueManager, @Username, @Password);
                SELECT last_insert_rowid();
                """;
            AddParameters(cmd, config);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        else
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                UPDATE ServerConfigs
                SET Name = @Name, Host = @Host, Port = @Port, Channel = @Channel,
                    QueueManager = @QueueManager, Username = @Username, Password = @Password
                WHERE Id = @Id
                """;
            cmd.Parameters.AddWithValue("@Id", config.Id);
            AddParameters(cmd, config);
            cmd.ExecuteNonQuery();
            return config.Id;
        }
    }

    public void Delete(int id)
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM ServerConfigs WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    private static void AddParameters(SqliteCommand cmd, ServerConfig config)
    {
        cmd.Parameters.AddWithValue("@Name", config.Name);
        cmd.Parameters.AddWithValue("@Host", config.Host);
        cmd.Parameters.AddWithValue("@Port", config.Port);
        cmd.Parameters.AddWithValue("@Channel", config.Channel);
        cmd.Parameters.AddWithValue("@QueueManager", config.QueueManager);
        cmd.Parameters.AddWithValue("@Username", config.Username);
        cmd.Parameters.AddWithValue("@Password", config.Password);
    }
}
