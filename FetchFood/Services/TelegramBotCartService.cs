using System.Collections.Concurrent;
using System.Security.AccessControl;
using System.Text;
using BusinessLogic.Services.MakingOrders.Implemenatation;
using FetchFood.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Services
{
    /// <summary>
    /// Сервис для управления корзиной товаров через Telegram бота.
    /// </summary>
    internal class TelegramBotCartService : ITelegramBotCartService
    {
        private readonly CancellationTokenSource _cts = new();
        // Сервис для основной логики корзины
        private readonly CartService _cartService = new();
        // для отслеживания состояния каждого пользователя (например, ожидание ввода данных)
        private readonly ConcurrentDictionary<long, string> _userState = new();
        // Сервис для оформления заказов
        private readonly ITelegramBotMakingOrdersService _makingOrdersService;
        public TelegramBotCartService(ITelegramBotMakingOrdersService makingOrdersService)
        {
            _makingOrdersService = makingOrdersService;
        }

        /// <summary>
        /// Создает и возвращает клавиатуру с главным меню.
        /// </summary>
        /// <returns>Объект ReplyKeyboardMarkup с кнопками меню.</returns>
        private ReplyKeyboardMarkup GetMainMenuKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "🛒 Показать корзину" },
                new KeyboardButton[] { "➕ Добавить товар", "➖ Удалить товар" },
                new KeyboardButton[] { "🗑️ Очистить корзину" },
            })
            {
                // Автоматически изменять размер клавиатуры
                ResizeKeyboard = true
            };
        }

        /// <summary>
        /// Асинхронно обрабатывает входящие сообщения от пользователя.
        /// </summary>
        /// <param name="bot">Клиент Telegram бота.</param>
        /// <param name="msg">Входящее сообщение.</param>
        /// <param name="ct">Токен отмены.</param>
        public async Task HandleMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            if (bot is null || msg.Text is not { } text) return;
            long userId = msg.Chat.Id;

            // Сначала проверяем, находится ли пользователь в каком-либо состоянии (например, ожидает ввода)
            if (_userState.TryGetValue(userId, out string? state))
            {
                switch (state)
                {
                    // Если пользователь добавляет товар, вызываем соответствующий метод
                    case "awaiting_item_to_add":
                        await AddItemFromMessageAsync(bot, msg, ct);
                        return;
                    // Если пользователь удаляет товар
                    case "awaiting_item_to_remove":
                        await RemoveItemFromMessageAsync(bot, msg, ct);
                        return;
                }
            }

            // Если пользователь не находится в каком-либо состоянии, обрабатываем текстовые команды
            switch (text)
            {
                case BotCommands.START:
                    string welcomeText = "Привет! Я бот для заказа еды FetchFood 🤖\n\nИспользуйте меню внизу для управления корзиной.";

                    await bot.SendMessage(
                        chatId: userId,
                        text: welcomeText,
                        replyMarkup: GetMainMenuKeyboard(),
                        cancellationToken: ct
                    );
                    break;

                case BotCommands.SHOWCART:
                    await ShowCartAsync(bot, userId, ct);
                    break;

                case BotCommands.ADDITEM:
                    // Устанавливаем состояние пользователя на "ожидание товара для добавления"
                    _userState[userId] = "awaiting_item_to_add";
                    await bot.SendMessage(
                        chatId: userId,
                        text: "Введите товар для добавления в формате:\n*Название Количество Цена*\n\n*Пример:* `Пицца 2 12.50`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct
                    );
                    break;

                case BotCommands.DELETEITEM:
                    // Устанавливаем состояние пользователя на "ожидание товара для удаления"
                    _userState[userId] = "awaiting_item_to_remove";
                    await bot.SendMessage(
                        chatId: userId,
                        text: "Введите ID товара, который хотите удалить.",
                        cancellationToken: ct
                    );
                    break;

                case BotCommands.CLEARCART:
                    _cartService.ClearCart(userId);
                    await bot.SendMessage(
                        chatId: userId,
                        text: "✅ Корзина очищена.",
                        cancellationToken: ct
                    );
                    break;
                default:
                    await bot.SendMessage(
                        chatId: userId,
                        text: "Неизвестная команда. Пожалуйста, используйте меню.",
                        cancellationToken: ct
                    );
                    break;
            }
        }

        /// <summary>
        /// Добавляет товар в корзину на основе сообщения от пользователя.
        /// </summary>
        /// <param name="bot">Клиент Telegram бота.</param>
        /// <param name="msg">Сообщение с данными о товаре.</param>
        /// <param name="ct">Токен отмены.</param>
        private async Task AddItemFromMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            if (bot is null) return;
            long userId = msg.Chat.Id;
            string[] parts = msg.Text!.Split(' ');

            // Проверяем, что сообщение содержит все необходимые части: название, количество, цена
            if (parts.Length < 3)
            {
                await bot.SendMessage(
                    chatId: userId,
                    text: "❌ *Неправильный формат.*\nПожалуйста, введите в формате: `Название Количество Цена`",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct
                );
                return;
            }

            try
            {
                // Парсим данные из сообщения
                string foodName = parts[0];
                int quantity = int.Parse(parts[1]);
                decimal price = decimal.Parse(parts[2].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);

                // Добавляем товар в корзину через сервис
                _cartService.AddToCart(userId, foodName, quantity, price);

                await bot.SendMessage(
                    chatId: userId,
                    text: $"✅ Товар '{foodName}' добавлен в корзину.",
                    cancellationToken: ct
                );
            }
            catch (Exception)
            {
                // Отправляем сообщение об ошибке, если данные некорректны
                await bot.SendMessage(
                    chatId: userId,
                    text: "❌ *Ошибка.*\nУбедитесь, что количество и цена являются числами.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct
                );
            }
            finally
            {
                // Вне зависимости от результата, сбрасываем состояние пользователя
                _userState.TryRemove(userId, out _);
            }
        }

        /// <summary>
        /// Удаляет товар из корзины по ID, полученному в сообщении.
        /// </summary>
        /// <param name="bot">Клиент Telegram бота.</param>
        /// <param name="msg">Сообщение с ID товара.</param>
        /// <param name="ct">Токен отмены.</param>
        private async Task RemoveItemFromMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            if (bot is null) return;
            long userId = msg.Chat.Id;

            try
            {
                // Парсим ID товара из текста сообщения
                int foodId = int.Parse(msg.Text!);
                bool wasRemoved = _cartService.RemoveFromCart(userId, foodId);

                // Формируем ответ в зависимости от того, был ли товар удален
                string response = wasRemoved
                    ? $"✅ Товар с ID {foodId} удален."
                    : $"❌ Товар с ID {foodId} не найден в вашей корзине.";

                await bot.SendMessage(
                    chatId: userId,
                    text: response,
                    cancellationToken: ct
                );
            }
            catch (Exception)
            {
                await bot.SendMessage(
                    chatId: userId,
                    text: "❌ *Ошибка.*\nID товара должен быть числом.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct
                );
            }
            finally
            {
                // Сбрасываем состояние пользователя
                _userState.TryRemove(userId, out _);
            }
        }

        /// <summary>
        /// Отображает содержимое корзины пользователя.
        /// </summary>
        /// <param name="bot">Клиент Telegram бота.</param>
        /// <param name="userId">ID пользователя (чата).</param>
        /// <param name="ct">Токен отмены.</param>
        private async Task ShowCartAsync(ITelegramBotClient bot, long userId, CancellationToken ct)
        {
            if (bot is null) return;
            // УДАЛИТЬ ПЕРЕД СЛИЯНИЕМ С ОСНОВНОЙ ВЕТКОЙ
            CartTestDataInitializer.InitializeTestData(_cartService, userId);
            var cart = _cartService.GetCart(userId);

            // Если корзина пуста, сообщаем об этом
            if (!cart.Any())
            {
                await bot.SendMessage(
                    chatId: userId,
                    text: "Ваша корзина пуста.",
                    cancellationToken: ct
                );
                return;
            }

            // Формируем текстовое представление корзины
            var cartContent = new StringBuilder("--- 🛒 Ваша корзина ---\n");
            foreach (var item in cart)
            {
                cartContent.AppendLine($"*ID {item.FoodId}:* `{item.Quantity} x {item.FoodName} @ ${item.Price:F2} = ${item.TotalPrice:F2}`");
            }
            cartContent.AppendLine($"\n*Итого:* `${_cartService.GetTotal(userId):F2}`");

            // Создаем инлайн-кнопку для оформления заказа
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Оформить заказ", "start_order")
                }
            });

            await bot.SendMessage(
                chatId: userId,
                text: cartContent.ToString(),
                parseMode: ParseMode.Markdown,
                replyMarkup: inlineKeyboard,
                cancellationToken: ct
            );
        }

        /// <summary>
        /// Отображает главное меню пользователю.
        /// </summary>
        /// <param name="bot">Клиент Telegram бота.</param>
        /// <param name="chatId">ID чата для отправки сообщения.</param>
        /// <param name="ct">Токен отмены.</param>
        public async Task ShowMainMenuAsync(ITelegramBotClient bot, long chatId, CancellationToken ct)
        {
            string welcomeText = "Привет! Я бот для заказа еды FetchFood 🤖\n\n" +
                                 "Используйте меню внизу для управления корзиной.";

            await bot.SendMessage(
                chatId: chatId,
                text: welcomeText,
                replyMarkup: GetMainMenuKeyboard(),
                cancellationToken: ct
            );
        }
    }
}