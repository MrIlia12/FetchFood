using DataAccess.Entities;
using DataAccess.Entities.Models;

namespace BusinessLogic.Services.Authorization.Abstractions
{
    /// <summary>
    /// Сервис работы с авторизацией (интерфейс).
    /// </summary>
    public interface IAuthorizationService
    {
        Task<bool> IsUserAuthorizedAsync(long userId);

        Task<bool> AuthorizeUserAsync(User user);

        Task<bool> RemoveAuthorization(long userId);

        Task<UserRole> GetUserRoleAsync(long userId);

        Task<bool> AuthorizeCourierAsync(long userId);

        Task<bool> IsUserAdministratorAsync(long userId);
    }
}
