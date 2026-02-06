using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Navigation
{
    /// <summary>
    /// Команда отображения страницы меню.
    /// </summary>
    public class PageCommand : BaseMenuCommand
    {
        public override string CommandKey => BotCommands.PAGE;

        public override async Task<bool> ExecuteAsync(MenuCommandContext ctx)
        {
            if (!int.TryParse(ctx.Args, out var page))
            {
                await SendMessageAsync(ctx, "Не удалось распознать номер страницы меню.");
                return true;
            }

            var positions = await ctx.MenuService.GetActivePositionsAsync(ctx.CancellationToken);
            int pageSize = GlobalParams.MENU_ITEMS_CNT;

            if (positions.Count == 0)
            {
                await SendMessageAsync(ctx, "Пока нет доступных позиций.");
                return true;
            }

            positions = positions.OrderBy(p => p.Name).ToList();

            int total = positions.Count;
            int totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 0) page = 0;
            if (page >= totalPages) page = totalPages - 1;

            int skip = page * pageSize;
            var pageItems = positions.Skip(skip).Take(pageSize).ToList();

            var itemButtons = pageItems
                .Select(p => InlineKeyboardButton.WithCallbackData(
                    $"{p.Name} — {FormatPrice(p.Price)}",
                    $"{BotCommands.MENU}:{BotCommands.POSITION}:{p.PositionId}"))
                .Chunk(2)
                .Select(r => r.ToArray())
                .ToList();

            var navRow = new List<InlineKeyboardButton>();
            if (page > 0)
                navRow.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"{BotCommands.MENU}:{BotCommands.PAGE}:{page - 1}"));
            if (page < totalPages - 1)
                navRow.Add(InlineKeyboardButton.WithCallbackData("Далее ➡️", $"{BotCommands.MENU}:{BotCommands.PAGE}:{page + 1}"));

            var bottomRow = new[]
            {
                InlineKeyboardButton.WithCallbackData("📂 Категории", $"{BotCommands.MENU}:{BotCommands.CATEGORIES}"),
                InlineKeyboardButton.WithCallbackData("🛒 Корзина", BotCommands.CART_SHOW)
            };

            var rows = new List<InlineKeyboardButton[]>();
            rows.AddRange(itemButtons);
            if (navRow.Count > 0) rows.Add(navRow.ToArray());

            // Показываем кнопку добавления только админам
            var isAdmin = await ctx.IsAdminAsync();
            if (isAdmin)
            {
                bottomRow = new[]
                {
                    InlineKeyboardButton.WithCallbackData("📂 Категории", $"{BotCommands.MENU}:{BotCommands.CATEGORIES}"),
                    InlineKeyboardButton.WithCallbackData("К консоли администратора", AdministrationCommands.ToHomeConsole.Command)
                };

                var actionRow = new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Добавить", $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:start")
                };
                rows.Add(actionRow);
            }

            rows.Add(bottomRow);

            string header = $"Меню (стр. {page + 1}/{totalPages}):\nВыберите позицию, чтобы посмотреть детали.";

            await SendMessageAsync(ctx, header, new InlineKeyboardMarkup(rows));
            return true;
        }
    }
}
