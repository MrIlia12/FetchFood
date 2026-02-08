using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using BusinessLogic.Services.Administration.Abstraction;
using FetchFood.States;
using System.Collections.Concurrent;
using FetchFood.Commands;
using DataAccess.Entities;
using System.Text;

namespace FetchFood.Services
{
    class BotAdministrationHandler : BotCommandHandler
    {
        private readonly IAdministrationService _administrationService;

        /// <summary>
        /// ctor.
        /// </summary>
        public BotAdministrationHandler(
            Update update,
            ITelegramBotClient botClient, 
            IAdministrationService administrationService,
            ConcurrentDictionary<long, UserState> userState) 
            : base(update, botClient, userState)
        {
            _administrationService = administrationService;
        }

        public override async Task Invoke()
        {
            try
            {
                await HandleCallbackQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex}");
                await _bot.SendMessage(
                    chatId: Update.Message.Chat.Id,
                    text: "❌ Произошла ошибка.");
            }
        }

        private async Task HandleCallbackQueryAsync()
        {
            var callback = Update.CallbackQuery;
            var userId = callback.From.Id;
            var chatId = callback.Message.Chat.Id;
            var command = callback.Data.Split(CommandsBase.Separator)[1];

            switch (command)
            {
                case AdministrationCommands.SHOWORDERS:
                    await this._bot.SendMessage(
                                chatId: userId,
                                text: $"Консоль администратора.",
                                replyMarkup: GetHomeOrdersKeyboard());
                    return;

                case AdministrationCommands.TOHOMECONSOLE:
                    await this._bot.SendMessage(
                                chatId: userId,
                                text: $"Консоль администратора.",
                                replyMarkup: GetHomeAdministrationKeyboard());
                    return;

                case AdministrationCommands.SHOWACTIVEORDERS:
                    await this.ShowOrders(command);
                    return;

                case AdministrationCommands.SHOWCOMPLETEDORDERS:
                    await this.ShowOrders(command);
                    return;

                case AdministrationCommands.COURIERSORDERS:
                    await this.ShowOrders(command);
                    return;

                case BotCommands.GETORDERS:
                    await GetOrderAsync();
                    return;

                case AdministrationCommands.TODELIVERY:
                    await UpdateOrderAsync(OrderStatus.ToDelivery);
                    return;

                case AdministrationCommands.CANCEL:
                    await UpdateOrderAsync(OrderStatus.Cancelled);
                    return;
            }

            return;
        }

        private async Task UpdateOrderAsync(string newStatus)
        {
            int orderId = this.ParseCommand();
            var userId = await this._administrationService.GetOrdersUserIdAsync(orderId);
            var result = await this._administrationService.UpdateOrderStatusAsync(orderId, newStatus);

            if (!result)
            {
                throw new Exception("Не удалось обновить статуса заказа.");
            }

            switch (newStatus)
            {
                case OrderStatus.ToDelivery:
                    await this._bot.SendMessage(
                            chatId: userId,
                            text: "Ваш заказ готов и передан в доставку, ожидайте курьера.");
                    break;

                case OrderStatus.Cancelled:
                    await this._bot.SendMessage(
                            chatId: userId,
                            text: "К сожалению, Ваш заказ отменён по техническим причинам.");
                    break;
            }

            return;
        }

