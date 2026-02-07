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

    public static class OrderStatus
    {
        public const string Created = "Created";
        public const string InDelivery = "InDelivery";
        public const string Cancelled = "Cancelled";
        public const string Confirmed = "Confirmed";
    }

    public static class BotCommands
    {
        public const string CART = "cart";

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
        public const string ADDITEM = "В меню";
        public const string DELETEITEM = "Оформить заказ";
        public const string CLEARCART = "🗑️ Очистить корзину";
        // --- НОВЫЕ КОНСТАНТЫ: Callback_data для Inline кнопок корзины ---
        public const string CART_SHOW = "cart_show";
        public const string CART_ADD = "cart_add";
        public const string CART_REMOVE = "cart_remove";
        public const string CART_CLEAR = "cart_clear";
        // Лия (2025-12-13): добавляю новую константу - префикс "cart" для того, чтобы одной проверкой определять сообщения, относящиеся к сервису корзины.
        public const string CART_PREFIX = "cart_";
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
        // команды категорий
        public const string CATEGORY = "category";
        public const string CATEGORIES = "categories";
        public const string CATEGORY_POSITIONS = "catpos";
        // команды CRUD категорий
        public const string ADD_CATEGORY = "addcat";
        public const string EDIT_CATEGORY = "editcat";
        public const string DELETE_CATEGORY = "delcat";
        public const string EDIT_CATEGORY_NAME = "editcatname";
        public const string EDIT_CATEGORY_DESC = "editcatdesc";
        public const string CONFIRM_DELETE = "confirm_del";
        // команды редактирования позиций
        public const string EDIT = "edit";
        public const string EDIT_NAME = "editname";
        public const string EDIT_PRICE = "editprice";
        public const string EDIT_INGREDIENTS = "editing";
        public const string EDIT_DESCRIPTION = "editdesc";
        public const string EDIT_IMAGE = "editimg";
        public const string EDIT_POS_CATEGORY = "editposcat";
        // быстрое редактирование (все параметры одной командой)
        public const string QUICK_EDIT_POS = "qpos";
        public const string QUICK_EDIT_CAT = "qcat";

        // Временные уникальные кооманды
        public const string MENU1 = "Чтобы добавить позицию:\n" +
                            $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:Имя;Цена(руб.);Состав;Описание;[ImageUrl];[CategoryId]\n\n" +
                            "Пример:\n" +
                            $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:Бургер;199.9;булка, котлета, сыр;" +
                            "Сочный бургер;https://img;1\n\n" +
                            "CategoryId - опциональный параметр (ID категории). Если не указан, позиция будет без категории.";
        public const string ORDER1 = "📝 Введите адрес доставки в формате:\nул. <улица>, д. <номер дома>, кв. <номер квартиры>\n\n" +
                              "Пример: ул. Ленина, д. 15, кв. 42\n\n" +
                              "Допустимые форматы дома: 15, 15а, 15/1, 15/1а";
    }

    public static class LogMessages
    {
        public static string ERROR = "Ошибка";
    }
}
