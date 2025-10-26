namespace FetchFood
{
    internal class StaticNames
    {
        //
    }
    public static class ApplicationInfo
    {
        public const string VERSION = "0.0.0.0";
    }
    public static class BotCommands
    {
        public const string START = "/start";
        public const string HELP = "/help";
        public const string MENU = "/menu";
        public const string FIND = "/find";
        public const string ADDPOS = "/addpos"; 
        public const string DELPOS = "/delpos";
        public const string GETORDERS = "GetOrder";
        public const string TOORDERMENU = "ToOrderMenu";
        public const string ORDERNEXTSTEP = "NextStep";
        public const string ORDERDELETE = "DeleteOrder";
        public const string SHOWCART = "🛒 Показать корзину";
        public const string ADDITEM = "➕ Добавить товар";
        public const string DELETEITEM = "➖ Удалить товар";
        public const string CLEARCART = "🗑️ Очистить корзину";
    }
    public static class LogMessages
    {
        public static string ERROR = "Ошибка";
    }
}
