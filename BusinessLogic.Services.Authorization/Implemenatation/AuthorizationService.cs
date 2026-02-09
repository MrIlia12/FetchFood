using BusinessLogic.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;
using DataAccess.Repositories.Abstractions;
using DataAccess.Entities;
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
        private readonly ICourierRepository CourierRepository;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="userRepository">Репозиторий пользователей.</param>
        /// <param name="logger">Логгер.</param>
        public AuthorizationService(
        IUserRepository userRepository,
        ICourierRepository courierRepository,
        ILogger<AuthorizationService> logger)
        {
            Logger = logger;
            UserRepository = userRepository;
            CourierRepository = courierRepository;
        }

        /// <summary>
        /// Проверяет, присутствует ли пользователь в базе (авторизован ли).
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <returns>True, если авторизован.</returns>
        public async Task<bool> IsUserAuthorizedAsync(long userId)
        {
            try
            {
                User user = await UserRepository.GetUserByIdAsync(userId);

                if (user is null)
                {
                    return false;
                }
            }
            catch
            {
                throw new Exception("Ошибка обращения к базе данных.");
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
        /// Авторизует курьера (добавляет в базу данных).
        /// </summary>
        /// <param name="user">Пользователь.</param>
        /// <returns>Если успешно - true.</returns>
        public async Task<bool> AuthorizeCourierAsync(long userId)
        {
            bool result;
            try
            {
                Courier courier = await CourierRepository.GetCourierByUserIdAsync(userId);

                if (courier is null)
                {
                    courier = new Courier
                    {
                        IdUser = userId,
                    };

                    return await CourierRepository.AddCourierAsync(courier);
                }
            }
            catch
            {
                throw new Exception("Ошибка обращения к базе данных.");
            }

            return true;
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
        public async Task<UserRole> GetUserRoleAsync(long userId)
        {
            var user = await UserRepository.GetUserByIdAsync(userId);

            return user.Role;
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