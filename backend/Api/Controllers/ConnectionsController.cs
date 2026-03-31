using Api.Models;
using Api.Services;
using Contracts;
using Core;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConnectionsController : ControllerBase
{
    private readonly IExplorerService _explorerService;
    private readonly SavedConnectionStore _savedConnectionStore;

    public ConnectionsController(SavedConnectionStore savedConnectionStore, IExplorerService explorerService)
    {
        _savedConnectionStore = savedConnectionStore;
        _explorerService = explorerService;
    }

    [HttpGet]
    public IActionResult List()
    {
        var connections = _savedConnectionStore
            .List()
            .Select(ToDto)
            .ToList();

        return Ok(connections);
    }

    [HttpGet("{id:guid}/databases")]
    public IActionResult ListDatabases(Guid id)
    {
        var connection = _savedConnectionStore.GetById(id);

        if (connection is null)
            return NotFound(new { error = "Saved connection not found." });

        try
        {
            var results = _explorerService.GetDatabases(new ExplorerConnectionRequest
            {
                Server = $"{connection.Server},{connection.Port}",
                Username = connection.Username,
                Password = connection.Password
            });

            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/databases/{database}/tables")]
    public IActionResult ListTables(Guid id, string database)
    {
        var connection = _savedConnectionStore.GetById(id);

        if (connection is null)
            return NotFound(new { error = "Saved connection not found." });

        if (string.IsNullOrWhiteSpace(database))
            return BadRequest(new { error = "Database is required." });

        try
        {
            var results = _explorerService.GetTables(new TableListRequest
            {
                Server = $"{connection.Server},{connection.Port}",
                Database = database,
                Username = connection.Username,
                Password = connection.Password
            });

            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult Save([FromBody] SaveConnectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Server))
            return BadRequest(new { error = "Server is required." });

        if (request.Port <= 0)
            return BadRequest(new { error = "Port must be greater than 0." });

        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { error = "Username is required." });

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Password is required." });

        var savedConnection = _savedConnectionStore.Save(new SavedConnectionRecord
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(request.Name),
            Server = request.Server.Trim(),
            Port = request.Port,
            Username = request.Username.Trim(),
            Password = request.Password,
            TrustServerCertificate = request.TrustServerCertificate
        });

        return Ok(ToDto(savedConnection));
    }

    private static SavedConnectionDto ToDto(SavedConnectionRecord connection)
    {
        var displayName = string.IsNullOrWhiteSpace(connection.Name)
            ? $"{connection.Server},{connection.Port}"
            : connection.Name!;

        return new SavedConnectionDto
        {
            Id = connection.Id,
            Name = connection.Name,
            Server = connection.Server,
            Port = connection.Port,
            Username = connection.Username,
            TrustServerCertificate = connection.TrustServerCertificate,
            DisplayName = displayName,
            IsConnected = true
        };
    }

    private static string? NormalizeName(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
