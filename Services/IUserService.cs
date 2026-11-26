using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public enum UpdateUserResult
{
    Updated,
    NotFound,
    DuplicateEmail
}

public interface IUserService
{
    IReadOnlyList<User> GetAll();
    User? GetById(int id);
    bool TryCreate(User user, out User? createdUser);
    UpdateUserResult TryUpdate(int id, User user);
    bool Delete(int id);
}
