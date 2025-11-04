using BusinessLogic.Services.MakingOrders.Abstractions;
using DataAccess.Entities;
using DataAccess.Entities.Models;
using FetchFood.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Services
{
    public class TelegramBotMakingOrdersService : ITelegramBotMakingOrdersService
    {
        private readonly IMakingOrdersService _makingOrdersService;
        private readonly ILogger<TelegramBotMakingOrdersService> _logger;
        private readonly string[] orderButtons = { "add_comment", "skip_comment", "confirm_order", "cancel_order" };

        public TelegramBotMakingOrdersService(IMakingOrdersService makingOrdersService, ILogger<TelegramBotMakingOrdersService> logger)
        {
            _makingOrdersService = makingOrdersService;
            _logger = logger;
        }

        public async Task HandleOrderCallbackAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
        {
            await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);

            // Нажата кнопка "Оформить заказ" (обработка)
            if (callbackQuery.Data == "start_order")
            {
                // Начинаем процесс оформления заказа
                await bot.SendMessage(
                    callbackQuery.From.Id,
                    "📝 Введите адрес доставки в формате:\nул. <улица>, д. <номер дома>, кв. <номер квартиры>\n\n" +
                    "Пример: ул. Ленина, д. 15, кв. 42\n\n" +
                    "Допустимые форматы дома: 15, 15а, 15/1, 15/1а",
                    cancellationToken: ct);
                await _makingOrdersService.StartOrderCreationAsync(callbackQuery.From.Id);
            }

            // Обработка кнопок, связанных с оформлением заказа
            if (IsOrderButton(callbackQuery.Data))
            {
                OrderProcessingResult orderResult = await _makingOrdersService.ProcessUserInputAsync(callbackQuery.From.Id, callbackQuery.Data);
                await SendOrderResultMessageAsync(bot, callbackQuery.Message.Chat.Id, orderResult, ct);
            }
            else
            {
                await bot.AnswerCallbackQuery(callbackQuery.Id, "Неизвестная команда в сервисе оформления заказа", cancellationToken: ct);
            }
        }

        public async Task SendOrderResultMessageAsync(ITelegramBotClient bot, long chatId, OrderProcessingResult result, CancellationToken ct)
        {
            if (result.Success)
            {
                if (result.HasInlineKeyboard && result.InlineKeyboard != null)
                {
                    await bot.SendMessage(
                        chatId,
                        result.Message,
                        replyMarkup: result.InlineKeyboard,
                        cancellationToken: ct);
                }
                else
                {
                    await bot.SendMessage(
                        chatId,
                        result.Message,
                        cancellationToken: ct);
                }
            }
            else
            {
                await bot.SendMessage(
                    chatId,
                    result.Message,
                    cancellationToken: ct);
            }
        }

        public bool IsOrderButton(string callbackData)
        {
            return orderButtons.Contains(callbackData);
        }
    }
}