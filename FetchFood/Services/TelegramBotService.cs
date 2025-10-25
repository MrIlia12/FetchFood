using BusinessLogic.Services.Authorization.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using FetchFood.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
using BusinessLogic.Services.Administration.Abstraction;
using BusinessLogic.Services.Administration.Models;
using DataAccess.Entities.Models;
using BusinessLogic.Services.Menu.Abstractions;
using DataAccess.Entities.Models;
using DataAccess.Entities;

namespace FetchFood.Services
{
    // Версия библиотеки Telegram.Bot: 22.6.2
    internal class TelegramBotService : ITelegramBotService
    {
        private TelegramBotClient _bot;
        private readonly CancellationTokenSource _cts = new();
        private readonly IAuthorizationService _authorizationService;
        private readonly IMenuService _menuService;
        private readonly IAdministrationService _administrationService;

        private readonly ITelegramBotCartService _cartService;

        public TelegramBotService(IAuthorizationService authorizationService, ITelegramBotCartService cartService, IAdministrationService administrationService)
        public TelegramBotService(IAuthorizationService authorizationService, IMenuService menuService)
        {
            _authorizationService = authorizationService;
            _administrationService = administrationService;
            _cartService = cartService;
            _menuService = menuService;
        }

        public async Task StartAsync(string token)
        {
            _bot = new TelegramBotClient(token);
            Telegram.Bot.Types.User user = await _bot.GetMe(_cts.Token);
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
            // Обработка событий по callback системе.
            if (update.Type is UpdateType.CallbackQuery)
            {
                if (update.CallbackQuery is not { } callBack) return;

                var callBackData = callBack.Data.Split(' ');
                var number = callBackData.Length > 1 ? Convert.ToInt32(callBackData[1]) : 0;

                switch (callBackData[0])
                {
                    case BotCommands.GETORDERS:
                        var order = await _administrationService.GetOrderInformationAsync(number);
                        var keyboard = new InlineKeyboardMarkup();
                        var keyBoardButtons = new List<InlineKeyboardButton>();

                        if (order.OrderPosition is not OrderPosition.First and not OrderPosition.Lonely)
                        {
                            keyBoardButtons.Add(new InlineKeyboardButton("⬅", $"GetOrder {number - 1}"));
                        }

                        keyBoardButtons.Add(new InlineKeyboardButton("Выбрать", $"ToOrderMenu {order.Id} {order.Status}"));

                        if (order.OrderPosition is not OrderPosition.Last and not OrderPosition.Lonely)
                        {
                            keyBoardButtons.Add(new InlineKeyboardButton("➡", $"GetOrder {number + 1}"));
                        }

                        keyboard.AddButtons(keyBoardButtons.ToArray());

                        await _bot.SendMessage(
                            chatId: callBack.Message.Chat.Id,
                            text: "Заказ: " + order.Id + "\n" +
                                    "Пользователь: " + order.UserName + "\n" +
                                    "Статус: " + order.Status + "\n" +
                                    "Цена: " + order.Price + "\n" +
                                    "Дата заказа: " + order.DateOrder,
                            replyMarkup: keyboard);


                        break;

                    case BotCommands.TOORDERMENU:
                        var menuKeyboard = new InlineKeyboardMarkup();
                        var menuKeyboardButtons = new List<InlineKeyboardButton>();

                        if (callBackData[2] != OrderStatus.Complete.ToString())
                        {
                            menuKeyboardButtons.Add(new InlineKeyboardButton("Перевести заказ на следующий этап.", $"NextStep {number}"));
                        }

                        menuKeyboardButtons.Add(new InlineKeyboardButton("Удалить заказ", $"DeleteOrder {number}"));

                        menuKeyboard.AddButtons(menuKeyboardButtons.ToArray());

                        await _bot.SendMessage(
                            chatId: callBack.Message.Chat.Id,
                            text: "Меню заказа: " + number,
                            replyMarkup: menuKeyboard);

                        break;

                    case BotCommands.ORDERNEXTSTEP:
                        if (await _administrationService.ChangeOrderStatus(number))
                        {
                            var afterStepKeyboard = new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    new InlineKeyboardButton("В меню.", "GetOrder")
                                }
                            });

                            await _bot.SendMessage(
                                chatId: callBack.Message.Chat.Id,
                                text: "Операция успешно выполнена",
                                replyMarkup: afterStepKeyboard);
                        }
                        break;

