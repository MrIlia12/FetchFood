namespace FetchFood.Commands.Menu.Categories
{
    /// <summary>
    /// Команда изменения описания категории.
    /// </summary>
    public class EditCategoryDescCommand : EditCategoryFieldCommand
    {
        public override string CommandKey => BotCommands.EDIT_CATEGORY_DESC;
        protected override string FieldNameRu => "описание";

        protected override Task<bool> ApplyValueAsync(MenuCommandContext ctx, DataAccess.Entities.PositionCategory category, string value)
        {
            category.Description = value;
            return Task.FromResult(true);
        }
    }
}
