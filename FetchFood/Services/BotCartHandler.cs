using System.Collections.Concurrent;
using System.Text;
using FetchFood.Abstractions;
using BusinessLogic.Services.Cart.Abstractions;
using DataAccess.Entities;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using FetchFood.Commands;
using BusinessLogic.Services.MakingOrders.Implemenatation;

namespace FetchFood.Services
{
    internal class BotCartHandler : BotCommandHandler
    {
        private readonly CancellationTokenSource _cts = new();

        // Зависимость от бизнес-логики корзины
        private readonly ICartService _cartService;

        // Отслеживает, какого ответа бот ждет от пользователя
        private readonly ConcurrentDictionary<long, string> _userState = new();

        // Внедрение зависимости бизнес-логики 
        public BotCartHandler(Update update, ITelegramBotClient botClient, ICartService cartService) : base(update, botClient)
        {
            _cartService = cartService;
        }

        public override async void Invoke()
        {
            try
            {
                // Обработка кнопок
                if (Update.CallbackQuery != null)
                {
                    await HandleCallbackQueryAsync(Update.CallbackQuery, default);
                    return;
                }

                // Обработка текстовых сообщений
                if (Update.Message != null)
                {
                    await HandleMessageAsync(Update.Message, default);
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка оформления заказа: {ex}");
                await _bot.SendMessage(
                    chatId: Update.Message.Chat.Id,
                    text: "❌ Произошла ошибка во время оформления заказа. Пожалуйста, попробуйте позже.");
            }
        }

        // Создает Inline-клавиатуру для меню корзины
        private static InlineKeyboardMarkup GetCartInlineKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    // callback_data - это то, что бот получит при нажатии
                    InlineKeyboardButton.WithCallbackData(BotCommands.SHOWCART, BotCommands.CART + CommandsBase.Separator + BotCommands.CART_SHOW)
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(BotCommands.DELETEITEM, MakingOrdersCommand.StartOrder.Command),
                    InlineKeyboardButton.WithCallbackData(BotCommands.ADDITEM, MenuCommand.GoBack.Command)
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(BotCommands.CLEARCART, BotCommands.CART + CommandsBase.Separator + BotCommands.CART_CLEAR)
                }
            });
        }

        // ОБРАБОТКА НАЖАТИЙ КНОПОК 
        public async Task HandleCallbackQueryAsync(CallbackQuery query, CancellationToken ct)
        {
            if (query.Data == null || query.Message == null) return;
            long userId = query.Message.Chat.Id;

            // Лия (2025-12-13): если команда пришла не одна, а с аргументами, разбираем её. Иначе просто обрабатываем запрос как есть
            string queryData = query.Data;
            if (query.Data.Contains(' '))
            {
                queryData = query.Data.Split(' ')[0];
            }
                // Определяем, какая кнопка была нажата, по ее callback_data
                switch (queryData)
            {
                case BotCommands.CART + CommandsBase.Separator + BotCommands.CART_SHOW:
                    await ShowCartAsync(this._bot, userId, ct);
                    break;

                case BotCommands.CART + CommandsBase.Separator + BotCommands.CART_ADD:
                    // Лия (2025-12-13): если команда пришла с аргументами, добавляем позицию в корзину сразу.
                    if (query.Data.Contains(' '))
                    {
                        string[] queryData_splitted = query.Data.Split(' ');
                        if (queryData_splitted.Length < 3)
                        {
                            break;
                        }
                        int posNum;
                        if (!int.TryParse(queryData_splitted[1], out posNum))
                        {
                            break;
                        }
                        int posQuantity;
                        if (!int.TryParse(queryData_splitted[2], out posQuantity))
                        {
                            break;
                        }
                        // если все проверки пройдены, засылаем команду на добавление позиции.
                        await _cartService.AddItemToCartAsync(userId, posNum, posQuantity);

                        await this._bot.SendMessage(
                            chatId: userId,
                            text: $"✅ Товар добавлен в корзину.",
                            replyMarkup: GetCartInlineKeyboard(), // Показываем меню
                            cancellationToken: ct
                        );
                    }
                    else
                    {
                        // Устанавливаем состояние "ждем ID и кол-во"
                        _userState[userId] = "awaiting_item_to_add";
                        await this._bot.SendMessage(
                            chatId: userId,
                            text: "Введите товар для добавления в формате:\n*ID_Товара Количество*\n\n*Пример:* `5 2`",
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ct
                        );
                    }
                    break;

                case BotCommands.CART + CommandsBase.Separator + BotCommands.CART_REMOVE:
                    // Устанавливаем состояние "ждем ID"
                    _userState[userId] = "awaiting_item_to_remove";
                    await this._bot.SendMessage(
                        chatId: userId,
                        text: "Введите ID товара, который хотите удалить.",
                        cancellationToken: ct
                    );
                    break;

                // --- ОЧИСТИТЬ КОРЗИНУ ---
                case BotCommands.CART + CommandsBase.Separator + BotCommands.CART_CLEAR:
                    await _cartService.ClearCartAsync(userId); // Вызов бизнес-логики
                    await this._bot.SendMessage(
                        chatId: userId,
                        text: "✅ Корзина очищена.",
                        replyMarkup: GetCartInlineKeyboard(), // Снова показываем меню
                        cancellationToken: ct
                    );
                    break;
            }

            // Отвечаем на Callback, чтобы убрать "часики" (загрузку) на кнопке
            try
            {
                await this._bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
            }
            catch (Exception)
            {
                // Игнорируем (если callback слишком старый, бот API выдаст ошибку)
            }
        }

        // Лия (2025-12-13): добавляю возврат значения, чтобы понимать - сообщение было принято сервисом или нет
        // (если бот не находится в состоянии, когда пользователь добавляет или удалят позицию из корзины, будет возвращено false)
        public async Task<bool> HandleMessageAsync(Message msg, CancellationToken ct)
        {
            if (this._bot is null || msg.Text is not { } text) return false;
            long userId = msg.Chat.Id;

            // 1. Проверяем, ждем ли мы ответа от пользователя (проверка состояния)
            if (_userState.TryGetValue(userId, out string? state))
            {
                switch (state)
                {
                    // Если ждали "ID Кол-во" для добавления
                    case "awaiting_item_to_add":
                        await AddItemFromMessageAsync(this._bot, msg, ct);
                        return true; 

                    // Если ждали "ID" для удаления
                    case "awaiting_item_to_remove":
                        await RemoveItemFromMessageAsync(this._bot, msg, ct);
                        return true;
                    // Лия (2025-12-13): добавляю дефолтную ветку, чтобы была возможность проверить состояние сервиса и выйти,
                    // если пользователь не в процессе добавления или удаления позиции.
                    default:
                        return false;
                }
            }
            return false;
            // Лия (2025-12-13): убираю эту часть кода за ненадобностью - обработка некорректного сообщения или сообщения "start" уже описана в TelegramServiceBot.
            //// 2. Если состояния нет, обрабатываем как обычную команду (/start)
            //switch (text)
            //{
            //    case BotCommands.START:
            //        string welcomeText = "Привет! Я бот для заказа еды FetchFood 🤖\n\nИспользуйте меню ниже для управления корзиной.";

            //        await bot.SendMessage(
            //            chatId: userId,
            //            text: welcomeText,
            //            replyMarkup: GetCartInlineKeyboard(), 
            //            cancellationToken: ct
            //        );
            //        break;

            //    default:
            //        // Реакция на любой другой текст
            //        await bot.SendMessage(
            //            chatId: userId,
            //            text: "Неизвестная команда. Пожалуйста, используйте меню.",
            //            replyMarkup: GetCartInlineKeyboard(),
            //            cancellationToken: ct
            //        );
            //        break;
            //}
        }

        // добавления товара 
        private async Task AddItemFromMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            if (bot is null) return;
            long userId = msg.Chat.Id;
            string[] parts = msg.Text!.Split(' '); 

            // Проверка формата ("ID" и "Кол-во" - 2 части)
            if (parts.Length < 2)
            {
                await bot.SendMessage(
                    chatId: userId,
                    text: "❌ *Неправильный формат.*\nПожалуйста, введите в формате: `ID_Товара Количество`",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: GetCartInlineKeyboard(), // Показываем меню
                    cancellationToken: ct
                );
                return;
            }

            try
            {
                int menuPositionId = int.Parse(parts[0]);
                int quantity = int.Parse(parts[1]);

                await _cartService.AddItemToCartAsync(userId, menuPositionId, quantity);

                await bot.SendMessage(
                    chatId: userId,
                    text: $"✅ Товар (ID {menuPositionId}) добавлен в корзину.",
                    replyMarkup: GetCartInlineKeyboard(), // Показываем меню
                    cancellationToken: ct
                );
            }
            catch (Exception ex) 
            {
                await bot.SendMessage(
                    chatId: userId,
                    text: $"❌ *Ошибка.*\n{ex.Message}\n\nУбедитесь, что ID и количество являются числами.",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: GetCartInlineKeyboard(), // Показываем меню
                    cancellationToken: ct
                );
            }
            finally
            {
                // Сбрасываем состояние "ожидания" в любом случае
                _userState.TryRemove(userId, out _);
            }
        }

        // Логика удаления товара 
        private async Task RemoveItemFromMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            if (bot is null) return;
            long userId = msg.Chat.Id;

            try
            {
                int productId = int.Parse(msg.Text!);

                // Проверяем, есть ли товар, ПЕРЕД удалением
                var cart = await _cartService.GetCartAsync(userId);
                var itemExists = cart.CartItems.Any(i => i.ProductId == productId);

                if (!itemExists)
                {
                    await bot.SendMessage(
                        chatId: userId,
                        text: $"❌ Товар с ID {productId} не найден в вашей корзине.",
                        replyMarkup: GetCartInlineKeyboard(),
                        cancellationToken: ct
                    );
                }
                else
                {
                    // Если товар есть, удаляем 
                    await _cartService.RemoveItemFromCartAsync(userId, productId);

                    await bot.SendMessage(
                        chatId: userId,
                        text: $"✅ Товар с ID {productId} удален.",
                        replyMarkup: GetCartInlineKeyboard(),
                        cancellationToken: ct
                    );
                }
            }
            catch (Exception) 
            {
                await bot.SendMessage(
                    chatId: userId,
                    text: "❌ *Ошибка.*\nID товара должен быть числом.",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: GetCartInlineKeyboard(),
                    cancellationToken: ct
                );
            }
            finally
            {
                // Сбрасываем состояние "ожидания"
                _userState.TryRemove(userId, out _);
            }
        }

        // Показывает содержимое корзины 
        private async Task ShowCartAsync(ITelegramBotClient bot, long userId, CancellationToken ct)
        {
            if (bot is null) return;

            // Получаем данные корзины из бизнес-логики
            var cart = await _cartService.GetCartAsync(userId);

            // Проверка на пустую корзину
            if (cart.CartItems == null || !cart.CartItems.Any())
            {
                await bot.SendMessage(
                    chatId: userId,
                    text: "Ваша корзина пуста.",
                    replyMarkup: GetCartInlineKeyboard(),
                    cancellationToken: ct
                );
                return;
            }

            var cartContent = new StringBuilder("--- 🛒 Ваша корзина ---\n");

            foreach (var item in cart.CartItems)
            {
                // Используем данные из CartItem.cs
                cartContent.AppendLine($"*ID {item.ProductId}:* `{item.Quantity} x {item.ProductName} @ {item.Price:F2} руб = {item.Quantity * item.Price:F2} руб`");
            }

            // Итоговая цена из UserOrderData.cs
            cartContent.AppendLine($"\n*Итого:* `{cart.Price:F2} руб`");

            await bot.SendMessage(
                chatId: userId,
                text: cartContent.ToString(),
                parseMode: ParseMode.Markdown,
                replyMarkup: GetCartInlineKeyboard(), // Показываем меню
                cancellationToken: ct
            );
        }

        // Реализация ShowMainMenuAsync из интерфейса
        public async Task ShowMainMenuAsync(ITelegramBotClient bot, long chatId, CancellationToken ct)
        {
            string welcomeText = "Привет! Я бот для заказа еды FetchFood 🤖\n\n" +
                                 "Используйте меню внизу для управления корзиной.";

            await bot.SendMessage(
                chatId: chatId,
                text: welcomeText,
                replyMarkup: GetCartInlineKeyboard(), // Показываем Inline-меню
                cancellationToken: ct
            );
        }
    }
}
