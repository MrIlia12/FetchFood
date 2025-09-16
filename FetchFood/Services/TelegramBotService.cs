using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;
using System.Collections.Concurrent;

namespace FetchFood.Services
{
    internal class TelegramBotService
    {
        private readonly TelegramBotClient _bot;
        private readonly CancellationTokenSource _cts = new();
        private readonly CartService _cartService = new();
        private readonly ConcurrentDictionary<long, string> _userState = new();

        public TelegramBotService(string token)
        {
            _bot = new TelegramBotClient(token);
        }

        public async Task StartAsync()
        {
            User user = await _bot.GetMeAsync(_cts.Token);
            Console.WriteLine($"@{user.Username} готов к работе.");

            var receiverOptions = new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message } };

            _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, _cts.Token);
        }

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

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.Message is { } message)
            {
                await HandleMessageAsync(bot, message, ct);
            }
        }

        private async Task HandleMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            if (msg.Text is not { } text) return;
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
                    await bot.SendTextMessageAsync(
                        chatId: userId,
                        text: welcomeText,
                        replyMarkup: GetMainMenuKeyboard(), 
                        cancellationToken: ct);
                    break;

                case "🛒 Показать корзину":
                    await ShowCartAsync(bot, userId, ct);
                    break;

                case "➕ Добавить товар":
                    _userState[userId] = "awaiting_item_to_add";
                    await bot.SendTextMessageAsync(userId, "Введите товар для добавления в формате:\n*Название Количество Цена*\n\n*Пример:* `Пицца 2 12.50`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                    break;

                case "➖ Удалить товар":
                    _userState[userId] = "awaiting_item_to_remove";
                    await bot.SendTextMessageAsync(userId, "Введите ID товара, который хотите удалить.", cancellationToken: ct);
                    break;

                case "🗑️ Очистить корзину":
                    _cartService.ClearCart(userId);
                    await bot.SendTextMessageAsync(userId, "✅ Корзина очищена.", cancellationToken: ct);
                    break;

                default:
                    await bot.SendTextMessageAsync(userId, "Неизвестная команда. Пожалуйста, используйте меню.", cancellationToken: ct);
                    break;
            }
        }

        private async Task AddItemFromMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            long userId = msg.Chat.Id;
            string[] parts = msg.Text!.Split(' ');

            if (parts.Length < 3)
            {
                await bot.SendTextMessageAsync(userId, "❌ *Неправильный формат.*\nПожалуйста, введите в формате: `Название Количество Цена`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            try
            {
                string foodName = parts[0];
                int quantity = int.Parse(parts[1]);
                decimal price = decimal.Parse(parts[2].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
                _cartService.AddToCart(userId, foodName, quantity, price);
                await bot.SendTextMessageAsync(userId, $"✅ Товар '{foodName}' добавлен в корзину.", cancellationToken: ct);
            }
            catch (Exception)
            {
                await bot.SendTextMessageAsync(userId, "❌ *Ошибка.*\nУбедитесь, что количество и цена являются числами.", parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
            finally
            {
                _userState.TryRemove(userId, out _);
            }
        }

        private async Task RemoveItemFromMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            long userId = msg.Chat.Id;
            try
            {
                int foodId = int.Parse(msg.Text!);
                bool wasRemoved = _cartService.RemoveFromCart(userId, foodId);

                if (wasRemoved)
                {
                    await bot.SendTextMessageAsync(userId, $"✅ Товар с ID {foodId} удален.", cancellationToken: ct);
                }
                else
                {
                    await bot.SendTextMessageAsync(userId, $"❌ Товар с ID {foodId} не найден в вашей корзине.", cancellationToken: ct);
                }
            }
            catch (Exception)
            {
                await bot.SendTextMessageAsync(userId, "❌ *Ошибка.*\nID товара должен быть числом.", parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
            finally
            {
                _userState.TryRemove(userId, out _);
            }
        }

        private async Task ShowCartAsync(ITelegramBotClient bot, long userId, CancellationToken ct)
        {
            var cart = _cartService.GetCart(userId);
            if (!cart.Any())
            {
                await bot.SendTextMessageAsync(userId, "Ваша корзина пуста.", cancellationToken: ct);
                return;
            }
            var cartContent = new StringBuilder("--- 🛒 Ваша корзина ---\n");
            foreach (var item in cart)
            {
                cartContent.AppendLine($"*ID {item.FoodId}:* `{item.Quantity} x {item.FoodName} @ ${item.Price:F2} = ${item.TotalPrice:F2}`");
            }
            cartContent.AppendLine($"\n*Итого:* `${_cartService.GetTotal(userId):F2}`");
            await bot.SendTextMessageAsync(userId, cartContent.ToString(), parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        public Task StopAsync()
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }

        private static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
        {
            Console.WriteLine($"Ошибка в Telegram Bot API: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}