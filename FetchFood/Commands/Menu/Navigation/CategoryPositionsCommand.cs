using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Navigation
{
    /// <summary>
    /// Команда отображения позиций в категории.
    /// </summary>
    public class CategoryPositionsCommand : BaseMenuCommand
    {
        public override string CommandKey => BotCommands.CATEGORY_POSITIONS;

        public override async Task<bool> ExecuteAsync(MenuCommandContext ctx)
        {
            var categoryArgs = ctx.Args.Split(':', StringSplitOptions.TrimEntries);

            if (categoryArgs.Length < 1 || !int.TryParse(categoryArgs[0], out var categoryId))
            {
                await SendMessageAsync(ctx, "Не удалось распознать ID категории.");
                return true;
            }

            int page = 0;
            if (categoryArgs.Length >= 2 && int.TryParse(categoryArgs[1], out var parsedPage))
            {
                page = parsedPage;
            }

            var category = await ctx.CategoryService.GetCategoryByIdAsync(categoryId, ctx.CancellationToken);
            if (category == null)
            {
                await SendMessageAsync(ctx, "Категория не найдена.");
                return true;
            }

            var positions = await ctx.MenuService.GetActivePositionsByCategoryAsync(categoryId, ctx.CancellationToken);
            int total = positions.Count;
            int totalPages = (int)Math.Ceiling(total / (double)GlobalParams.MENU_ITEMS_CNT);
            if (totalPages == 0) totalPages = 1;
            if (page < 0) page = 0;
            if (page >= totalPages) page = totalPages - 1;

            int skip = page * GlobalParams.MENU_ITEMS_CNT;
            var pageItems = positions.Skip(skip).Take(GlobalParams.MENU_ITEMS_CNT).ToList();

            if (pageItems.Count == 0)
            {
                await SendMessageAsync(ctx,
                    $"В категории «{category.Name}» пока нет позиций.",
                    new InlineKeyboardMarkup(new[]
                    {
                        new[] { BackToCategoriesButton() }
                    }));
                return true;
            }

            var itemButtons = pageItems
                .Select(p => InlineKeyboardButton.WithCallbackData(
                    $"{p.Name} — {FormatPrice(p.Price)}",
                    $"{BotCommands.MENU}:{BotCommands.POSITION}:{p.PositionId}"))
                .Chunk(2)
                .Select(r => r.ToArray())
                .ToList();

            var navRow = new List<InlineKeyboardButton>();
            if (page > 0)
                navRow.Add(InlineKeyboardButton.WithCallbackData(
                    "⬅️ Назад",
                    $"{BotCommands.MENU}:{BotCommands.CATEGORY_POSITIONS}:{categoryId}:{page - 1}"));
            if (page < totalPages - 1)
                navRow.Add(InlineKeyboardButton.WithCallbackData(
                    "Далее ➡️",
                    $"{BotCommands.MENU}:{BotCommands.CATEGORY_POSITIONS}:{categoryId}:{page + 1}"));

            var rows = new List<InlineKeyboardButton[]>();
            rows.AddRange(itemButtons);
            if (navRow.Count > 0) rows.Add(navRow.ToArray());
            rows.Add(new[] { BackToCategoriesButton() });

            await SendMessageAsync(ctx,
                $"Категория: {category.Name}\nСтраница {page + 1}/{totalPages}:",
                new InlineKeyboardMarkup(rows));

            return true;
        }
    }
}
