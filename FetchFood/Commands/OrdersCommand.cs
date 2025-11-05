namespace FetchFood.Commands
{
    // команды сервиса заказов
    public class OrdersCommand
    {
        public string Command;
        private OrdersCommand(string command)
        {
            Command = command;
        }

        public static OrdersCommand GetOrder { get { return new OrdersCommand("GetOrder"); } }
        public static OrdersCommand ToOrderMenu { get { return new OrdersCommand("ToOrderMenu"); } }
        public static OrdersCommand DeleteOrder { get { return new OrdersCommand("DeleteOrder"); } }

        public override string ToString()
        {
            return Command;
        }
    }
}
