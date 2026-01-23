using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Positions
{
    /// <summary>
    /// Команда удаления позиции по ID.
    /// </summary>
    public class DeletePositionCommand : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.DELETE_POSITION;

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            // Ожидаем ID позиции
            if (!int.TryParse(ctx.Args, out var positionId))
            {
                await SendMessageAsync(ctx,
                    "Некорректный ID позиции.",
                    new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                return true;
            }

            try
            {
                var position = await ctx.MenuService.GetPositionAsync(positionId, ctx.CancellationToken);

                if (position == null)
                {
                    await SendMessageAsync(ctx,
                        "Позиция не найдена.",
                        new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                    return true;
                }

                var ok = await ctx.MenuService.DeleteAsync(positionId, ctx.CancellationToken);

                if (ok)
                {
                    await SendMessageAsync(ctx,
                        $"🗑️ Удалено: {position.Name} (#{position.PositionId})",
                        new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                }
                else
                {
                    await SendMessageAsync(ctx,
                        "❌ Не удалось удалить позицию.",
                        new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                }
            }
            catch (Exception ex)
            {
                await SendMessageAsync(ctx,
                    "⚠️ Ошибка при удалении позиции.",
                    new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                Console.WriteLine($"[DelPos ERROR]: {ex}");
            }

            return true;
        }
    }
}
