using BusinessLogic.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;
using DataAccess.Repositories.Abstractions;
using Telegram.Bot;

namespace BusinessLogic.Services.Authorization
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ILogger<AuthorizationService> Logger;
        private readonly IUserRepository UserRepository;
        private readonly ITeleg

        public AuthorizationService(
        IUserRepository userRepository,
        ILogger<AuthorizationService> logger)
        {
            Logger = logger;
            UserRepository = userRepository;
        }

        public bool IsUserAuthorized(long userId)
        {
            var user 
        }
    }