                    case BotCommands.ORDERDELETE:
                        if (await _administrationService.DeleteOrderAsync(number))
                        {
                            var afterDeleteKeyboard = new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    new InlineKeyboardButton("В меню.", "GetOrder")
                                }
                            });

                            await _bot.SendMessage(
                                chatId: callBack.Message.Chat.Id,
                                text: "Операция успешно выполнена",
                                replyMarkup: afterDeleteKeyboard);
                        }
                        break;
                }
            }

            if (update.CallbackQuery is { } cq)
            {
                await HandleCallbackAsync(bot, cq, ct);
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

                    var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(msg.From.Id);
                    if (!isAuthorized)
                    {
                        await RequestContactAsync(msg.Chat.Id);
                        return;
                    }

                    var isAdministrator = await _authorizationService.IsUserAdministratorAsync(msg.From.Id);
                    if (isAdministrator)
                    {
                        GetAdministratorConsoleAsync(msg.Chat.Id);
                        return;
                    }


                    await _cartService.ShowMainMenuAsync(bot, msg.Chat.Id, ct);
                    break;

                case BotCommands.HELP:
                    await bot.SendMessage(msg.Chat.Id, "Всем привет! Я - бот доставки еды. \r\nПока я ещё совсем молодой и почти ничего не умею, но в будущем смогу отображать меню, помогать с оформлением и отслеживанием заказов.\r\nПожелайте мне успехов в развитии!♥️", cancellationToken: ct);
                    break;

                case BotCommands.MENU:
                    // тут будем выдавать меню
                    await HandleMenuCommandAsync(bot, msg, ct);
                    break;

                case BotCommands.FIND:
                    // тут ищем позицию по части её имени
                    string findArgs = text.Length > command.Length
                    ? text[command.Length..].Trim()
                    : string.Empty;

                    await HandleFindCommandAsync(bot, msg, findArgs, ct);
                    break;

                case BotCommands.ADDPOS:
                    string addArgs = text.Length > command.Length ? text[command.Length..].Trim() : string.Empty;
                    await HandleAddPosCommandAsync(bot, msg, addArgs, ct);
                    break;

                case BotCommands.DELPOS:
                    string delArgs = text.Length > command.Length ? text[command.Length..].Trim() : string.Empty;
                    await HandleDelPosCommandAsync(bot, msg, delArgs, ct);
                    break;

                default:
                    await _cartService.HandleMessageAsync(bot, msg, ct);
                    break;
            }
        }
        private static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
        {
            Console.WriteLine($"[{LogMessages.ERROR}]: {ex.Message}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Метод запроса на предоставления контакта.
        /// </summary>
        /// <param name="chatId">Id чата.</param>
        #region Сервис авторизации
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
        #endregion
        #region Сервис меню
        private async Task HandleMenuCommandAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            // (опционально) пускаем только авторизованных
            //var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(msg.From.Id);
            //if (!isAuthorized)
            //{
            //    await RequestContactAsync(msg.Chat.Id);
            //    return;
            //}

            var positions = await _menuService.GetActivePositionsAsync(ct);

            if (positions.Count == 0)
            {
                await bot.SendMessage(msg.Chat.Id, "Пока нет доступных позиций 🙈", cancellationToken: ct);
                return;
            }

            // отображаем меню по 2 кнопки в ряд
            var rows = positions
                .Select(p => InlineKeyboardButton.WithCallbackData(
                    $"{p.Name} — {FormatPrice(p.Price)}", $"pos:{p.PositionId}"))
                .Chunk(2)
                .Select(r => r.ToArray())
                .ToArray();

            await bot.SendMessage(
                chatId: msg.Chat.Id,
                text: "Меню:\nВыберите позицию, чтобы посмотреть детали.",
                replyMarkup: new InlineKeyboardMarkup(rows),
                cancellationToken: ct);
        }

        private async Task HandleFindCommandAsync(ITelegramBotClient bot, Message msg, string query, CancellationToken ct)
        {
            //var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(msg.From.Id);
            //if (!isAuthorized)
            //{
            //    await RequestContactAsync(msg.Chat.Id);
            //    return;
            //}

            if (string.IsNullOrWhiteSpace(query))
            {
                await bot.SendMessage(
                    msg.Chat.Id,
                    "Формат команды: /find <часть названия>\nНапример: /find бургер",
                    cancellationToken: ct);
                return;
            }

            var results = await _menuService.SearchPositionsAsync(query, true, ct);
            if (results.Count == 0)
            {
                await bot.SendMessage(msg.Chat.Id, $"Ничего не нашёл по запросу: “{query}”", cancellationToken: ct);
                return;
            }

            // Чтобы не перегрузить сообщение — ограничим, например, 20 кнопками
            int take = Math.Min(20, results.Count);
            var rows = results
                .Take(take)
                .Select(p => InlineKeyboardButton.WithCallbackData(
                    $"{p.Name} — {FormatPrice(p.Price)}", $"pos:{p.PositionId}"))
                .Chunk(2)
                .Select(r => r.ToArray())
                .ToArray();

            await bot.SendMessage(
                msg.Chat.Id,
                $"Нашёл {results.Count} позиций. Показано {take}. Выберите нужную:",
                replyMarkup: new InlineKeyboardMarkup(rows),
                cancellationToken: ct);
        }
        private async Task HandleAddPosCommandAsync(ITelegramBotClient bot, Message msg, string args, CancellationToken ct)
        {
            // (опционально) Разрешить только авторизованным/админам
            //var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(msg.From!.Id);
            //if (!isAuthorized)
            //{
            //    await bot.SendMessage(msg.Chat.Id, "Команда недоступна. Отправьте контакт через /start.", cancellationToken: ct);
            //    return;
            //}

            // Ожидаемый формат: /addpos Имя;Цена;[ImageUrl]
            if (string.IsNullOrWhiteSpace(args))
            {
                await bot.SendMessage(msg.Chat.Id,
                    "Формат: /addpos Имя;Цена;[ImageUrl]\nНапр.: /addpos Бургер;199.9;https://img",
                    cancellationToken: ct);
                return;
            }

            var parts = args.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length < 2)
            {
                await bot.SendMessage(msg.Chat.Id,
                    "Нужно указать значения полей Имя и Цена. Пример: /addpos Бургер;199.9;[тут может быть картинка]",
                    cancellationToken: ct);
                return;
            }

            var name = parts[0];
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            {
                await bot.SendMessage(msg.Chat.Id, "Имя обязательно и ≤ 100 символов.", cancellationToken: ct);
                return;
            }

            if (!decimal.TryParse(parts[1],
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var price) || price <= 0)
            {
                await bot.SendMessage(msg.Chat.Id,
                    "Цена некорректна. Используйте десятичную Точку: 199.9",
                    cancellationToken: ct);
                return;
            }

            var image = parts.Length >= 3 ? parts[2] : null;

            var pos = new Position
            {
                Name = name.Trim(),
                Price = price,
                Status = PositionStatus.Active,
                Image = string.IsNullOrWhiteSpace(image) ? null : image.Trim()
            };
            try
            {
                if(!await _menuService.CreateAsync(pos, ct))
                {
                    await _bot.SendMessage(msg.Chat.Id,
                    "❌ Не удалось добавить позицию. Попробуйте позже.");
                    Console.WriteLine($"[AddPos ERROR]: Ошибка БД.");
                }

                await bot.SendMessage(msg.Chat.Id,
                    $"✅ Добавлено: #{pos.PositionId} • {pos.Name} — {pos.Price:0.##}",
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                await _bot.SendMessage(msg.Chat.Id,
                    "❌ Не удалось добавить позицию. Попробуйте позже.");
                Console.WriteLine($"[AddPos ERROR]: {ex}"); 
            }
            
        }

        private async Task HandleDelPosCommandAsync(ITelegramBotClient bot, Message msg, string args, CancellationToken ct)
        {
            var query = args?.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                await bot.SendMessage(
                    msg.Chat.Id,
                    "Формат: /delpos <название>\nНапр.: /delpos Бургер",
                    cancellationToken: ct);
                return;
            }

            try
            {
                // ищем по всему списку позиций 
                var matches = await _menuService.SearchPositionsAsync(query, false, ct);

                if (matches.Count == 0)
                {
                    await bot.SendMessage(msg.Chat.Id, "❌ Позиции не найдены.", cancellationToken: ct);
                    return;
                }

                // если совпадение одно — удаляем
                if (matches.Count == 1)
                {
                    var p = matches[0];
                    var ok = await _menuService.DeleteAsync(p.PositionId, ct);
                    await bot.SendMessage(
                        msg.Chat.Id,
                        ok ? $"🗑️ Удалено: {p.Name} (#{p.PositionId})" : "Не удалось удалить позицию.",
                        cancellationToken: ct);
                    return;
                }

                // несколько совпадений — пробуем точное совпадение по имени (без учёта регистра)
                var exact = matches.FirstOrDefault(p =>
                    string.Equals(p.Name, query, StringComparison.OrdinalIgnoreCase));

                if (exact is not null)
                {
                    var ok = await _menuService.DeleteAsync(exact.PositionId, ct);
                    await bot.SendMessage(
                        msg.Chat.Id,
                        ok ? $"🗑️ Удалено: {exact.Name} (#{exact.PositionId})" : "Не удалось удалить позицию.",
                        cancellationToken: ct);
                    return;
                }

                // иначе — просим уточнить (покажем до 10 вариантов)
                var list = string.Join("\n", matches.Take(10).Select(p => $"#{p.PositionId}: {p.Name} ({p.Price:F2})"));
                await bot.SendMessage(
                    msg.Chat.Id,
                    $"Найдено несколько позиций:\n{list}\n\n" +
                    "Уточните название (напр.: `/delpos Бургер классик`) ",
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                await bot.SendMessage(msg.Chat.Id, "⚠️ Ошибка при удалении позиции.", cancellationToken: ct);
                Console.WriteLine($"[DelPos ERROR]: {ex}");
            }
        }

        private async Task HandleCallbackAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
        {
            if (cq.Data is null) return;

            if (cq.Data.Equals("action:back_to_menu", StringComparison.OrdinalIgnoreCase))
            {
                // показать меню заново
                await HandleMenuCommandAsync(bot, cq.Message!, ct);
                await bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);
                return;
            }

            if (cq.Data.StartsWith("pos:", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(cq.Data.AsSpan(4), out var id))
                {
                    await bot.AnswerCallbackQuery(cq.Id, "Некорректный идентификатор.", cancellationToken: ct);
                    return;
                }

                var pos = await _menuService.GetPositionAsync(id, ct);
                if (pos is null || pos.Status != PositionStatus.Active)
                {
                    await bot.AnswerCallbackQuery(cq.Id, "Эта позиция недоступна 😔", cancellationToken: ct);
                    return;
                }

                // С картинкой ещё предстоит разобраться.. Пока без картинки.
                await bot.SendMessage(
                        chatId: cq.Message!.Chat.Id,
                        text: FormatPositionCaption(pos),
                        replyMarkup: PositionActionsKeyboard(pos),
                        cancellationToken: ct);

                await bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);
            }
        }

        private static string FormatPrice(decimal price) => $"{price:0.##}";

        private static string FormatPositionCaption(Position p)
        {
            var name = (p.Name ?? "").Trim();
            var price = FormatPrice(p.Price);
            return $"{name}\nЦена: {price}";
        }

        private static InlineKeyboardMarkup PositionActionsKeyboard(Position p)
        {
            var buttons = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад к меню", "action:back_to_menu"),
                }
            };
            return new InlineKeyboardMarkup(buttons);
        }

        #endregion
    }
}
