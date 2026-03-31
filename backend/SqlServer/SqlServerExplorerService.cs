using Contracts;
using Core;
using Microsoft.Data.SqlClient;

namespace SqlServer;

public class SqlServerExplorerService : IExplorerService
{
    public List<DatabaseInfo> GetDatabases(ExplorerConnectionRequest request)
    {
        var connectionString =
            $"Server={request.Server};Database=master;User Id={request.Username};Password={request.Password};TrustServerCertificate=True;Encrypt=True;";

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = new SqlCommand(
            """
            SELECT name
            FROM sys.databases
            ORDER BY name
            """,
            connection);

        using var reader = command.ExecuteReader();

        var databases = new List<DatabaseInfo>();

        while (reader.Read())
        {
            databases.Add(new DatabaseInfo
            {
                Name = reader["name"]?.ToString() ?? string.Empty
            });
        }

        return databases;
    }

    public List<TableInfo> GetTables(TableListRequest request)
    {
        var connectionString =
            $"Server={request.Server};Database={request.Database};User Id={request.Username};Password={request.Password};TrustServerCertificate=True;Encrypt=True;";

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = new SqlCommand(
            """
            SELECT
                s.name AS SchemaName,
                t.name AS TableName
            FROM sys.tables t
            INNER JOIN sys.schemas s
                ON t.schema_id = s.schema_id
            ORDER BY s.name, t.name
            """,
            connection);

        using var reader = command.ExecuteReader();

        var tables = new List<TableInfo>();

        while (reader.Read())
        {
            tables.Add(new TableInfo
            {
                Schema = reader["SchemaName"]?.ToString() ?? string.Empty,
                Name = reader["TableName"]?.ToString() ?? string.Empty
            });
        }

        return tables;
    }
}
