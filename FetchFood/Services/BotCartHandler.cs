using System.Collections.Concurrent;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using BusinessLogic.Services.Cart.Abstractions;
using FetchFood.Abstractions;

namespace FetchFood.Services
{
    // Класс-обработчик логики корзины
    class BotCartHandler : BotCommandHandler
    {
        private readonly ICartService _cartService;
        private readonly CancellationToken _ct;

        // Для хранения состояния пользователя между запросами
        private static readonly ConcurrentDictionary<long, string> _userState = new();

        public BotCartHandler(Update update, ITelegramBotClient botClient, ICartService cartService, CancellationToken ct)
            : base(update, botClient)
        {
            _cartService = cartService;
            _ct = ct;
        }

        public override async void Invoke()
        {
            if (this.Update == null) return;

            try
            {
                // Точка входа
                if (Update.Type == UpdateType.CallbackQuery)
                {
                    if (Update.CallbackQuery is { } callback)
                    {
                        await HandleCallbackLogicAsync(callback);
                    }
                }
                else if (Update.Type == UpdateType.Message)
                {
                    if (Update.Message is { } message)
                    {
                        await HandleMessageLogicAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cart Error: {ex.Message}");
            }
        }

        // Логика обработки нажатий на инлайн-кнопки
        private async Task HandleCallbackLogicAsync(CallbackQuery callback)
        {
            if (callback.Message is not { } message) return;

            long chatId = message.Chat.Id;

            if (callback.Data is not { } data) return;

            switch (data)
            {
                case BotCommands.CART_SHOW:
                    await ShowCartAsync(chatId);
                    break;

                case BotCommands.CART_ADD:
                    // Переводим пользователя в режим ожидания ввода ID и количества
                    _userState[chatId] = "awaiting_item_to_add";
                    await _bot.SendMessage(chatId,
                        "Введите товар для добавления в формате:\n*ID_Товара Количество*\n\n*Пример:* `5 2`",
                        parseMode: ParseMode.Markdown, cancellationToken: _ct);
                    break;

                case BotCommands.CART_REMOVE:
                    _userState[chatId] = "awaiting_item_to_remove";
                    await _bot.SendMessage(chatId, "Введите ID товара, который хотите удалить.", cancellationToken: _ct);
                    break;

                case BotCommands.CART_CLEAR:
                    await _cartService.ClearCartAsync(chatId);
                    await _bot.SendMessage(chatId, "✅ Корзина очищена.", replyMarkup: GetCartKeyboard(), cancellationToken: _ct);
                    break;
            }

            try { await _bot.AnswerCallbackQuery(callback.Id, cancellationToken: _ct); } catch { }
        }

        // Обработка текстового ввода, если пользователь находится в определенном состоянии
        private async Task HandleMessageLogicAsync(Message message)
        {
            long chatId = message.Chat.Id;
            string? text = message.Text;

            if (_userState.TryGetValue(chatId, out string? state))
            {
                // Защита от получения стикеров или фото вместо текста
                if (string.IsNullOrEmpty(text))
                {
                    await _bot.SendMessage(chatId, "Пожалуйста, введите текстовое значение.", cancellationToken: _ct);
                    return;
                }

                switch (state)
                {
                    case "awaiting_item_to_add":
                        await ProcessAddItemAsync(chatId, text);
                        return;

                    case "awaiting_item_to_remove":
                        await ProcessRemoveItemAsync(chatId, text);
                        return;
                }
            }

            await ShowMainMenuAsync(chatId);
        }

        // Попытка парсинга строки и добавление товара через сервис
        private async Task ProcessAddItemAsync(long chatId, string text)
        {
            try
            {
                string[] parts = text.Split(' ');
                if (parts.Length < 2) throw new FormatException();

                int id = int.Parse(parts[0]);
                int qty = int.Parse(parts[1]);

                await _cartService.AddItemToCartAsync(chatId, id, qty);
                await _bot.SendMessage(chatId, $"✅ Товар {id} добавлен.", replyMarkup: GetCartKeyboard(), cancellationToken: _ct);
            }
            catch
            {
                await _bot.SendMessage(chatId, "❌ Ошибка формата. Используйте: ID Количество", replyMarkup: GetCartKeyboard(), cancellationToken: _ct);
            }
            finally
            {
                // Очищаем состояние пользователя после завершения операции
                _userState.TryRemove(chatId, out _);
            }
        }

        private async Task ProcessRemoveItemAsync(long chatId, string text)
        {
            try
            {
                int id = int.Parse(text);
                await _cartService.RemoveItemFromCartAsync(chatId, id);
                await _bot.SendMessage(chatId, $"✅ Товар {id} удален.", replyMarkup: GetCartKeyboard(), cancellationToken: _ct);
            }
            catch
            {
                await _bot.SendMessage(chatId, "❌ Ошибка. Введите числовой ID.", replyMarkup: GetCartKeyboard(), cancellationToken: _ct);
            }
            finally
            {
                _userState.TryRemove(chatId, out _);
            }
        }

        // Формирование и отображение содержимого корзины
        private async Task ShowCartAsync(long chatId)
        {
            var cart = await _cartService.GetCartAsync(chatId);

            if (cart.CartItems == null || !cart.CartItems.Any())
            {
                await _bot.SendMessage(chatId, "Ваша корзина пуста.", replyMarkup: GetCartKeyboard(), cancellationToken: _ct);
                return;
            }

            var sb = new StringBuilder("--- 🛒 Ваша корзина ---\n");
            foreach (var item in cart.CartItems)
            {
                sb.AppendLine($"*ID {item.ProductId}:* `{item.Quantity} x {item.ProductName} = {item.Quantity * item.Price:F2}`");
            }
            sb.AppendLine($"\n*Итого:* `{cart.Price:F2}`");

            await _bot.SendMessage(chatId, sb.ToString(), parseMode: ParseMode.Markdown, replyMarkup: GetCartKeyboard(), cancellationToken: _ct);
        }

        private async Task ShowMainMenuAsync(long chatId)
        {
            await _bot.SendMessage(chatId, "Меню корзины:", replyMarkup: GetCartKeyboard(), cancellationToken: _ct);
        }

        public static InlineKeyboardMarkup GetCartKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(BotCommands.SHOWCART, BotCommands.CART_SHOW) },
                new[] {
                    InlineKeyboardButton.WithCallbackData(BotCommands.ADDITEM, BotCommands.CART_ADD),
                    InlineKeyboardButton.WithCallbackData(BotCommands.DELETEITEM, BotCommands.CART_REMOVE)
                },
                new[] { InlineKeyboardButton.WithCallbackData(BotCommands.CLEARCART, BotCommands.CART_CLEAR) }
            });
        }

        // Проверка, занят ли пользователь вводом данных для корзины
        public static bool IsUserBusy(long userId)
        {
            return _userState.ContainsKey(userId);
        }
    }
}