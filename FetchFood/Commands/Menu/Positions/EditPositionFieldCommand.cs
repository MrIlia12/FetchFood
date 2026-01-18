using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Positions
{
    /// <summary>
    /// Базовый класс для команд редактирования отдельных полей позиции.
    /// </summary>
    public abstract class EditPositionFieldCommand : AdminMenuCommand
    {
        protected abstract string FieldType { get; }
        protected abstract string FieldNameRu { get; }

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            var parts = ctx.Args.Split(':', 2, StringSplitOptions.TrimEntries);

            if (parts.Length < 1 || !int.TryParse(parts[0], out var positionId))
            {
                await SendMessageAsync(ctx, "Некорректный формат команды.");
                return true;
            }

            var position = await ctx.MenuService.GetPositionAsync(positionId, ctx.CancellationToken);
            if (position == null)
            {
                await SendMessageAsync(ctx, "Позиция не найдена.");
                return true;
            }

            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                await SendMessageAsync(ctx,
                    $"Введите новое {FieldNameRu}:\n{BotCommands.MENU}:{CommandKey}:{positionId}:НовоеЗначение",
                    new ForceReplyMarkup { Selective = true });
                return true;
            }

            var newValue = parts[1].Trim();

            if (!await ApplyValueAsync(ctx, position, newValue))
            {
                return true;
            }

            var result = await ctx.MenuService.UpdateAsync(position, ctx.CancellationToken);

            if (result)
            {
                await SendMessageAsync(ctx,
                    $"✅ {char.ToUpper(FieldNameRu[0])}{FieldNameRu[1..]} позиции обновлено!",
                    new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("✏️ Продолжить редактирование", $"{BotCommands.MENU}:{BotCommands.EDIT}:{positionId}") },
                        new[] { InlineKeyboardButton.WithCallbackData("⬅️ К позиции", $"{BotCommands.MENU}:{BotCommands.POSITION}:{positionId}") }
                    }));
            }
            else
            {
                await SendMessageAsync(ctx, "❌ Не удалось обновить позицию.");
            }

            return true;
        }

        protected abstract Task<bool> ApplyValueAsync(MenuCommandContext ctx, DataAccess.Entities.Position position, string value);
    }

    public class EditNameCommand : EditPositionFieldCommand
    {
        public override string CommandKey => BotCommands.EDIT_NAME;
        protected override string FieldType => "name";
        protected override string FieldNameRu => "название";

        protected override Task<bool> ApplyValueAsync(MenuCommandContext ctx, DataAccess.Entities.Position position, string value)
        {
            if (value.Length > 100)
            {
                SendMessageAsync(ctx, "Название должно быть <= 100 символов.");
                return Task.FromResult(false);
            }
            position.Name = value;
            return Task.FromResult(true);
        }
    }

    public class EditPriceCommand : EditPositionFieldCommand
    {
        public override string CommandKey => BotCommands.EDIT_PRICE;
        protected override string FieldType => "price";
        protected override string FieldNameRu => "цену";

        protected override Task<bool> ApplyValueAsync(MenuCommandContext ctx, DataAccess.Entities.Position position, string value)
        {
            if (!decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var price) || price <= 0)
            {
                SendMessageAsync(ctx, "Некорректная цена. Используйте точку для десятичного разделителя.");
                return Task.FromResult(false);
            }
            position.Price = price;
            return Task.FromResult(true);
        }
    }

    public class EditIngredientsCommand : EditPositionFieldCommand
    {
        public override string CommandKey => BotCommands.EDIT_INGREDIENTS;
        protected override string FieldType => "ingredients";
        protected override string FieldNameRu => "состав";

        protected override Task<bool> ApplyValueAsync(MenuCommandContext ctx, DataAccess.Entities.Position position, string value)
        {
            position.Ingredients = value;
            return Task.FromResult(true);
        }
    }

    public class EditDescriptionCommand : EditPositionFieldCommand
    {
        public override string CommandKey => BotCommands.EDIT_DESCRIPTION;
        protected override string FieldType => "description";
        protected override string FieldNameRu => "описание";

        protected override Task<bool> ApplyValueAsync(MenuCommandContext ctx, DataAccess.Entities.Position position, string value)
        {
            position.Description = value;
            return Task.FromResult(true);
        }
    }

    public class EditImageCommand : EditPositionFieldCommand
    {
        public override string CommandKey => BotCommands.EDIT_IMAGE;
        protected override string FieldType => "image";
        protected override string FieldNameRu => "URL изображения";

        protected override Task<bool> ApplyValueAsync(MenuCommandContext ctx, DataAccess.Entities.Position position, string value)
        {
            position.Image = value;
            return Task.FromResult(true);
        }
    }
}
