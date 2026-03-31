using Contracts;

namespace Core;

public interface IQueryExecutor
{
    QueryExecutionResponse Execute(QueryExecutionRequest request);
}