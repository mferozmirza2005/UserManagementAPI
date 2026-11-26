using Microsoft.AspNetCore.Mvc;

namespace UserManagementAPI.Controllers;

/// <summary>
/// Development-only helper used to verify error-handling middleware.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    [HttpGet("error")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult ThrowError()
    {
        throw new InvalidOperationException("Intentional test exception for error-handling middleware.");
    }
}
