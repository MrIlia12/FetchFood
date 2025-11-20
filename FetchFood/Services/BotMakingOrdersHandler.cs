using BusinessLogic.Services.MakingOrders.Abstractions;
using DataAccess.Entities;
using FetchFood.Commands;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;


namespace FetchFood.Services
{
    /// <summary>
    /// Обработчик для сервиса оформления заказов
    /// </summary>
    class BotMakingOrdersHandler : BotCommandHandler
    {
        private readonly IMakingOrdersService _makingOrdersService;
        public BotMakingOrdersHandler(Update update, ITelegramBotClient botClient, IMakingOrdersService makingOrdersService) : base(update, botClient)
        {
            _makingOrdersService = makingOrdersService;
        }

        public override async void Invoke()
        {
            try
            {
                // Обработка callback-запросов
                if (Update.CallbackQuery != null)
                {
                    Console.WriteLine($"Update.CallbackQuery = {Update.CallbackQuery.Message}");
                    await HandleCallbackQueryAsync();
                    return;
                }

                // Обработка текстовых сообщений
                if (Update.Message != null)
                {
                    Console.WriteLine($"Update.Message = {Update.Message}");
                    await HandleMessageAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка оформления заказа: {ex}");
                await _bot.SendMessage(
                    chatId: Update.Message.Chat.Id,
                    text: "❌ Произошла ошибка при оформлении заказа. Пожалуйста, попробуйте позже.");
            }
        }

        private async Task HandleCallbackQueryAsync()
        {
            var callback = Update.CallbackQuery;
            var userId = callback.From.Id;
            var chatId = callback.Message.Chat.Id;

            if (callback.Data == MakingOrdersCommand.StartOrder.Command)
            {
                bool success = await _makingOrdersService.StartOrderCreationAsync(userId);
                if (success)
                {
                    await _bot.SendMessage(
                        chatId: chatId,
                        text: "📝 Введите адрес доставки в формате:\nул. <улица>, д. <номер дома>, кв. <номер квартиры>\n\n" +
                              "Пример: ул. Ленина, д. 15, кв. 42\n\n" +
                              "Допустимые форматы дома: 15, 15а, 15/1, 15/1а");
                }
                else
                {
                    await _bot.SendMessage(
                        chatId: chatId,
                        text: "❌ Не удалось начать оформление заказа. Попробуйте позже.");
                }               
            }
            else
            {
                // Передаем команду от кнопки в сервис оформления заказа
                var orderResult = await _makingOrdersService.ProcessUserInputAsync(userId, callback.Data);
                await SendOrderResultMessage(chatId, orderResult);
            }
            
            await _bot.AnswerCallbackQuery(callback.Id);
        }

        /// <summary>
        /// Обработка текстовых сообщений от пользователя
        /// </summary>
        /// <param name="message">Команда</param>
        private async Task HandleMessageAsync()
        {
            var message = Update.Message;
            // Обработка других команд оформления заказа
            var orderResult = await _makingOrdersService.ProcessUserInputAsync(message.From.Id, message.Text);
            await SendOrderResultMessage(message.Chat.Id, orderResult);
        }


        /// <summary>
        /// Начало процесса оформления заказа
        /// </summary>
        /// <param name="message">Команда</param>
        private async Task MakingOrdersCommandAsync(Message message)
        {
            bool success = await _makingOrdersService.StartOrderCreationAsync(message.From.Id);
            if (success)
            {
                // запрашиваем адрес
                await _bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: "📝 Введите адрес доставки в формате:\nул. <улица>, д. <номер дома>, кв. <номер квартиры>\n\n" +
                          "Пример: ул. Ленина, д. 15, кв. 42\n\n" +
                          "Допустимые форматы дома: 15, 15а, 15/1, 15/1а");
            }
            else
            {
                await _bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Не удалось начать оформление заказа. Попробуйте позже.");
            }
        }

        /// <summary>
        /// Обработка команд сервиса оформления заказов
        /// </summary>
        /// <param name="message">Команда</param>
        private async Task ProcessMakingOrdersCommandAsync(Message message)
        {
            var orderResult = await _makingOrdersService.ProcessUserInputAsync(message.From.Id, message.Text);

            if (orderResult.Success || !string.IsNullOrEmpty(orderResult.Message))
            {
                Console.WriteLine($"ProcessMakingOrdersCommandAsync: SendOrderResultMessage");
                await SendOrderResultMessage(message.Chat.Id, orderResult);
            }
            else
            {
                await _bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Неизвестная команда оформления заказа.");
            }
        }

        /// <summary>
        /// Отправляет результат обработки введенного текста пользователем на текущем шаге оформления заказа
        /// </summary>
        /// <param name="chatId">ID чата</param>
        /// <param name="result">Результат обработки</param>
        private async Task SendOrderResultMessage(long chatId, OrderProcessingResult result)
        {
            Console.WriteLine($"start SendOrderResultMessage");
            if (result.Success)
            {
                if (result.HasInlineKeyboard && result.InlineKeyboard != null)
                {
                    await _bot.SendMessage(
                        chatId: chatId,
                        text: result.Message,
                        replyMarkup: result.InlineKeyboard);
                }
                else
                {
                    await _bot.SendMessage(
                        chatId: chatId,
                        text: result.Message);
                }
            }
            else
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: result.Message);
            }
        }
    }
}
