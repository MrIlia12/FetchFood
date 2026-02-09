using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Positions
{
    /// <summary>
    /// Команда отображения меню редактирования позиции.
    /// </summary>
    public class EditPositionMenuCommand : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.EDIT;

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            if (!int.TryParse(ctx.Args, out var positionId))
            {
                await SendMessageAsync(ctx, "Некорректный ID позиции.");
                return true;
            }

            var position = await ctx.MenuService.GetPositionAsync(positionId, ctx.CancellationToken);
            if (position == null)
            {
                await SendMessageAsync(ctx, "Позиция не найдена.");
                return true;
            }

            var rows = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Название", $"{BotCommands.MENU}:{BotCommands.EDIT_NAME}:{positionId}:"),
                    InlineKeyboardButton.WithCallbackData("Цена", $"{BotCommands.MENU}:{BotCommands.EDIT_PRICE}:{positionId}:")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Состав", $"{BotCommands.MENU}:{BotCommands.EDIT_INGREDIENTS}:{positionId}:"),
                    InlineKeyboardButton.WithCallbackData("Описание", $"{BotCommands.MENU}:{BotCommands.EDIT_DESCRIPTION}:{positionId}:")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Изображение", $"{BotCommands.MENU}:{BotCommands.EDIT_IMAGE}:{positionId}:"),
                    InlineKeyboardButton.WithCallbackData("Категория", $"{BotCommands.MENU}:{BotCommands.EDIT_POS_CATEGORY}:{positionId}:")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"{BotCommands.MENU}:{BotCommands.POSITION}:{positionId}")
                }
            };

            var categoryName = position.Category?.Name ?? "Без категории";

            await SendMessageAsync(ctx,
                $"Редактирование позиции «{position.Name}»:\n\n" +
                $"Название: {position.Name}\n" +
                $"Цена: {position.Price:0.##}\n" +
                $"Состав: {position.Ingredients ?? "—"}\n" +
                $"Описание: {position.Description ?? "—"}\n" +
                $"Изображение: {(string.IsNullOrEmpty(position.Image) ? "—" : "Установлено")}\n" +
                $"Категория: {categoryName}",
                new InlineKeyboardMarkup(rows));

            return true;
        }
    }
}
