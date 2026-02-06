namespace FetchFood.Commands
{
    // команды сервиса заказов
    public class AdministrationCommands : CommandsBase
    {
        public const string ADMIN = "admin";
        private AdministrationCommands(string command) : base(command) 
        { }

        public static AdministrationCommands GetOrder { get { return new AdministrationCommands("GetOrder"); } }
        public static AdministrationCommands ToOrderMenu { get { return new AdministrationCommands("ToOrderMenu"); } }
        public static AdministrationCommands DeleteOrder { get { return new AdministrationCommands("DeleteOrder"); } }

        public override string ToString()
        {
            return Command;
        }
    }
}
