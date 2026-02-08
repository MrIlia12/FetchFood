using DataAccess.Entities;

namespace FetchFood.Commands
{
    // команды сервиса заказов
    public class AdministrationCommands : CommandsBase
    {
        public const string ADMIN = "admin";
        // команды сервиса администрирования
        public const string SHOWORDERS = "ShowOrders";
        public const string SHOWACTIVEORDERS = "ShowActiveOrders";
        public const string SHOWCOMPLETEDORDERS = "ShowCompletedOrders";
        public const string TOHOMECONSOLE = "Home";
        public const string TODELIVERY = "ToDelivery";
        public const string CANCEL = "Cancel";
        public const string COURIERSORDERS = "ShowCouriersOrders";
        private AdministrationCommands(string command) : base(command)
        { }

        public static AdministrationCommands ToHomeConsole { get { return new AdministrationCommands($"{ADMIN}{Separator}{TOHOMECONSOLE}"); } }
        public static AdministrationCommands ShowOrders { get { return new AdministrationCommands($"{ADMIN}{Separator}{SHOWORDERS}"); } }
        public static AdministrationCommands ToMenuConsole { get { return new AdministrationCommands($"{ADMIN}{Separator}ToMenuConsole"); } }
        public static AdministrationCommands ShowActiveOrders { get { return new AdministrationCommands($"{ADMIN}{Separator}{SHOWACTIVEORDERS}"); } }
        public static AdministrationCommands ShowCouriersOrders { get { return new AdministrationCommands($"{ADMIN}{Separator}{COURIERSORDERS}"); } }
        public static AdministrationCommands ShowCompletedOrders { get { return new AdministrationCommands($"{ADMIN}{Separator}{SHOWCOMPLETEDORDERS}"); } }
        public static AdministrationCommands GetOrder { get { return new AdministrationCommands($"{ADMIN}{Separator}{BotCommands.GETORDERS}{Separator}"); } }
        public static AdministrationCommands ToDelivery { get { return new AdministrationCommands($"{ADMIN}{Separator}{TODELIVERY}{Separator}"); } }
        public static AdministrationCommands CancelOrder { get { return new AdministrationCommands($"{ADMIN}{Separator}{CANCEL}{Separator}"); } }


        public override string ToString()
        {
            return Command;
        }
    }
}
