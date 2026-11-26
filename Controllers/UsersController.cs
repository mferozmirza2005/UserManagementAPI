using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/users — returns a snapshot of every user. Status 200.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<IEnumerable<User>> GetUsers()
    {
        try
        {
            return Ok(_userService.GetAll());
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to retrieve users.");
            throw;
        }
    }

    /// <summary>
    /// GET /api/users/{id} — 200 if found, 400 for an invalid ID, 404 if missing.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<User> GetUserById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "User ID must be a positive integer." });
            }

            var user = _userService.GetById(id);
            if (user is null)
            {
                return NotFound(new { error = $"User with ID {id} was not found." });
            }

            return Ok(user);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to retrieve user {UserId}.", id);
            throw;
        }
    }

    /// <summary>
    /// POST /api/users — 201 Created, or 400 for invalid/duplicate data.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<User> CreateUser([FromBody] User user)
    {
        try
        {
            if (!TryValidateUser(user, out var error))
            {
                return BadRequest(new { error });
            }

            if (!_userService.TryCreate(user, out var createdUser) || createdUser is null)
            {
                return BadRequest(new { error = "A user with this email address already exists." });
            }

            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to create a user.");
            throw;
        }
    }

    /// <summary>
    /// PUT /api/users/{id} — 204 on success, 400 if invalid/duplicate, 404 if missing.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateUser(int id, [FromBody] User user)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "User ID must be a positive integer." });
            }

            if (user.Id != 0 && user.Id != id)
            {
                return BadRequest(new { error = "The ID in the URL must match the ID in the request body." });
            }

            if (!TryValidateUser(user, out var error))
            {
                return BadRequest(new { error });
            }

            return _userService.TryUpdate(id, user) switch
            {
                UpdateUserResult.NotFound => NotFound(new { error = $"User with ID {id} was not found." }),
                UpdateUserResult.DuplicateEmail => BadRequest(new { error = "A user with this email address already exists." }),
                _ => NoContent()
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update user {UserId}.", id);
            throw;
        }
    }

    /// <summary>
    /// DELETE /api/users/{id} — 204 on success, 400 for an invalid ID, 404 if missing.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteUser(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "User ID must be a positive integer." });
            }

            if (!_userService.Delete(id))
            {
                return NotFound(new { error = $"User with ID {id} was not found." });
            }

            return NoContent();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to delete user {UserId}.", id);
            throw;
        }
    }

    private static bool TryValidateUser(User user, out string? error)
    {
        var fullName = user.FullName.Trim();
        var email = user.Email.Trim();
        var department = user.Department.Trim();

        if (string.IsNullOrWhiteSpace(fullName)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(department))
        {
            error = "Full name, email, and department are required and cannot be blank.";
            return false;
        }

        if (fullName.Length < 2 || fullName.Length > 100)
        {
            error = "Full name must be between 2 and 100 non-whitespace characters.";
            return false;
        }

        if (department.Length < 2 || department.Length > 50)
        {
            error = "Department must be between 2 and 50 non-whitespace characters.";
            return false;
        }

        error = null;
        return true;
    }
}
