using BusinessLogic.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Authorization
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly TelegramAuthDbContext _context;
        private readonly ILogger<AuthorizationService> _logger;

        public AuthorizationService(
        ILogger<AuthorizationService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> IsUserAuthorizedAsync(long userId)
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
