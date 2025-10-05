using System.Collections.Concurrent; 
using System.Text; 
using FetchFood.Abstractions; 
using Telegram.Bot; 
using Telegram.Bot.Polling; 
using Telegram.Bot.Types; 
using Telegram.Bot.Types.Enums; 
using Telegram.Bot.Types.ReplyMarkups; 
 
namespace FetchFood.Services
{
    internal class TelegramBotCartService : ITelegramBotCartService
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly CartService _cartService = new();
        private readonly ConcurrentDictionary<long, string> _userState = new();

        public TelegramBotCartService() { }

        private ReplyKeyboardMarkup GetMainMenuKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "🛒 Показать корзину" },
                new KeyboardButton[] { "➕ Добавить товар", "➖ Удалить товар" },
                new KeyboardButton[] { "🗑️ Очистить корзину" },
            })
            {
                ResizeKeyboard = true
            };
        }

        public async Task HandleMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            if (bot is null || msg.Text is not { } text) return;
            long userId = msg.Chat.Id;

            if (_userState.TryGetValue(userId, out string? state))
            {
                switch (state)
                {
                    case "awaiting_item_to_add":
                        await AddItemFromMessageAsync(bot, msg, ct);
                        return;
                    case "awaiting_item_to_remove":
                        await RemoveItemFromMessageAsync(bot, msg, ct);
                        return;
                }
            }

            switch (text)
            {
                case "/start":
                    string welcomeText = "Привет! Я бот для заказа еды FetchFood 🤖\n\nИспользуйте меню внизу для управления корзиной.";


                    await bot.SendMessage(
                        chatId: userId,
                        text: welcomeText,
                        replyMarkup: GetMainMenuKeyboard(),
                        cancellationToken: ct
                    );
                    break;

                case "🛒 Показать корзину":
                    await ShowCartAsync(bot, userId, ct);
                    break;

                case "➕ Добавить товар":
                    _userState[userId] = "awaiting_item_to_add";
                    await bot.SendMessage(
                        chatId: userId,
                        text: "Введите товар для добавления в формате:\n*Название Количество Цена*\n\n*Пример:* `Пицца 2 12.50`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct
                    );
                    break;

                case "➖ Удалить товар":
                    _userState[userId] = "awaiting_item_to_remove";
                    await bot.SendMessage(
                        chatId: userId,
                        text: "Введите ID товара, который хотите удалить.",
                        cancellationToken: ct
                    );
                    break;

                case "🗑️ Очистить корзину":
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

        private async Task AddItemFromMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            if (bot is null) return;
            long userId = msg.Chat.Id;
            string[] parts = msg.Text!.Split(' ');

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
                string foodName = parts[0];
                int quantity = int.Parse(parts[1]);
                decimal price = decimal.Parse(parts[2].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);

                _cartService.AddToCart(userId, foodName, quantity, price);

                await bot.SendMessage(
                    chatId: userId,
                    text: $"✅ Товар '{foodName}' добавлен в корзину.",
                    cancellationToken: ct
                );
            }
            catch (Exception)
            {
                await bot.SendMessage(
                    chatId: userId,
                    text: "❌ *Ошибка.*\nУбедитесь, что количество и цена являются числами.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct
                );
            }
            finally
            {
                _userState.TryRemove(userId, out _);
            }
        }

        private async Task RemoveItemFromMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            if (bot is null) return;
            long userId = msg.Chat.Id;

            try
            {
                int foodId = int.Parse(msg.Text!);
                bool wasRemoved = _cartService.RemoveFromCart(userId, foodId);

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
                _userState.TryRemove(userId, out _);
            }
        }

        private async Task ShowCartAsync(ITelegramBotClient bot, long userId, CancellationToken ct)
        {
            if (bot is null) return;
            var cart = _cartService.GetCart(userId);

            if (!cart.Any())
            {
                await bot.SendMessage(
                    chatId: userId,
                    text: "Ваша корзина пуста.",
                    cancellationToken: ct
                );
                return;
            }

            var cartContent = new StringBuilder("--- 🛒 Ваша корзина ---\n");
            foreach (var item in cart)
            {
                cartContent.AppendLine($"*ID {item.FoodId}:* `{item.Quantity} x {item.FoodName} @ ${item.Price:F2} = ${item.TotalPrice:F2}`");
            }
            cartContent.AppendLine($"\n*Итого:* `${_cartService.GetTotal(userId):F2}`");

            await bot.SendMessage(
                chatId: userId,
                text: cartContent.ToString(),
                parseMode: ParseMode.Markdown,
                cancellationToken: ct
            );
        }

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