        private async Task GetOrderAsync()
        {
            int orderId = this.ParseCommand();
            var order = await this._administrationService.GetOrderAsync(orderId);
            var message = FormatOrderCaption(order);

            switch (order.Status)
            {
                case OrderStatus.Created:
                    await this._bot.SendMessage(
                        chatId: Update.CallbackQuery.From.Id,
                        text: message,
                        replyMarkup: new InlineKeyboardMarkup(
                        new[]
                        {
                        InlineKeyboardButton.WithCallbackData("📦 Передать в доставку.", AdministrationCommands.ToDelivery.Command + $"{order.OrderId}"),
                        InlineKeyboardButton.WithCallbackData("❌ Отменить.", AdministrationCommands.CancelOrder.Command + $"{order.OrderId}"),
                        InlineKeyboardButton.WithCallbackData("⬅️ Назад к заказам.", AdministrationCommands.ShowActiveOrders.Command + ":0")
                        }));
                    break;

                case OrderStatus.ToDelivery:
                    await this._bot.SendMessage(
                        chatId: Update.CallbackQuery.From.Id,
                        text: message,
                        replyMarkup: new InlineKeyboardMarkup(
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Назад к заказам.", AdministrationCommands.ShowCouriersOrders.Command + ":0")
                        }));
                    break;

                case OrderStatus.Completed:
                    await this._bot.SendMessage(
                        chatId: Update.CallbackQuery.From.Id,
                        text: message,
                        replyMarkup: new InlineKeyboardMarkup(
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⬅️ Назад к заказам.", AdministrationCommands.ShowCompletedOrders.Command + ":0")
                        }));
                    break;
            }


            return;
        }

        private async Task ShowOrders(string command)
        {
            int page = this.ParseCommand();

            var orderStatus = command switch
            {
                AdministrationCommands.SHOWACTIVEORDERS => OrderStatus.Created,
                AdministrationCommands.COURIERSORDERS => OrderStatus.ToDelivery,
                AdministrationCommands.SHOWCOMPLETEDORDERS => OrderStatus.Completed,
                _ => throw new Exception("Неизвестная команда.")
            };

            var buttonCommand = command switch
            {
                AdministrationCommands.SHOWACTIVEORDERS => AdministrationCommands.ShowActiveOrders.Command,
                AdministrationCommands.COURIERSORDERS => AdministrationCommands.ShowCouriersOrders.Command,
                AdministrationCommands.SHOWCOMPLETEDORDERS => AdministrationCommands.ShowCompletedOrders.Command,
                _ => throw new Exception("Неизвестная команда.")
            };

            var orders = await this._administrationService.GetOrdersAsync(orderStatus);

            if (orderStatus == OrderStatus.ToDelivery)
            {
                orders.AddRange(await this._administrationService.GetOrdersAsync(OrderStatus.InDelivery));
            }

            if (orders.Count == 0)
            {
                await this._bot.SendMessage(
                        chatId: Update.CallbackQuery.From.Id,
                        text: "Нет заказов.",
                        replyMarkup: new InlineKeyboardMarkup(
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Вернуться.", AdministrationCommands.ShowOrders.Command)
                        }));
                return;
            }

            int pageSize = GlobalParams.MENU_ITEMS_CNT;

            int total = orders.Count;
            int totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 0) page = 0;
            if (page >= totalPages) page = totalPages - 1;

            int skip = page * pageSize;
            var pageItems = orders.Skip(skip).Take(pageSize).ToList();

            var itemButtons = pageItems
                .Select(p => InlineKeyboardButton.WithCallbackData(
                    $"{p.DateOrder}",
                    AdministrationCommands.GetOrder + $"{p.OrderId}"))
                .Chunk(2)
                .Select(r => r.ToArray())
                .ToList();

            var navRow = new List<InlineKeyboardButton>();
            if (page > 0)
                navRow.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", buttonCommand + $":{page - 1}"));

            navRow.Add(InlineKeyboardButton.WithCallbackData("Вернуться.", AdministrationCommands.ShowOrders.Command));

            if (page < totalPages - 1)
                navRow.Add(InlineKeyboardButton.WithCallbackData("Далее ➡️", buttonCommand + $":{page + 1}"));

            var rows = new List<InlineKeyboardButton[]>();
            rows.AddRange(itemButtons);
            if (navRow.Count > 0) rows.Add(navRow.ToArray());

            string header = $"Заказы (стр. {page + 1}/{totalPages}):\nВыберите заказ, чтобы посмотреть детали.";

            await this._bot.SendMessage(
                chatId: Update.CallbackQuery.From.Id,
                text: header,
                replyMarkup: new InlineKeyboardMarkup(rows));

            return;
        }

        private static string FormatOrderCaption(Orders p)
        {
            var sb = new StringBuilder();
            sb.Append($"*{p.DateOrder}*\n *ID:* {p.OrderId}" +
                $"\n👤 *Пользователь:* {p.IdUser}" +
                $"\n📞 *Телефон:* {p.PhoneNumber}" +
                $"\n📍 *Адрес:* {p.Address}" +
                $"\n💰 *Сумма:* {p.Price} ₽" +
                $"\n📊 *Статус:* {GetStatusText(p.Status)}");

            if (p.IdCourier != null)
                sb.Append($"\n📦 *ID курьера*: {p.IdCourier}");

            if (!string.IsNullOrWhiteSpace(p.Comment))
                sb.Append($"\n💬 *Комментарий:* {p.Comment}");

            return sb.ToString();
        }

        private static InlineKeyboardMarkup GetHomeAdministrationKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Посмотреть заказы", AdministrationCommands.ShowOrders.Command),
                    InlineKeyboardButton.WithCallbackData("К меню", MenuCommand.GetPage.Command)
                },
            });
        }

        private static InlineKeyboardMarkup GetHomeOrdersKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📋 Активные заказы.", AdministrationCommands.ShowActiveOrders.Command + ":0"),
                    InlineKeyboardButton.WithCallbackData("📦 Заказы у курьеров.", AdministrationCommands.ShowCouriersOrders.Command + ":0"),
                    InlineKeyboardButton.WithCallbackData("✅ Завершённые заказы.", AdministrationCommands.ShowCompletedOrders.Command + ":0")
                },
            });
        }

        private int ParseCommand()
        {
            if (!int.TryParse(this.Update.CallbackQuery.Data.Split(CommandsBase.Separator)[2], out int data))
            {
                throw new Exception("Неверный формат команды");
            }

            return data;
        }

        private static string GetStatusText(string status) => status switch
        {
            "Created" => "🆕 Создан",
            "ToDelivery" => "🚗 Готов к доставке",
            "InDelivery" => "🚗 В доставке",
            "CourierArrived" => "📍 Курьер на месте",
            "Completed" => "✅ Завершен",
            "Cancelled" => "❌ Отменен",
            _ => status
        };
    }
}
