using BusinessLogic.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;
using DataAccess.Repositories.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace BusinessLogic.Services.Authorization
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ILogger<AuthorizationService> Logger;
        private readonly IUserRepository UserRepository;
        private readonly ITelegramBotClient BotClient;

        public AuthorizationService(
        IUserRepository userRepository,
        ILogger<AuthorizationService> logger)
        {
            Logger = logger;
            UserRepository = userRepository;
        }

        public async Task<bool> IsUserAuthorizedAsync(long userId)
        {
            var user = await UserRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return false;
            }

            return true;
        }

        public bool IsUserAuthorized(long userId)
        {
            var requestContactKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[]
                {
                    KeyboardButton.WithRequestContact("📞 Share My Contact")
                }
            })

            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            BotClient.SendMessage(
            chatId: chatId,
            text: "👋 Welcome! To use this bot, please share your contact information for authorization.",
            replyMarkup: requestContactKeyboard);
        }
    }
}