using BusinessLogic.Services.Courier.Abstractions;
using DataAccess.Entities;
using FetchFood.Commands;
using FetchFood.States;
using System.Collections.Concurrent;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Services
{
    /// <summary>
    /// Обработчик команд курьера
    /// </summary>
    class BotCourierHandler : BotCommandHandler
    {
        private readonly ICourierService _courierService;
        private readonly ConcurrentDictionary<long, UserState> _userState;

        public BotCourierHandler(Update update, ITelegramBotClient botClient, ICourierService courierService, ConcurrentDictionary<long, UserState> userState) 
            : base(update, botClient, userState)
        {
            _courierService = courierService;
        }

        public override async Task Invoke()
        {
            try
            {
                if (Update.CallbackQuery != null)
                {
                    await HandleCallbackQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Ошибка в BotCourierHandler: {ex.Message}");
                
                var chatId = Update.CallbackQuery?.Message?.Chat.Id ?? Update.Message?.Chat.Id;
                if (chatId.HasValue)
                {
                    await _bot.SendMessage(
                        chatId: chatId.Value,
                        text: "❌ Произошла ошибка. Попробуйте позже.");
                }
            }
        }

        private async Task HandleCallbackQueryAsync()
        {
            var callback = Update.CallbackQuery;
            var chatId = callback.Message.Chat.Id;
            var userId = callback.From.Id;
            var data = callback.Data;

            // Парсим команду: courier:action:orderId
            var parts = data.Split(':');
            if (parts.Length < 2) return;

            var action = parts[1];
            long orderId = parts.Length >= 3 && long.TryParse(parts[2], out var id) ? id : 0;

            switch (action)
            {
                case CourierCommands.AVAILABLE_ORDERS:
                    await ShowAvailableOrdersAsync(chatId, userId);
                    break;

                case CourierCommands.ORDERS:
                    await ShowCourierOrdersAsync(chatId, userId);
                    break;

                case CourierCommands.TAKE:
                    await TakeOrderToDelivery(chatId, userId, (int)orderId);
                    break;

                case CourierCommands.DETAILS:
                    if (orderId > 0)
                        await ShowOrderDetailsAsync(chatId, orderId);
                    break;

                case CourierCommands.ARRIVED:
                    if (orderId > 0)
                        await HandleCourierArrivedAsync(chatId, userId, orderId);
                    break;

                case CourierCommands.COMPLETE:
                    if (orderId > 0)
                        await HandleCompleteDeliveryAsync(chatId, userId, orderId);
                    break;
            }

            await _bot.AnswerCallbackQuery(callback.Id);
        }

        private async Task TakeOrderToDelivery(long chatId, long courierId, int orderId)
        {
            var result = await _courierService.TakeOrderInDeliveryAsync(courierId, orderId);

            if (!result)
            {
                throw new Exception("Ошибка при обновлении заказа.");
            }

            var courierKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📦 Мои заказы", Commands.CourierCommands.ViewOrders.Command),
                    InlineKeyboardButton.WithCallbackData("📦 Доступные заказы", CourierCommands.ViewAvailableOrders.Command)
                }
            });

            await _bot.SendMessage(
                chatId: chatId,
                text: "🚗 Консоль курьера!\n\nНажмите на кнопки ниже, чтобы посмотреть активные заказы для доставки.",
                replyMarkup: courierKeyboard);

            return;
        }

        /// <summary>
        /// Показывает список активных заказов для курьера
        /// </summary>
        private async Task ShowAvailableOrdersAsync(long chatId, long courierId)
        {
            var orders = await _courierService.GetAvailableOrdersAsync(courierId);

            if (orders == null || !orders.Any())
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: "📦 Нет активных заказов для доставки.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("📦 *Активные заказы для доставки:*\n");

            var buttons = new List<InlineKeyboardButton[]>();

            foreach (var order in orders)
            {
                sb.AppendLine($"🔹 Заказ #{order.OrderId}");
                sb.AppendLine($"   📍 {order.Address}");
                sb.AppendLine($"   💰 {order.Price:N2} ₽");
                sb.AppendLine();

                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"📋 Заказ #{order.OrderId}",
                        CourierCommands.WithOrderId(CourierCommands.DETAILS, order.OrderId))
                });
            }

            var keyboard = new InlineKeyboardMarkup(buttons);

            await _bot.SendMessage(
                chatId: chatId,
                text: sb.ToString(),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        /// <summary>
        /// Показывает список активных заказов курьера
        /// </summary>
        private async Task ShowCourierOrdersAsync(long chatId, long courierId)
        {
            var orders = await _courierService.GetCourierOrdersAsync(courierId);

            if (orders == null || !orders.Any())
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: "📦 Нет активных заказов для доставки.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("📦 *Активные заказы для доставки:*\n");

            var buttons = new List<InlineKeyboardButton[]>();

            foreach (var order in orders)
            {
                sb.AppendLine($"🔹 Заказ #{order.OrderId}");
                sb.AppendLine($"   📍 {order.Address}");
                sb.AppendLine($"   💰 {order.Price:N2} ₽");
                sb.AppendLine();

                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"📋 Заказ #{order.OrderId}",
                        CourierCommands.WithOrderId(CourierCommands.DETAILS, order.OrderId))
                });
            }

            var keyboard = new InlineKeyboardMarkup(buttons);

            await _bot.SendMessage(
                chatId: chatId,
                text: sb.ToString(),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        /// <summary>
        /// Показывает детали заказа с кнопкой "Я на месте"
        /// </summary>
        private async Task ShowOrderDetailsAsync(long chatId, long orderId)
        {
            var order = await _courierService.GetOrderDetailsAsync(orderId);

            if (order == null)
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: "❌ Заказ не найден.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"📦 *Заказ #{order.OrderId}*\n");
            sb.AppendLine($"📍 *Адрес:* {order.Address}");
            sb.AppendLine($"📞 *Телефон:* {order.PhoneNumber}");
            sb.AppendLine($"💰 *Сумма:* {order.Price:N2} ₽");

            if (!string.IsNullOrEmpty(order.Comment))
            {
                sb.AppendLine($"💬 *Комментарий:* {order.Comment}");
            }

            sb.AppendLine($"\n📊 *Статус:* {GetStatusText(order.Status)}");

            InlineKeyboardMarkup keyboard;

            if (order.IdCourier is not null)
            {
                keyboard =
                    new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "🚗 Я на месте!",
                                CourierCommands.WithOrderId(CourierCommands.ARRIVED, order.OrderId))
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "✅ Завершить доставку",
                                CourierCommands.WithOrderId(CourierCommands.COMPLETE, order.OrderId))
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "◀️ Назад к списку",
                                CourierCommands.ViewOrders.Command)
                        }
                    });
            }
            else
            {
                keyboard =
                    new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "📦 Взять в доставку.",
                                CourierCommands.WithOrderId(CourierCommands.TAKE, order.OrderId))
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "◀️ Назад к списку",
                                CourierCommands.ViewAvailableOrders.Command)
                        }
                    });
            }

            await _bot.SendMessage(
                chatId: chatId,
                text: sb.ToString(),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Я на месте"
        /// </summary>
        private async Task HandleCourierArrivedAsync(long chatId, long courierId, long orderId)
        {
            var result = await _courierService.NotifyArrivalAsync(courierId, orderId);

            // Отправляем сообщение курьеру
            await _bot.SendMessage(
                chatId: chatId,
                text: result.Message);

            // Если успешно - отправляем уведомление пользователю
            if (result.Success && result.UserIdToNotify.HasValue)
            {
                try
                {
                    await _bot.SendMessage(
                        chatId: result.UserIdToNotify.Value,
                        text: result.UserNotificationMessage);

                    var userId = (long)result.UserIdToNotify;
                    var state = this._userState[userId];
                    this._userState[userId].State.ToNextState(state);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Не удалось отправить уведомление пользователю {result.UserIdToNotify}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Обрабатывает завершение доставки
        /// </summary>
        private async Task HandleCompleteDeliveryAsync(long chatId, long courierId, long orderId)
        {
            var success = await _courierService.CompleteDeliveryAsync(courierId, orderId);

            if (success)
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: $"✅ Заказ #{orderId} успешно завершен!\n\nСпасибо за работу! 🎉",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "📦 Мои заказы",
                            CourierCommands.ViewOrders.Command)
                    }));
            }
            else
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: "❌ Не удалось завершить заказ. Попробуйте позже.");
            }
        }

        /// <summary>
        /// Возвращает текстовое описание статуса
        /// </summary>
        private static string GetStatusText(string status) => status switch
        {
            "Created" => "🆕 Создан",
            "InDelivery" => "🚗 В доставке",
            "CourierArrived" => "📍 Курьер на месте",
            "Completed" => "✅ Завершен",
            "Cancelled" => "❌ Отменен",
            _ => status
        };
    }
}
