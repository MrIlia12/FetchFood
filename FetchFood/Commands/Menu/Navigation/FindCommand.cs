using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Navigation
{
    /// <summary>
    /// Команда поиска позиций по названию.
    /// </summary>
    public class FindCommand : BaseMenuCommand
    {
        public override string CommandKey => BotCommands.FIND;

        public override async Task<bool> ExecuteAsync(MenuCommandContext ctx)
        {
            var query = ctx.Args?.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                await SendMessageAsync(ctx,
                    $"Формат команды: {BotCommands.MENU}:{BotCommands.FIND}:<часть названия>\n" +
                    $"Например: {BotCommands.MENU}:{BotCommands.FIND}:бургер");
                return true;
            }

            var results = await ctx.MenuService.SearchPositionsAsync(query, true, ctx.CancellationToken);

            if (results.Count == 0)
            {
                await SendMessageAsync(ctx, $"Ничего не нашёл по запросу: «{query}»");
                return true;
            }

            int take = Math.Min(20, results.Count);
            var rows = results
                .Take(take)
                .Select(p => InlineKeyboardButton.WithCallbackData(
                    $"{p.Name} — {FormatPrice(p.Price)}",
                    $"{BotCommands.MENU}:{BotCommands.POSITION}:{p.PositionId}"))
                .Chunk(2)
                .Select(r => r.ToArray())
                .ToArray();

            await SendMessageAsync(ctx,
                $"Нашёл {results.Count} позиций. Показано {take}. Выберите нужную:",
                new InlineKeyboardMarkup(rows));

            return true;
        }
    }
}
