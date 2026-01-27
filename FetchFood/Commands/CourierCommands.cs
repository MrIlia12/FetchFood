namespace FetchFood.Commands
{
    /// <summary>
    /// Команды для работы курьера
    /// </summary>
    public class CourierCommands : CommandsBase
    {
        public const string COURIER = "courier";

        // Действия курьера
        public const string ORDERS = "orders";           // Список заказов
        public const string DETAILS = "details";         // Детали заказа
        public const string ARRIVED = "arrived";         // Я на месте
        public const string COMPLETE = "complete";       // Завершить доставку

        // Готовые команды для кнопок
        public static readonly CourierCommands ViewOrders = new($"{COURIER}:{ORDERS}");
        public static readonly CourierCommands OrderDetails = new($"{COURIER}:{DETAILS}");
        public static readonly CourierCommands ImHere = new($"{COURIER}:{ARRIVED}");
        public static readonly CourierCommands CompleteDelivery = new($"{COURIER}:{COMPLETE}");

        public CourierCommands(string command) : base(command)
        {
        }

        /// <summary>
        /// Создает команду с ID заказа
        /// </summary>
        public static string WithOrderId(string action, long orderId)
        {
            return $"{COURIER}:{action}:{orderId}";
        }
    }
}
