using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

/// <summary>
/// Thread-safe in-memory store. A list preserves insertion order for GET all;
/// a dictionary makes GET by ID O(1). Data resets when the app restarts.
/// </summary>
public class UserService : IUserService
{
    private readonly object _lock = new();
    private readonly List<User> _users;
    private readonly Dictionary<int, User> _usersById;
    private int _nextId = 3;

    public UserService()
    {
        _users =
        [
            new() { Id = 1, FullName = "Amina Khan", Email = "amina.khan@techhive.local", Department = "Engineering" },
            new() { Id = 2, FullName = "Luis Ramirez", Email = "luis.ramirez@techhive.local", Department = "Support" }
        ];

        _usersById = _users.ToDictionary(user => user.Id);
    }

    public IReadOnlyList<User> GetAll()
    {
        lock (_lock)
        {
            return _users.Select(CloneUser).ToList();
        }
    }

    public User? GetById(int id)
    {
        lock (_lock)
        {
            return _usersById.TryGetValue(id, out var user) ? CloneUser(user) : null;
        }
    }

    public bool TryCreate(User user, out User? createdUser)
    {
        var trimmedEmail = user.Email.Trim();

        lock (_lock)
        {
            if (EmailExistsInternal(trimmedEmail))
            {
                createdUser = null;
                return false;
            }

            var newUser = new User
            {
                Id = _nextId++,
                FullName = user.FullName.Trim(),
                Email = trimmedEmail,
                Department = user.Department.Trim()
            };

            _users.Add(newUser);
            _usersById[newUser.Id] = newUser;
            createdUser = CloneUser(newUser);
            return true;
        }
    }

    public UpdateUserResult TryUpdate(int id, User updatedUser)
    {
        var trimmedEmail = updatedUser.Email.Trim();

        lock (_lock)
        {
            if (!_usersById.TryGetValue(id, out var existingUser))
            {
                return UpdateUserResult.NotFound;
            }

            if (EmailExistsInternal(trimmedEmail, excludeUserId: id))
            {
                return UpdateUserResult.DuplicateEmail;
            }

            existingUser.FullName = updatedUser.FullName.Trim();
            existingUser.Email = trimmedEmail;
            existingUser.Department = updatedUser.Department.Trim();
            return UpdateUserResult.Updated;
        }
    }

    public bool Delete(int id)
    {
        lock (_lock)
        {
            if (!_usersById.TryGetValue(id, out var existingUser))
            {
                return false;
            }

            _users.Remove(existingUser);
            _usersById.Remove(id);
            return true;
        }
    }

    private bool EmailExistsInternal(string trimmedEmail, int? excludeUserId = null)
    {
        return _users.Any(user =>
            string.Equals(user.Email, trimmedEmail, StringComparison.OrdinalIgnoreCase)
            && user.Id != excludeUserId);
    }

    private static User CloneUser(User user)
    {
        return new User
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Department = user.Department
        };
    }
}
