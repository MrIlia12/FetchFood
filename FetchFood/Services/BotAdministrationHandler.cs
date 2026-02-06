using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using BusinessLogic.Services.Administration.Abstraction;
using Telegram.Bot;
using FetchFood.States;
using System.Collections.Concurrent;
using FetchFood.Commands;
using System.Diagnostics;
using DataAccess.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static System.Net.Mime.MediaTypeNames;

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
                // Обработка кнопок
                if (Update.CallbackQuery != null)
                {
                    await HandleCallbackQueryAsync();
                    return;
                }

                // Обработка текстовых сообщений
                if (Update.Message != null)
                {
                    ////await HandleMessageAsync();
                    ////return;
                }
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

            if (callback.Data == AdministrationCommands.ToHomeConsole.Command)
            {
                await this._bot.SendMessage(
                                chatId: userId,
                                text: $"Консоль администратора.",
                                replyMarkup: GetHomeAdministrationKeyboard());
                return;
            }

            if (callback.Data == AdministrationCommands.ShowOrders.Command)
            {
                await this._bot.SendMessage(
                                chatId: userId,
                                text: $"Консоль администратора.",
                                replyMarkup: GetHomeOrdersKeyboard());
                return;
            }

            if (callback.Data == AdministrationCommands.ShowActiveOrders.Command)
            {
                await this.ShowActiveOrders();
            }

            await _bot.AnswerCallbackQuery(callback.Id);
        }

        private async Task ShowActiveOrders()
        {
            int page;

            if (!int.TryParse(Update.CallbackQuery.Data.Split(AdministrationCommands.Separator)[2], out page))
            { 
                throw new Exception("Неверный формат команды");
            }

            var orders = await this._administrationService.GetOrdersAsync("Created");
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
                    $"{BotCommands.MENU}:{BotCommands.POSITION}:{p.OrderId}"))
                .Chunk(2)
                .Select(r => r.ToArray())
                .ToList();

            var navRow = new List<InlineKeyboardButton>();
            if (page > 0)
                navRow.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", AdministrationCommands.ShowActiveOrders.Command + $":{page - 1}"));
            if (page < totalPages - 1)
                navRow.Add(InlineKeyboardButton.WithCallbackData("Далее ➡️", AdministrationCommands.ShowActiveOrders.Command + $":{page + 1}"));

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
                    InlineKeyboardButton.WithCallbackData("Активные заказы.", AdministrationCommands.ShowActiveOrders.Command + ":0"),
                    InlineKeyboardButton.WithCallbackData("Заказы у курьеров.", AdministrationCommands.ShowCouriersOrders.Command),
                    InlineKeyboardButton.WithCallbackData("Завершённые заказы.", AdministrationCommands.ShowCompletedOrders.Command)
                },
            });
        }
    }
}
