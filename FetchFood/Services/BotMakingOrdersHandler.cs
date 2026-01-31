using BusinessLogic.Services.MakingOrders.Abstractions;
using DataAccess.Entities;
using FetchFood.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;


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
                // Обработка кнопок
                if (Update.CallbackQuery != null)
                {
                    await HandleCallbackQueryAsync();
                    return;
                }

                // Обработка текстовых сообщений
                if (Update.Message != null)
                {
                    await HandleMessageAsync();
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
                        replyMarkup: new ForceReplyMarkup { Selective = true },
                        text: BotCommands.ORDER1);
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
                // Нужно для следующего вывода сообщений
                await SendOrderResultMessage(chatId, orderResult);
            }
            
            await _bot.AnswerCallbackQuery(callback.Id);
        }

        private async Task HandleMessageAsync()
        {
            var message = Update.Message;
            // Обработка других команд оформления заказа
            var orderResult = await _makingOrdersService.ProcessUserInputAsync(message.From.Id, message.Text);
            // Нужно для следующего вывода сообщений
            await SendOrderResultMessage(message.Chat.Id, orderResult);
        }

        /// <summary>
        /// Отпавка последующих сообщений после полученного и обработанного ввода от пользователя
        /// </summary>
        /// <param name="chatId">ID чата</param>
        /// <param name="result">Результат обработки</param>
        private async Task SendOrderResultMessage(long chatId, OrderProcessingResult result)
        {
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
