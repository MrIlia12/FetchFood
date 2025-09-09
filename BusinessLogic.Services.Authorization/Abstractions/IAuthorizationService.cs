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
        Task<bool> IsUserAuthorized(long userId);
        Task<bool> AuthorizeUser(long userId, string authorizationCode);
        Task<bool> RemoveAuthorization(long userId);
    }
}
