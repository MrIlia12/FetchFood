using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Authorization.Abstractions
{
    /// <summary>
    /// Сервис работы с авторизацией (интерфейс).
    /// </summary>
    public interface IAuthorizationService
    {
        Task<bool> IsUserAuthorizedAsync(long userId);
        Task<bool> AuthorizeUserAsync(long userId, string authorizationCode);
        Task<bool> RemoveAuthorizationAsync(long userId);
    }
}
