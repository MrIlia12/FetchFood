using BusinessLogic.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;
using DataAccess.Repositories.Abstractions;
using DataAccess.Entities;
using Telegram.Bot;
using DataAccess.Entities.Models;

namespace BusinessLogic.Services.Authorization
{
    /// <summary>
    /// Сервис авторизации.
    /// </summary>
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ILogger<AuthorizationService> Logger;
        private readonly IUserRepository UserRepository;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="userRepository">Репозиторий пользователей.</param>
        /// <param name="logger">Логгер.</param>
        public AuthorizationService(
        IUserRepository userRepository,
        ILogger<AuthorizationService> logger)
        {
            Logger = logger;
            UserRepository = userRepository;
        }

        /// <summary>
        /// Проверяет, присутствует ли пользователь в базе (авторизован ли).
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <returns>True, если авторизован.</returns>
        public async Task<bool> IsUserAuthorizedAsync(long userId)
        {
            var user = await UserRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Авторизует пользователя (добавляет в базу данных).
        /// </summary>
        /// <param name="user">Пользователь.</param>
        /// <returns>Если успешно - true.</returns>
        public async Task<bool> AuthorizeUserAsync(User user)
        {
            return await UserRepository.AddUserAsync(user);
        }

        /// <summary>
        /// Удаляет пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <returns>Если успешно - true.</returns>
        public async Task<bool> RemoveAuthorization(long userId)
        {
            return await UserRepository.RemoveUserByIdAsync(userId);
        }

        /// <summary>
        /// Проверяет, является ли пользователь администратором.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <returns>True, если является администратором.</returns>
        public async Task<bool> IsUserAdministratorAsync(long userId)
        {
            var user = await UserRepository.GetUserByIdAsync(userId);

            if (user.Role is UserRole.Administrator)
            {
                return true;
            }

            return false;
        }
    }
}