using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Positions
{
    /// <summary>
    /// Команда выбора позиции для удаления (кнопочный интерфейс).
    /// </summary>
    public class SelectPositionToDeleteCommand : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.DELETE;

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            // Парсим страницу из аргументов (по умолчанию 0)
            int page = 0;
            if (!string.IsNullOrWhiteSpace(ctx.Args) && int.TryParse(ctx.Args, out var parsedPage))
            {
                page = parsedPage;
            }

            var positions = await ctx.MenuService.GetActivePositionsAsync(ctx.CancellationToken);

            if (positions.Count == 0)
            {
                await SendMessageAsync(ctx,
                    "Нет позиций для удаления.",
                    new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                return true;
            }

            positions = positions.OrderBy(p => p.Name).ToList();

            int pageSize = 8; // Меньше, т.к. позиций обычно больше, чем категорий
            int total = positions.Count;
            int totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 0) page = 0;
            if (page >= totalPages) page = totalPages - 1;

            int skip = page * pageSize;
            var pageItems = positions.Skip(skip).Take(pageSize).ToList();

            var rows = pageItems
                .Select(p => new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"🗑 {p.Name} — {FormatPrice(p.Price)}",
                        $"{BotCommands.MENU}:{BotCommands.DELETE_POSITION}:{p.PositionId}")
                })
                .ToList();

            // Кнопки навигации
            var navRow = new List<InlineKeyboardButton>();
            if (page > 0)
                navRow.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"{BotCommands.MENU}:{BotCommands.DELETE}:{page - 1}"));
            if (page < totalPages - 1)
                navRow.Add(InlineKeyboardButton.WithCallbackData("Далее ➡️", $"{BotCommands.MENU}:{BotCommands.DELETE}:{page + 1}"));

            if (navRow.Count > 0)
                rows.Add(navRow.ToArray());

            rows.Add(new[] { BackToMenuButton() });

            string header = totalPages > 1
                ? $"Выберите позицию для удаления (стр. {page + 1}/{totalPages}):"
                : "Выберите позицию для удаления:";

            await SendMessageAsync(ctx, header, new InlineKeyboardMarkup(rows));
            return true;
        }
    }
}
