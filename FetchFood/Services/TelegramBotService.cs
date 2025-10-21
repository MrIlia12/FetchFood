using BusinessLogic.Services.Authorization.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using FetchFood.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
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

        public TelegramBotService(IAuthorizationService authorizationService, IMenuService menuService)
        {
            _authorizationService = authorizationService;
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
                    await bot.SendMessage(msg.Chat.Id, "Вас не понял... Попробуйте команду /help.", cancellationToken: ct);
                    break;
            }
        }
        private static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
        {
            Console.WriteLine($"[{LogMessages.ERROR}]: {ex.Message}");
            return Task.CompletedTask;
        }

        #region Сервис авторизации
        private async Task RequestContactAsync(long chatId)
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

            await _bot.SendMessage(
                chatId: chatId,
                text: "👋 Welcome! To use this bot, please share your contact information for authorization.",
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
