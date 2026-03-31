using Api.Services;
using Contracts;
using Core;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly IQueryExecutor _queryExecutor;
    private readonly SavedConnectionStore _savedConnectionStore;

    public QueryController(IQueryExecutor queryExecutor, SavedConnectionStore savedConnectionStore)
    {
        _queryExecutor = queryExecutor;
        _savedConnectionStore = savedConnectionStore;
    }

    [HttpPost("execute")]
    public IActionResult Execute([FromBody] QueryExecutionRequest request)
    {
        var resolvedRequest = ResolveRequest(request);

        if (resolvedRequest is null)
        {
            return NotFound(new { error = "Saved connection not found." });
        }

        var response = _queryExecutor.Execute(resolvedRequest);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    private QueryExecutionRequest? ResolveRequest(QueryExecutionRequest request)
    {
        if (!request.ConnectionId.HasValue)
        {
            return request;
        }

        var connection = _savedConnectionStore.GetById(request.ConnectionId.Value);

        if (connection is null)
        {
            return null;
        }

        return new QueryExecutionRequest
        {
            ConnectionId = request.ConnectionId,
            Server = $"{connection.Server},{connection.Port}",
            Database = request.Database,
            Username = connection.Username,
            Password = connection.Password,
            Sql = request.Sql
        };
    }
}
