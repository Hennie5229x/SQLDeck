using Contracts;
using Core;
using Microsoft.Data.SqlClient;

namespace SqlServer;

public class SqlServerQueryExecutor : IQueryExecutor
{
    public QueryExecutionResponse Execute(QueryExecutionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Server))
        {
            return Fail("Server is required.", request);
        }

        if (string.IsNullOrWhiteSpace(request.Database))
        {
            return Fail("Database is required.", request);
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return Fail("Username is required.", request);
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Fail("Password is required.", request);
        }

        if (string.IsNullOrWhiteSpace(request.Sql))
        {
            return Fail("SQL text is required.", request);
        }

        var connectionString =
            $"Server={request.Server};Database={request.Database};User Id={request.Username};Password={request.Password};TrustServerCertificate=True;Encrypt=True;";

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var command = new SqlCommand(request.Sql, connection);
            using var reader = command.ExecuteReader();

            var response = new QueryExecutionResponse
            {
                Success = true,
                Server = request.Server,
                Database = request.Database,
                ExecutedSql = request.Sql
            };

            do
            {
                var resultSet = new QueryExecutionResultSet();

                if (reader.FieldCount > 0)
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        resultSet.Columns.Add(reader.GetName(i));
                    }

                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object?>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            row[reader.GetName(i)] = value;
                        }

                        resultSet.Rows.Add(row);
                    }

                    resultSet.Message = $"Returned {resultSet.Rows.Count} row(s).";
                }
                else
                {
                    resultSet.RowsAffected = reader.RecordsAffected;
                    resultSet.Message = resultSet.RowsAffected >= 0
                        ? $"{resultSet.RowsAffected} row(s) affected."
                        : "Command executed successfully.";
                }

                response.ResultSets.Add(resultSet);
            } while (reader.NextResult());

            response.Message = response.ResultSets.Count switch
            {
                0 => "Command executed successfully.",
                1 => "Returned 1 result.",
                _ => $"Returned {response.ResultSets.Count} results."
            };

            return response;
        }
        catch (Exception ex)
        {
            return Fail(ex.Message, request);
        }
    }

    private static QueryExecutionResponse Fail(string message, QueryExecutionRequest request)
    {
        return new QueryExecutionResponse
        {
            Success = false,
            Message = message,
            Server = request.Server,
            Database = request.Database,
            ExecutedSql = request.Sql
        };
    }
}
