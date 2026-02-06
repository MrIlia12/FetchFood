using DataAccess.Entities.Models;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Positions
{
    /// <summary>
    /// Команда подтверждения удаления позиции.
    /// Показывает детали позиции и кнопки подтверждения/отмены.
    /// </summary>
    public class ConfirmDeletePositionCommand : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.CONFIRM_DELETE;

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            if (!int.TryParse(ctx.Args, out var positionId))
            {
                await SendMessageAsync(ctx,
                    "Некорректный ID позиции.",
                    new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                return true;
            }

            var position = await ctx.MenuService.GetPositionAsync(positionId, ctx.CancellationToken);

            if (position == null || position.Status != PositionStatus.Active)
            {
                await SendMessageAsync(ctx,
                    "Позиция не найдена или уже удалена.",
                    new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                return true;
            }

            var sb = new StringBuilder();
            sb.AppendLine("⚠️ Удалить позицию?");
            sb.AppendLine();
            sb.AppendLine($"📝 {position.Name}");
            sb.AppendLine($"💰 {FormatPrice(position.Price)} ₽");

            if (position.Category != null)
                sb.AppendLine($"📂 {position.Category.Name}");

            if (!string.IsNullOrWhiteSpace(position.Ingredients))
                sb.AppendLine($"🥗 {position.Ingredients}");

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"{BotCommands.MENU}:{BotCommands.DELETE_POSITION}:{positionId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", $"{BotCommands.MENU}:{BotCommands.POSITION}:{positionId}")
                }
            });

            await SendMessageAsync(ctx, sb.ToString(), keyboard);
            return true;
        }
    }
}
