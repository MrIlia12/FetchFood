using BusinessLogic.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;
using DataAccess.EntityFramework;

namespace BusinessLogic.Services.Authorization
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ILogger<AuthorizationService> _logger;
        private readonly 

        public AuthorizationService(
        ILogger<AuthorizationService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> IsUserAuthorized(long userId)
        {
            try
            {
                return await _context.AuthorizedUsers
                    .AnyAsync(u => u.TelegramUserId == userId && u.IsActive && u.AuthorizationStatus == "Approved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authorization for user {UserId}", userId);
                return false;
            }
        }
    }
