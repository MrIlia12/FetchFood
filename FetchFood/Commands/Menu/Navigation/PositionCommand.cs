using DataAccess.Entities;
using DataAccess.Entities.Models;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Navigation
{
    /// <summary>
    /// Команда отображения информации о позиции.
    /// </summary>
    public class PositionCommand : BaseMenuCommand
    {
        public override string CommandKey => BotCommands.POSITION;

        public override async Task<bool> ExecuteAsync(MenuCommandContext ctx)
        {
            if (!int.TryParse(ctx.Args, out var posNum))
            {
                await SendMessageAsync(ctx, "Не удалось распознать номер позиции меню.");
                return true;
            }

            string mssgTxt;
            var pos = await ctx.MenuService.GetPositionAsync(posNum, ctx.CancellationToken);

            if (pos is null || pos.Status != PositionStatus.Active)
            {
                posNum = -1;
                mssgTxt = "Эта позиция недоступна";
            }
            else
            {
                mssgTxt = FormatPositionCaption(pos);
            }

            var keyboard = await BuildKeyboardAsync(posNum, ctx);

            if (!string.IsNullOrWhiteSpace(pos?.Image))
            {
                try
                {
                    await SendPhotoAsync(ctx, pos.Image, mssgTxt, keyboard);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException)
                {
                    // Невалидный file_id — показываем текст
                    await SendMessageAsync(ctx, mssgTxt, keyboard);
                }
            }
            else
            {
                await SendMessageAsync(ctx, mssgTxt, keyboard);
            }

            return true;
        }

        private static string FormatPositionCaption(Position p)
        {
            var sb = new StringBuilder();
            sb.Append($"{(p.Name ?? "").Trim()}\nЦена: {FormatPrice(p.Price)}");

            if (p.Category != null)
                sb.Append($"\nКатегория: {p.Category.Name}");

            if (!string.IsNullOrWhiteSpace(p.Ingredients))
                sb.Append($"\nСостав: {p.Ingredients}");

            if (!string.IsNullOrWhiteSpace(p.Description))
                sb.Append($"\n\n{p.Description}");

            return sb.ToString();
        }

        private static async Task<InlineKeyboardMarkup> BuildKeyboardAsync(int posNum, MenuCommandContext ctx)
        {
            var isAdmin = await ctx.IsAdminAsync();
            var buttons = new List<InlineKeyboardButton[]>();

            if (posNum != -1)
            {
                var mainRow = new List<InlineKeyboardButton>();

                if (isAdmin)
                {
                    mainRow.Add(InlineKeyboardButton.WithCallbackData(
                        "✏️ Редактировать",
                        $"{BotCommands.MENU}:{BotCommands.EDIT}:{posNum}"));
                    mainRow.Add(InlineKeyboardButton.WithCallbackData(
                        "🗑 Удалить",
                        $"{BotCommands.MENU}:{BotCommands.CONFIRM_DELETE}:{posNum}"));
                }
                else
                {
                    mainRow.Add(InlineKeyboardButton.WithCallbackData(
                        "Добавить в корзину",
                        $"{BotCommands.CART}{CommandsBase.Separator}{BotCommands.CART_ADD} {posNum} {1}"));
                }

                    buttons.Add(mainRow.ToArray());
            }

            buttons.Add(new[] { BackToMenuButton() });
            return new InlineKeyboardMarkup(buttons);
        }
    }
}
