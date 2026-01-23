namespace FetchFood.Commands.Menu.Categories
{
    /// <summary>
    /// Команда изменения названия категории.
    /// </summary>
    public class EditCategoryNameCommand : EditCategoryFieldCommand
    {
        public override string CommandKey => BotCommands.EDIT_CATEGORY_NAME;
        protected override string FieldNameRu => "название";

        protected override Task<bool> ApplyValueAsync(MenuCommandContext ctx, DataAccess.Entities.PositionCategory category, string value)
        {
            if (value.Length > 100)
            {
                SendMessageAsync(ctx, "Название должно быть <= 100 символов.");
                return Task.FromResult(false);
            }
            category.Name = value;
            return Task.FromResult(true);
        }
    }
}
