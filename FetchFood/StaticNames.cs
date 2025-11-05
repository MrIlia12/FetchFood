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
	internal static class GlobalParams
    {
        // число элементов, отображаемых на странице меню
        public const int MENU_ITEMS_CNT = 10;
    }
    public static class BotCommands
    {
		// основные команды бота
        public const string START = "/start";
        public const string HELP = "/help";
		// команды сервиса заказов
        public const string GETORDERS = "GetOrder";
        public const string TOORDERMENU = "ToOrderMenu";
        public const string ORDERNEXTSTEP = "NextStep";
        public const string ORDERDELETE = "DeleteOrder";
		// команды сервиса корзины
        public const string SHOWCART = "🛒 Показать корзину";
        public const string ADDITEM = "➕ Добавить товар";
        public const string DELETEITEM = "➖ Удалить товар";
        public const string CLEARCART = "🗑️ Очистить корзину";
        // --- НОВЫЕ КОНСТАНТЫ: Callback_data для Inline кнопок корзины ---
        public const string CART_SHOW = "cart_show";
        public const string CART_ADD = "cart_add";
        public const string CART_REMOVE = "cart_remove";
        public const string CART_CLEAR = "cart_clear";
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
    }
    public static class LogMessages
    {
        public static string ERROR = "Ошибка";
    }
}
