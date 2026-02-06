using DataAccess.Entities;

namespace FetchFood.Commands
{
    // команды сервиса заказов
    public class AdministrationCommands : CommandsBase
    {
        public const string ADMIN = "admin";
        private AdministrationCommands(string command) : base(command)
        { }

        public static AdministrationCommands ToHomeConsole { get { return new AdministrationCommands($"{ADMIN}{Separator}Home"); } }
        public static AdministrationCommands ShowOrders { get { return new AdministrationCommands($"{ADMIN}{Separator}ShowOrders"); } }
        public static AdministrationCommands ToMenuConsole { get { return new AdministrationCommands($"{ADMIN}{Separator}ToMenuConsole"); } }
        public static AdministrationCommands ShowActiveOrders { get { return new AdministrationCommands($"{ADMIN}{Separator}ShowActiveOrders"); } }
        public static AdministrationCommands ShowCouriersOrders { get { return new AdministrationCommands($"{ADMIN}{Separator}ShowCouriersOrders"); } }
        public static AdministrationCommands ShowCompletedOrders { get { return new AdministrationCommands($"{ADMIN}{Separator}ShowCompletedOrders"); } }

        public override string ToString()
        {
            return Command;
        }
    }
}
