using BusinessLogic.Services.Authorization.Abstractions;
using BusinessLogic.Services.MakingOrders.Abstractions;
using BusinessLogic.Services.MakingOrders.Implemenatation;
using DataAccess.Entities;
using FetchFood.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Services
{
    // Версия библиотеки Telegram.Bot: 22.6.2
    internal class TelegramBotService : ITelegramBotService
    {
        private TelegramBotClient _bot;
        private readonly CancellationTokenSource _cts = new();
        private readonly ILogger<MakingOrdersService> _logger;
        private readonly IAuthorizationService _authorizationService;
        private readonly IMakingOrdersService _makingOrdersService;

        public TelegramBotService(IAuthorizationService authorizationService, IMakingOrdersService makingOrdersService)
        {
            _authorizationService = authorizationService;
            _makingOrdersService = makingOrdersService;
        }

        public async Task StartAsync(string token)
        {
            _bot = new TelegramBotClient(token);
            var user = await _bot.GetMe(_cts.Token);
            Console.WriteLine($"@{user.Username} готов к работе.");

            ReceiverOptions receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                _cts.Token
            );
        }

        public Task StopAsync()
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.CallbackQuery is { } callbackQuery)
            {
                await HandleCallbackQueryAsync(bot, callbackQuery, ct);
                return;
            }

            if (update.Message is not { } msg) return;

            if (msg.Type == MessageType.Contact)
            {
                await HandleContactMessage(msg);
                return;
            }

            if (msg.Text is not { } text) return;

            string? command = text.Split(' ')[0];

            switch (command)
            {
                case BotCommands.START:
                    await bot.SendMessage(msg.Chat.Id, "Привет! Меня зовут FetchFood. \nСейчас я проверю, знакомы ли мы. \nТакже Вы можете написать /help, чтобы узнать, что я могу!", cancellationToken: ct);
                    bool isAuthorized = await _authorizationService.IsUserAuthorizedAsync(msg.From.Id);
                    if (!isAuthorized)
                    {
                        // Если не авторизован - просим контакт
                        await RequestContactAsync(msg.Chat.Id);
                        return;
                    }
                    else
                    {
                        // Если авторизован - предлагаем начать оформление заказа
                        await ShowOrderSuggestion(bot, msg.Chat.Id, ct);
                    }
                    break;

                case BotCommands.HELP:
                    await bot.SendMessage(msg.Chat.Id, "Всем привет! Я - бот доставки еды. \r\nПока я ещё совсем молодой и почти ничего не умею, но в будущем смогу отображать меню, помогать с оформлением и отслеживанием заказов.\r\nПожелайте мне успехов в развитии!♥️", cancellationToken: ct);
                    break;

                default:
                    // проверка на то, может ли быть пользователь в процессе оформления заказа
                    OrderProcessingResult orderResult = await _makingOrdersService.ProcessUserInputAsync(msg.From.Id, text);

                    if (orderResult.Success || !string.IsNullOrEmpty(orderResult.Message))
                    {
                        await SendOrderResultMessage(bot, msg.Chat.Id, orderResult, ct);
                    }
                    else
                    {
                        await bot.SendMessage(msg.Chat.Id, "Вас не понял... Попробуйте команду /help.", cancellationToken: ct);
                    }
                    break;
            }
        }

        // Показываем предложение оформить заказ с инлайн-кнопкой
        private async Task ShowOrderSuggestion(ITelegramBotClient bot, long chatId, CancellationToken ct)
        {
            string message = "✅ Вы авторизованы!\n\n" +
                          "🎉 Отлично! Теперь вы можете оформить свой заказ!\n\n" +
                          "Готовы начать?";

            // Создаем инлайн-кнопку
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🛍️ Оформить заказ", "start_order")
                }
            });

            await bot.SendMessage(chatId,
                message,
                replyMarkup: inlineKeyboard,
                cancellationToken: ct);
        }

        // Обработка нажатий на инлайн-кнопки
        private async Task HandleCallbackQueryAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
        {
            try
            {
                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);

                if (callbackQuery.Data == "start_order")
                {
                    // Нажата кнопка "Оформить заказ"
                    await bot.AnswerCallbackQuery(callbackQuery.Id, "Начинаем оформление заказа!", cancellationToken: ct);
                    Message message = new Message
                    {
                        Chat = callbackQuery.Message.Chat,
                        From = callbackQuery.From,
                        Text = callbackQuery.Data,
                    };
                    await HandleOrderCommand(bot, message, ct);
                }
                else if (IsOrderButton(callbackQuery.Data)) // Если нажата кнопка, связанная с оформлением заказа
                {
                    OrderProcessingResult orderResult = await _makingOrdersService.ProcessUserInputAsync(callbackQuery.From.Id, callbackQuery.Data);
                    await SendOrderResultMessage(bot, callbackQuery.Message.Chat.Id, orderResult, ct);
                }
                else
                {
                    await bot.AnswerCallbackQuery(callbackQuery.Id, "Неизвестная команда", cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка в HandleCallbackQueryAsync: {ex}");
                await bot.AnswerCallbackQuery(callbackQuery.Id, "Произошла ошибка", cancellationToken: ct);
            }
        }

        // Проверка является ли callbackQuery кнопкой заказа
        private bool IsOrderButton(string callbackData)
        {
            string[] orderButtons = { "add_comment", "skip_comment", "confirm_order", "cancel_order" };
            return orderButtons.Contains(callbackData);
        }

        // Отправка результата обработки заказа
        private async Task SendOrderResultMessage(ITelegramBotClient bot, long chatId, OrderProcessingResult result, CancellationToken ct)
        {
            if (result.Success)
            {
                if (result.HasInlineKeyboard && result.InlineKeyboard != null)
                {
                    await bot.SendMessage(chatId,
                        result.Message,
                        replyMarkup: result.InlineKeyboard,
                        cancellationToken: ct);
                }
                else
                {
                    await bot.SendMessage(chatId,
                        result.Message,
                        cancellationToken: ct);
                }
            }
            else
            {
                await bot.SendMessage(chatId,
                    result.Message,
                    cancellationToken: ct);
            }
        }

        // Начало оформления заказа
        private async Task HandleOrderCommand(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            // Проверяем авторизацию
            bool isAuthorized = await _authorizationService.IsUserAuthorizedAsync(msg.From.Id);
            if (!isAuthorized)
            {
                await bot.SendMessage(msg.Chat.Id,
                    "❌ Сначала необходимо авторизоваться! Напишите /start",
                    cancellationToken: ct);
                return;
            }

            // TODO: Здесь будет проверка, что корзина не пуста
            // Пока что имитируем, что корзина с товарами пуста
            // Заглушка - потом заменить на реальную проверку
            bool isCartEmpty = false;

            if (isCartEmpty)
            {
                await bot.SendMessage(msg.Chat.Id,
                    "🛒 Ваша корзина пуста!\n\n" +
                    "Добавьте товары в корзину, чтобы оформить заказ.",
                    cancellationToken: ct);
                return;
            }

            // Начинаем процесс оформления заказа
            bool success = await _makingOrdersService.StartOrderCreationAsync(msg.From.Id);
            if (success)
            {
                await bot.SendMessage(msg.Chat.Id,
                    "📝 Введите адрес доставки в формате:\nул. <улица>, д. <номер дома>, кв. <номер квартиры>\n\n" +
                    "Пример: ул. Ленина, д. 15, кв. 42\n\n" +
                    "Допустимые форматы дома: 15, 15а, 15/1, 15/1а",
                    cancellationToken: ct);
            }
            else
            {
                await bot.SendMessage(msg.Chat.Id,
                    "❌ Не удалось начать оформление заказа. Попробуйте позже.",
                    cancellationToken: ct);
            }
        }

        private async Task RequestContactAsync(long chatId)
        {
            ReplyKeyboardMarkup requestContactKeyboard = new ReplyKeyboardMarkup(new[]
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

            await _bot.SendMessage(
                chatId: chatId,
                text: "👋 Welcome! To use this bot, please share your contact information for authorization.",
                replyMarkup: requestContactKeyboard,
                cancellationToken: _cts.Token);
        }

        private async Task HandleContactMessage(Message message)
        {
            DataAccess.Entities.User user = new DataAccess.Entities.User
            {
                TelegramUserId = message.From.Id,
                Name = message.Contact.FirstName,
                PhoneNumber = message.Contact.PhoneNumber,
                Role = DataAccess.Entities.Models.UserRole.User,
            };

            bool result = await _authorizationService.AuthorizeUserAsync(user);

            if (result)
            {
                // Remove the contact request keyboard
                ReplyKeyboardRemove removeKeyboard = new ReplyKeyboardRemove();

                await _bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: "✅ Thank you! Your contact information has been received and saved. " +
                         "Your authorization request is now pending approval. " +
                         "You will be notified once approved.",
                    replyMarkup: removeKeyboard);
            }
            else
            {
                await _bot.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Sorry, there was an error saving your contact information. Please try again later.");
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
        {
            Console.WriteLine($"[{LogMessages.ERROR}]: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
