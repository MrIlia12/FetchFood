using BusinessLogic.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;
using DataAccess.Repositories.Abstractions;
using DataAccess.Entities;
using Telegram.Bot;

namespace BusinessLogic.Services.Authorization
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ILogger<AuthorizationService> Logger;
        private readonly IUserRepository UserRepository;

        public AuthorizationService(
        IUserRepository userRepository,
        ILogger<AuthorizationService> logger)
        {
            Logger = logger;
            UserRepository = userRepository;
        }

        public async Task<bool> IsUserAuthorizedAsync(long userId)
        {
            User user = await UserRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> AuthorizeUserAsync(User user)
        {
            return await UserRepository.AddUserAsync(user);
        }

        public async Task<bool> RemoveAuthorization(long userId)
        {
            return await UserRepository.RemoveUserByIdAsync(userId);
        }
    }
}