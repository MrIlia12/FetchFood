using BusinessLogic.Services.Menu.Abstractions;
using DataAccess.Entities.Models;
using DataAccess.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;

namespace FetchFood.Services
{
    class BotMenuHandler : BotCommandHandler
    {
        private readonly IMenuService _menuService;
        public BotMenuHandler(Update update, ITelegramBotClient botClient, IMenuService menuService) : base(update, botClient)
        {
            _menuService = menuService;
        }
        public override async void Invoke()
        {
            string? data = string.Empty;
            long chatId;
            // если получен сигнал от кнопки
            if (Update.CallbackQuery != null)
            {
                var callbackQuery = Update.CallbackQuery;
                chatId = callbackQuery.Message.Chat.Id;
                data = callbackQuery.Data;
            }

            // если получено текстовое сообщение
            else if (Update.Message != null)
            {
                var mssg = Update.Message;
                chatId = mssg.Chat.Id;
                data = mssg.Text;
            }
            else
            {
                // хз, что пришло.
                return;
            }
            await HandleMenuCommandAsync(_bot, chatId, data);
        }
        #region Сервис меню
        private static InlineKeyboardMarkup UserMainInlineKeyboard()
        {
            // Кнопка иеню для авторизованного пользователя:
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🍔 Меню", "menu:page:0")
                }
            });
        }

        public async Task HandleMenuCommandAsync(ITelegramBotClient bot, long _chatId, string message, CancellationToken ct = default)
        {

            var messageSplitted = message.Split(':', 3, StringSplitOptions.TrimEntries);
            // если пришла просто команда "menu"
            if (messageSplitted.Length <= 1)
            {
                await HandleMenuPageCommandAsync(bot, _chatId, 0, ct);
                return;
            }

            string action = messageSplitted[1];
            string args = messageSplitted.Length >= 3 ? messageSplitted[2] : BotCommands.EMPTY;
            switch (action)
            {
                // отобразить страницу меню по номеру
                case BotCommands.PAGE:
                    if (args != BotCommands.EMPTY && int.TryParse(args, out var pageNum))
                    {
                        await HandleMenuPageCommandAsync(bot, _chatId, pageNum, ct);
                    }
                    else
                    {
                        await bot.SendMessage(
                        chatId: _chatId,
                        text:
                            "Не удалось распознать номер страницы меню. Проверьте правильность команды.",
                        cancellationToken: ct);
                    }
                    break;
                // показать информацию о позиции по номеру
                case BotCommands.POSITION:
                    if (args != BotCommands.EMPTY && int.TryParse(args, out var posNum))
                    {
                        await HandlePositionCallbackAsync(bot, _chatId, posNum, ct);
                    }
                    else
                    {
                        await bot.SendMessage(
                        chatId: _chatId,
                        text:
                            "Не удалось распознать номер позиции меню. Проверьте правильность команды.",
                        cancellationToken: ct);
                    }
                    break;

                // нажата кнопка "добавить"
                case BotCommands.ADD:
                    await bot.SendMessage(
                        chatId: _chatId,
                        text:
                            "Чтобы добавить позицию:\n" +
                            $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:Имя;Цена(руб.);Состав;Описание;[ImageUrl]\n\n" +
                            "Пример:\n" +
                            $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:Бургер;199.9;булка, котлета, сыр;" +
                            "Сочный бургер;https://img",
                        cancellationToken: ct);
                    break;

                // добавить позицию
                case BotCommands.ADD_POSITION:
                    if (args != BotCommands.EMPTY)
                    {
                        await HandleAddPosCommandAsync(bot, _chatId, args, ct);
                    }
                    break;

                // нажата кнопка "Удалить"
                case BotCommands.DELETE:
                    await bot.SendMessage(
                        chatId: _chatId,
                        text:
                            "Чтобы удалить позицию:\n" +
                            $"{BotCommands.MENU}:{BotCommands.DELETE_POSITION}:<название>\n\n" +
                            "Пример:\n" +
                            $"{BotCommands.MENU}:{BotCommands.DELETE_POSITION}:Бургер",
                        cancellationToken: ct);
                    break;

                // удалить позицию
                case BotCommands.DELETE_POSITION:
                    if (args != BotCommands.EMPTY)
                    {
                        await HandleDelPosCommandAsync(bot, _chatId, args, ct);
                    }
                    break;

                // отобразить искомые позиции
                case BotCommands.FIND:
                    if (args != BotCommands.EMPTY)
                    {
                        await HandleFindCommandAsync(bot, _chatId, args, ct);
                    }
                    break;

                // "назад в меню"
                case BotCommands.BACK:
                    await HandleMenuPageCommandAsync(bot, _chatId, 0, ct);
                    break;
                default:
                    await bot.SendMessage(
                        chatId: _chatId,
                        text:
                            "Неизвестная команда управления меню.",
                        cancellationToken: ct);
                    break;
            }
            return;
        }

        private async Task HandleMenuPageCommandAsync(ITelegramBotClient bot, long chatId, int page, CancellationToken ct)
        {
            var positions = await _menuService.GetActivePositionsAsync(ct);
            int total = 0;
            int totalPages = 0;
            int skip = 0;
            int pageSize = GlobalParams.MENU_ITEMS_CNT;
            if (positions.Count == 0)
            {
                await bot.SendMessage(chatId, "Пока нет доступных позиций.", cancellationToken: ct);
            }
            else
            {
                positions = positions
                    .OrderBy(p => p.Name)
                    .ToList();

                total = positions.Count;
                totalPages = (int)Math.Ceiling(total / (double)pageSize);
                if (totalPages == 0) totalPages = 1;
                if (page < 0) page = 0;
                if (page >= totalPages) page = totalPages - 1;
                skip = page * pageSize;
            }
            var pageItems = positions.Skip(skip).Take(pageSize).ToList();
            var itemButtons = pageItems
                    .Select(p =>
                        InlineKeyboardButton.WithCallbackData(
                            $"{p.Name} — {p.Price:0.##}",
                            $"{BotCommands.MENU}:{BotCommands.POSITION}:{p.PositionId}"))
                    .Chunk(2)
                    .Select(r => r.ToArray())
                    .ToList();

            var navRow = new List<InlineKeyboardButton>();
            if (page > 0)
                navRow.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"{BotCommands.MENU}:{BotCommands.PAGE}:{page - 1}"));
            //else
            // navRow.Add(InlineKeyboardButton.WithCallbackData(" ", $"{BotCommands.MENU}:{BotCommands.DO_NOTHING}"));

            if (page < totalPages - 1)
                navRow.Add(InlineKeyboardButton.WithCallbackData("Далее ➡️", $"{BotCommands.MENU}:{BotCommands.PAGE}:{page + 1}"));
            //else
            // navRow.Add(InlineKeyboardButton.WithCallbackData(" ", $"{BotCommands.MENU}:{BotCommands.DO_NOTHING}"));

            var actionRow = new[]
            {
            InlineKeyboardButton.WithCallbackData("➕ Добавить", $"{BotCommands.MENU}:{BotCommands.ADD}"),
            InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"{BotCommands.MENU}:{BotCommands.DELETE}")
            };

            var rows = new List<InlineKeyboardButton[]>();
            rows.AddRange(itemButtons);
            rows.Add(navRow.ToArray());
            rows.Add(actionRow);

            var markup = new InlineKeyboardMarkup(rows);

            string header = $"Меню (стр. {page + 1}/{totalPages}):\nВыберите позицию, чтобы посмотреть детали.";

            await bot.SendMessage(
                chatId: chatId,
                text: header,
                replyMarkup: markup,
                cancellationToken: ct);
        }

        private async Task HandleFindCommandAsync(ITelegramBotClient bot, long chatId, string query, CancellationToken ct)
        {
            //var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(chatId);
            //if (!isAuthorized)
            //{
            //    await RequestContactAsync(chatId);
            //    return;
            //}

            if (string.IsNullOrWhiteSpace(query))
            {
                await bot.SendMessage(
                    chatId,
                    $"Формат команды: {BotCommands.MENU}:{BotCommands.FIND}:<часть названия>\nНапример: {BotCommands.MENU}:{BotCommands.FIND}:бургер",
                    cancellationToken: ct);
                return;
            }

            var results = await _menuService.SearchPositionsAsync(query, true, ct);
            if (results.Count == 0)
            {
                await bot.SendMessage(chatId, $"Ничего не нашёл по запросу: “{query}”", cancellationToken: ct);
                return;
            }

            // Чтобы не перегрузить сообщение — ограничим, например, 20 кнопками
            int take = Math.Min(20, results.Count);
            var rows = results
                .Take(take)
                .Select(p => InlineKeyboardButton.WithCallbackData(
                    $"{p.Name} — {FormatPrice(p.Price)}", $"{BotCommands.MENU}:{BotCommands.POSITION}:{p.PositionId}"))
                .Chunk(2)
                .Select(r => r.ToArray())
                .ToArray();

            await bot.SendMessage(
                chatId,
                $"Нашёл {results.Count} позиций. Показано {take}. Выберите нужную:",
                replyMarkup: new InlineKeyboardMarkup(rows),
                cancellationToken: ct);
        }
        private async Task HandleAddPosCommandAsync(ITelegramBotClient bot, long chatId, string args, CancellationToken ct)
        {
            // (опционально) Разрешить только авторизованным/админам
            //bool isAuthorized = await _authorizationService.IsUserAuthorizedAsync(msg.From!.Id);
            //bool isAdmin = await _authorizationService.IsUserAdministratorAsync(msg.From!.Id);
            //if (!isAuthorized)
            //{
            //    await bot.SendMessage(chatId, "Команда недоступна. Авторизуйтесь через /start.", cancellationToken: ct);
            //    return;
            //}
            //if (!isAdmin)
            //{
            //    await bot.SendMessage(chatId, "Недостаточно прав доступа. Обратитесь в службу поддержки.", cancellationToken: ct);
            //    return;
            //}

            // Ожидаемый формат: /addpos Имя;Цена;Состав;Описание;[ImageUrl]
            if (string.IsNullOrWhiteSpace(args))
            {
                await bot.SendMessage(chatId,
                    $"Формат: {BotCommands.MENU}:{BotCommands.ADD_POSITION}:Имя;Цена(руб.);Состав;Описание;[ImageUrl]\n" +
                    $"Например:\n{BotCommands.MENU}:{BotCommands.ADD_POSITION}:Бургер;199.9;ингредиент1,ингредиент2;" +
                    "Пара слов о блюде.;https://img",
                    cancellationToken: ct);
                return;
            }

            var parts = args.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length < 4)
            {
                await bot.SendMessage(chatId,
                    "Нужно указать корректные значения полей Имя и Цена. Состав и описание являются опциональными параметрами.\n" +
                    $"Например: {BotCommands.MENU}:{BotCommands.ADD_POSITION}:Бургер;199.9;-;-;[тут может быть картинка]",
                    cancellationToken: ct);
                return;
            }
            // Парсим имя
            string name = parts[0];
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            {
                await bot.SendMessage(chatId, "Имя обязательно и ≤ 100 символов.", cancellationToken: ct);
                return;
            }
            // проверяем на дубликат имени
            {
                var existing = await _menuService.SearchPositionsAsync(name, false, ct);
                bool alreadyExists = existing.Any(p =>
                    p.Status == PositionStatus.Active &&
                    string.Equals(p.Name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));

                if (alreadyExists)
                {
                    await bot.SendMessage(
                        chatId,
                        $"❌ Позиция «{name}» уже существует. Дубликаты не добавляю.",
                        cancellationToken: ct);
                    return;
                }
            }

            // Проверяем цену на соответствие формату
            if (!decimal.TryParse(parts[1],
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var price) || price <= 0)
            {
                await bot.SendMessage(chatId,
                    "Цена некорректна. Используйте десятичную Точку: 199.9",
                    cancellationToken: ct);
                return;
            }
            // Парсим состав
            string? ingredients = parts.Length >= 2 ? parts[2] : null;

            // Парсим описание
            string? description = parts.Length >= 3 ? parts[3] : null;

            // Парсим картинку, если есть
            string? image = parts.Length == 5 ? parts[4] : null;

            var pos = new Position
            {
                Name = name.Trim(),
                Price = price,
                Status = PositionStatus.Active,
                Ingredients = string.IsNullOrEmpty(ingredients) ? null : ingredients.Trim(),
                Description = string.IsNullOrEmpty(description) ? null : description.Trim(),
                Image = string.IsNullOrWhiteSpace(image) ? null : image.Trim()
            };
            try
            {
                if (!await _menuService.CreateAsync(pos, ct))
                {
                    await bot.SendMessage(chatId,
                    "❌ Не удалось добавить позицию. Попробуйте позже.", replyMarkup: UserMainInlineKeyboard());
                    Console.WriteLine($"[AddPos ERROR]: Ошибка БД.");
                }

                await bot.SendMessage(chatId,
                    $"✅ Добавлено: #{pos.PositionId} • {pos.Name} — {pos.Price:0.##}",
                    replyMarkup: UserMainInlineKeyboard(),
                    cancellationToken: ct);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException)
            {
                Console.WriteLine($"ChatId: {chatId}");
            }
            catch (Exception ex)
            {
                await bot.SendMessage(chatId,
                    "❌ Не удалось добавить позицию. Попробуйте позже.", replyMarkup: UserMainInlineKeyboard());
                Console.WriteLine($"[AddPos ERROR]: {ex}");
            }

        }

        private async Task HandleDelPosCommandAsync(ITelegramBotClient bot, long chatId, string args, CancellationToken ct)
        {
            var query = args?.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                await bot.SendMessage(
                    chatId,
                    $"Формат: {BotCommands.MENU}:{BotCommands.DELETE_POSITION}:<название>\nНапример: {BotCommands.MENU}:{BotCommands.DELETE_POSITION}:Бургер",
                    cancellationToken: ct);
                return;
            }

            try
            {
                // ищем по всему списку позиций 
                var matches = await _menuService.SearchPositionsAsync(query, false, ct);

                if (matches.Count == 0)
                {
                    await bot.SendMessage(chatId, "❌ Позиции не найдены.", cancellationToken: ct);
                    return;
                }

                // если совпадение одно — удаляем
                if (matches.Count == 1)
                {
                    var p = matches[0];
                    var ok = await _menuService.DeleteAsync(p.PositionId, ct);
                    await bot.SendMessage(
                        chatId,
                        ok ? $"🗑️ Удалено: {p.Name} (#{p.PositionId})" : "Не удалось удалить позицию.",
                        replyMarkup: UserMainInlineKeyboard(),
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
                        chatId,
                        ok ? $"🗑️ Удалено: {exact.Name} (#{exact.PositionId})" : "Не удалось удалить позицию.",
                        cancellationToken: ct);
                    return;
                }

                // иначе — просим уточнить (покажем до 10 вариантов)
                var list = string.Join("\n", matches.Take(10).Select(p => $"#{p.PositionId}: {p.Name} ({p.Price:F2})"));
                await bot.SendMessage(
                    chatId,
                    $"Найдено несколько позиций:\n{list}\n\n" +
                    $"Уточните название (Например: `{BotCommands.MENU}:{BotCommands.DELETE_POSITION}:Бургер классик`) ",
                    cancellationToken: ct);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException)
            {
                Console.WriteLine($"ChatId: {chatId}");
            }
            catch (Exception ex)
            {
                await bot.SendMessage(chatId, "⚠️ Ошибка при удалении позиции.", cancellationToken: ct);
                Console.WriteLine($"[DelPos ERROR]: {ex}");
            }
        }

        private async Task HandlePositionCallbackAsync(ITelegramBotClient bot, long _chatId, int posNum, CancellationToken ct)
        {
            string mssgTxt = string.Empty;
            var pos = await _menuService.GetPositionAsync(posNum, ct);
            if (pos is null || pos.Status != PositionStatus.Active)
            {
                posNum = -1;
                mssgTxt = "Эта позиция недоступна 😔";
            }
            else
            {
                mssgTxt = FormatPositionCaption(pos);
            }

            await bot.SendMessage(
                    chatId: _chatId,
                    text: mssgTxt,
                    replyMarkup: PositionActionsKeyboard(posNum),
                    cancellationToken: ct);
            return;
        }

        private static string FormatPrice(decimal price) => $"{price:0.##}";

        private static string FormatPositionCaption(Position p)
        {
            var name = (p.Name ?? "").Trim();
            var price = FormatPrice(p.Price);
            var ingredients = p.Ingredients;
            var description = p.Description;
            StringBuilder outputMssg = new StringBuilder();
            outputMssg.Append($"{name}\nЦена: {price}");
            if (!string.IsNullOrWhiteSpace(ingredients))
            {
                outputMssg.Append($"\nСостав: {ingredients}");
            }
            if (!string.IsNullOrWhiteSpace(description)) 
            {
                outputMssg.Append($"\n\n{description}");
            }
            return outputMssg.ToString();
        }

        private static InlineKeyboardMarkup PositionActionsKeyboard(int posNum)
        {
            InlineKeyboardButton[][] buttons =
            [
                [
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад к меню", $"{BotCommands.MENU}:{BotCommands.BACK}"),
                ]
            ];
            // Думаю, правильнее всего будет подвязать кнопку добавления в корзину при получении развёрнутой информации о позиции.
            if (posNum != -1)
            {
                buttons =
                [
                    [
                        InlineKeyboardButton.WithCallbackData("Добавить в корзину", $"{BotCommands.CART_ADD} {posNum} {1}"),
                        InlineKeyboardButton.WithCallbackData("⬅️ Назад к меню", $"{BotCommands.MENU}:{BotCommands.BACK}"),
                    ]
                ];
            }

            return new InlineKeyboardMarkup(buttons);
        }

        #endregion
    }
}
