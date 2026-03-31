using Contracts;
using Core;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExplorerController : ControllerBase
{
    private readonly IExplorerService _explorerService;

    public ExplorerController(IExplorerService explorerService)
    {
        _explorerService = explorerService;
    }

    [HttpPost("databases")]
    public IActionResult GetDatabases([FromBody] ExplorerConnectionRequest request)
    {
        try
        {
            var results = _explorerService.GetDatabases(request);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("tables")]
    public IActionResult GetTables([FromBody] TableListRequest request)
    {
        try
        {
            var results = _explorerService.GetTables(request);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
