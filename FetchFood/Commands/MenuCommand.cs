namespace FetchFood.Commands
{
    public class MenuCommand : CommandsBase
    {
        public const string MENU = "menu";
        private MenuCommand(string command) : base(command) 
        { }

        public static MenuCommand AddPosition { get { return new MenuCommand($"{MENU}{Separator}{BotCommands.ADD_POSITION}"); } }
        public static MenuCommand DeletePosition { get { return new MenuCommand($"{MENU}{Separator}{BotCommands.DELETE_POSITION}"); } }
        public static MenuCommand GetPosition { get { return new MenuCommand($"{MENU}{Separator}{BotCommands.POSITION}"); } }
        public static MenuCommand GetPage { get { return new MenuCommand($"{MENU}{Separator}{BotCommands.PAGE}"); } }
        public static MenuCommand GoBack { get { return new MenuCommand($"{MENU}{Separator}{BotCommands.BACK}"); } }
        public static MenuCommand FindPositions { get { return new MenuCommand($"{MENU}{Separator}{BotCommands.FIND}"); } }
        public static MenuCommand DoNothing { get { return new MenuCommand($"{MENU}{Separator}{BotCommands.DO_NOTHING}"); } }

        public override string ToString()
        {
            return Command;
        }
    }
}
