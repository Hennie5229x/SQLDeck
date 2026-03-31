using System.Text.Json;
using Api.Models;

namespace Api.Services;

public class SavedConnectionStore
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public SavedConnectionStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDirectory);

        _filePath = Path.Combine(dataDirectory, "connections.json");

        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, "[]");
        }
    }

    public IReadOnlyList<SavedConnectionRecord> List()
    {
        lock (_sync)
        {
            return ReadAll();
        }
    }

    public SavedConnectionRecord? GetById(Guid id)
    {
        lock (_sync)
        {
            var connections = ReadAll();
            return connections.FirstOrDefault(connection => connection.Id == id);
        }
    }

    public SavedConnectionRecord Save(SavedConnectionRecord connection)
    {
        lock (_sync)
        {
            var connections = ReadAll();

            var existing = connections.FirstOrDefault(item =>
                string.Equals(item.Server, connection.Server, StringComparison.OrdinalIgnoreCase) &&
                item.Port == connection.Port &&
                string.Equals(item.Username, connection.Username, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                existing.Name = connection.Name;
                existing.Password = connection.Password;
                existing.TrustServerCertificate = connection.TrustServerCertificate;

                WriteAll(connections);
                return existing;
            }

            connections.Add(connection);
            WriteAll(connections);

            return connection;
        }
    }

    private List<SavedConnectionRecord> ReadAll()
    {
        var json = File.ReadAllText(_filePath);

        return JsonSerializer.Deserialize<List<SavedConnectionRecord>>(json, _jsonOptions) ?? [];
    }

    private void WriteAll(List<SavedConnectionRecord> connections)
    {
        var json = JsonSerializer.Serialize(connections, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
