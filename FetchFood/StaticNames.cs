namespace FetchFood
{
    internal class StaticNames
    {
        //
    }
    internal static class GlobalParams
    {
        // число элементов, отображаемых на странице меню
        public const int MENU_ITEMS_CNT = 10;
    }
    internal static class BotCommands
    {
        // основные команды бота
        public const string START = "/start";
        public const string HELP = "/help";
        // команды сервиса заказов
        public const string GETORDERS = "GetOrder";
        public const string TOORDERMENU = "ToOrderMenu";
        public const string ORDERNEXTSTEP = "NextStep";
        public const string ORDERDELETE = "DeleteOrder";
        // команды сервиса меню
        public const string MENU = "menu";
        public const string PAGE = "page";
        public const string POSITION = "pos";
        public const string ADD = "add";
        public const string DELETE = "delete";
        public const string ADD_POSITION = "addpos";
        public const string DELETE_POSITION = "delpos";
        public const string BACK = "back";
        public const string FIND = "find";
        public const string DO_NOTHING = "noop";
        public const string EMPTY = "";
        //
    }
    internal static class LogMessages
    {
        public static string ERROR = "Ошибка";
    }
}
