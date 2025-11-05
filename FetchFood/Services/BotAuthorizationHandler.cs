using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using BusinessLogic.Services.Authorization.Abstractions;

namespace FetchFood.Services
{
    /// <summary>
    /// Обработчик комманд на авторизацию.
    /// </summary>
    class BotAuthorizationHandler : BotCommandHandler
    {
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// ctor.
        /// </summary>
        public BotAuthorizationHandler(Update update, ITelegramBotClient botClient, IAuthorizationService authorizationService) : base(update, botClient) 
        {
            _authorizationService = authorizationService;
        }

        public override async void Invoke()
        {
            var message = (this.Update ?? throw new ArgumentNullException("Взаимодействие с ботом не может быть нулевым.")).Message;

            var userId = ((message ?? throw new ArgumentNullException("Сообщение не может быть нулевым."))
                .From ?? throw new ArgumentNullException("Пользователь не может отсутствовать."))
                .Id;

            try
            {
                var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(userId);
                if (!isAuthorized)
                {
                    // Если не авторизован - просим контакт
                    await RequestContactAsync(message.Chat.Id);
                    return;
                }

                var isAdministrator = await _authorizationService.IsUserAdministratorAsync(userId);
                if (isAdministrator)
                {
                    GetAdministratorConsoleAsync(message.Chat.Id);
                    return;
                }
            }
            catch
            {
                throw new Exception("Ошибка авторизации.");
            }
        }

        /// <summary>
        /// Метод запроса на предоставления контакта.
        /// </summary>
        /// <param name="chatId">Id чата.</param>
        private async Task RequestContactAsync(long chatId)
        {
            var requestContactKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[]
                {
                    KeyboardButton.WithRequestContact("📞 Поделиться контактом.")
                }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await _bot.SendMessage(
                chatId: chatId,
                text: "👋 Добро пожаловать! Чтобы использовать бота, пожалуйста поделитесь контактом.",
                replyMarkup: requestContactKeyboard);
        }

        private async Task GetAdministratorConsoleAsync(long chatId)
        {
            var requestContactKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    new InlineKeyboardButton("Перейти к списку заказов", "GetOrder")
                }
            });

            await _bot.SendMessage(
                chatId: chatId,
                text: "Ваша роль - администратор.",
                replyMarkup: requestContactKeyboard);
        }

        private async Task HandleContactMessage(Message message)
        {
            var user = new DataAccess.Entities.User
            {
                TelegramUserId = message.From.Id,
                Name = message.Contact.FirstName,
                PhoneNumber = message.Contact.PhoneNumber,
                Role = DataAccess.Entities.Models.UserRole.User,
            };

            var result = await _authorizationService.AuthorizeUserAsync(user);

            if (result)
            {
                // Remove the contact request keyboard
                var removeKeyboard = new ReplyKeyboardRemove();

                await _bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: "✅ Спасибо! Ваша контактная информация успешно сохранена.",
                    replyMarkup: removeKeyboard);
            }
            else
            {
                await _bot.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Произошла ошибка, пожалуйста попробуйте позже.");
            }
        }
    }
}
