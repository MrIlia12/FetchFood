using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using FetchFood.Commands;
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
            try
            {
                var message = this.Update.Message;
                var userId = message.From.Id;

            
                if (message.Type is MessageType.Contact)
                {
                    await HandleContactMessage(message);
                }

                var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(userId);
                if (!isAuthorized)
                {
                    // Если не авторизован - просим контакт
                    await RequestContactAsync(message.Chat.Id);
                    return;
                }

                // Проверяем роль пользователя
                var userRole = await _authorizationService.GetUserRoleAsync(userId);
                
                switch (userRole)
                {
                    case DataAccess.Entities.Models.UserRole.Administrator:
                        await GetAdministratorConsoleAsync(message.Chat.Id);
                        break;
                    case DataAccess.Entities.Models.UserRole.Courier:
                        await GetCourierConsoleAsync(message.Chat.Id);
                        break;
                    default:
                        await ShowMenuButton(message.Chat.Id);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка авторизации: {ex}");
                await _bot.SendMessage(
                    chatId: Update.Message.Chat.Id,
                    text: "❌ Произошла ошибка во время авторизации. Пожалуйста, попробуйте позже.");
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

        /// <summary>
        /// Метод отправки команды на отоброжение меню.
        /// </summary>
        /// <param name="update">Информация об обновлении бота.</param>
        /// <returns></returns>
        public async Task ShowMenuButton(long _chatId)
        {
            var menuKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🍔 Меню", "menu:page:0")
                }
            });

            var mssg = "Готов к работе. Нажмите «🍔 Меню», чтобы посмотреть список позиций.";

            await _bot.SendMessage(
                    chatId: _chatId,
                    text: mssg,
                    replyMarkup: menuKeyboard);
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

        /// <summary>
        /// Консоль курьера с кнопкой для просмотра заказов
        /// </summary>
        private async Task GetCourierConsoleAsync(long chatId)
        {
            var courierKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📦 Мои заказы", Commands.CourierCommands.ViewOrders.Command)
                }
            });

            await _bot.SendMessage(
                chatId: chatId,
                text: "🚗 Добро пожаловать, курьер!\n\nНажмите кнопку ниже, чтобы посмотреть активные заказы для доставки.",
                replyMarkup: courierKeyboard);
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
