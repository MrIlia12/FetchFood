using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu
{
    /// <summary>
    /// Базовый класс для команд меню с общими утилитами.
    /// </summary>
    public abstract class BaseMenuCommand : IMenuCommand
    {
        public abstract string CommandKey { get; }

        public abstract Task<bool> ExecuteAsync(MenuCommandContext context);

        /// <summary>
        /// Отправить текстовое сообщение
        /// </summary>
        protected static Task SendMessageAsync(
            MenuCommandContext ctx,
            string text,
            ReplyMarkup? replyMarkup = null)
        {
            return ctx.Bot.SendMessage(
                chatId: ctx.ChatId,
                text: text,
                replyMarkup: replyMarkup,
                cancellationToken: ctx.CancellationToken);
        }

        /// <summary>
        /// Форматирование цены
        /// </summary>
        protected static string FormatPrice(decimal price) => $"{price:0.##}";

        /// <summary>
        /// Парсинг аргументов вида "id:value"
        /// </summary>
        protected static (int? id, string? value) ParseIdAndValue(string args)
        {
            var parts = args.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length < 1 || !int.TryParse(parts[0], out var id))
                return (null, null);

            var value = parts.Length >= 2 ? parts[1] : null;
            return (id, value);
        }

        /// <summary>
        /// Кнопка "Назад к меню"
        /// </summary>
        protected static InlineKeyboardButton BackToMenuButton() =>
            InlineKeyboardButton.WithCallbackData("⬅️ Назад к меню", $"{BotCommands.MENU}:{BotCommands.BACK}");

        /// <summary>
        /// Кнопка "К категориям"
        /// </summary>
        protected static InlineKeyboardButton BackToCategoriesButton() =>
            InlineKeyboardButton.WithCallbackData("📂 К категориям", $"{BotCommands.MENU}:{BotCommands.CATEGORIES}");
    }

    /// <summary>
    /// Базовый класс для команд, требующих прав администратора.
    /// </summary>
    public abstract class AdminMenuCommand : BaseMenuCommand
    {
        public override async Task<bool> ExecuteAsync(MenuCommandContext context)
        {
            var isAdmin = await context.IsAdminAsync();

            if (!isAdmin)
            {
                await SendMessageAsync(context, "Недостаточно прав для выполнения этой операции.");
                return true;
            }

            return await ExecuteAdminAsync(context);
        }

        /// <summary>
        /// Выполнить команду администратора
        /// </summary>
        protected abstract Task<bool> ExecuteAdminAsync(MenuCommandContext context);
    }
}
