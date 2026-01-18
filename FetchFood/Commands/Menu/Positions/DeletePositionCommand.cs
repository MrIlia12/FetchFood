using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Positions
{
    /// <summary>
    /// Команда удаления позиции.
    /// </summary>
    public class DeletePositionCommand : BaseMenuCommand
    {
        public override string CommandKey => BotCommands.DELETE_POSITION;

        public override async Task<bool> ExecuteAsync(MenuCommandContext ctx)
        {
            var query = ctx.Args?.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                await SendMessageAsync(ctx,
                    $"Формат: {BotCommands.MENU}:{BotCommands.DELETE_POSITION}:<название>\n" +
                    $"Например: {BotCommands.MENU}:{BotCommands.DELETE_POSITION}:Бургер");
                return true;
            }

            try
            {
                var matches = await ctx.MenuService.SearchPositionsAsync(query, false, ctx.CancellationToken);

                if (matches.Count == 0)
                {
                    await SendMessageAsync(ctx, "❌ Позиции не найдены.");
                    return true;
                }

                if (matches.Count == 1)
                {
                    var p = matches[0];
                    var ok = await ctx.MenuService.DeleteAsync(p.PositionId, ctx.CancellationToken);
                    await SendMessageAsync(ctx,
                        ok ? $"🗑️ Удалено: {p.Name} (#{p.PositionId})" : "Не удалось удалить позицию.",
                        new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                    return true;
                }

                // Ищем точное совпадение
                var exact = matches.FirstOrDefault(p =>
                    string.Equals(p.Name, query, StringComparison.OrdinalIgnoreCase));

                if (exact is not null)
                {
                    var ok = await ctx.MenuService.DeleteAsync(exact.PositionId, ctx.CancellationToken);
                    await SendMessageAsync(ctx,
                        ok ? $"🗑️ Удалено: {exact.Name} (#{exact.PositionId})" : "Не удалось удалить позицию.");
                    return true;
                }

                // Просим уточнить
                var list = string.Join("\n", matches.Take(10).Select(p => $"#{p.PositionId}: {p.Name} ({p.Price:F2})"));
                await SendMessageAsync(ctx,
                    $"Найдено несколько позиций:\n{list}\n\n" +
                    $"Уточните название (Например: `{BotCommands.MENU}:{BotCommands.DELETE_POSITION}:Бургер классик`)");
            }
            catch (Exception ex)
            {
                await SendMessageAsync(ctx, "⚠️ Ошибка при удалении позиции.");
                Console.WriteLine($"[DelPos ERROR]: {ex}");
            }

            return true;
        }
    }
}
