namespace FetchFood.Commands
{
    public class MenuCommand
    {
        public string Command;
        private MenuCommand(string command)
        {
            Command = command;
        }

        public static MenuCommand AddPosition { get { return new MenuCommand($"{BotCommands.MENU}:{BotCommands.ADD_POSITION}"); } }
        public static MenuCommand DeletePosition { get { return new MenuCommand($"{BotCommands.MENU}:{BotCommands.DELETE_POSITION}"); } }
        public static MenuCommand GetPosition { get { return new MenuCommand($"{BotCommands.MENU}:{BotCommands.POSITION}"); } }
        public static MenuCommand GetPage { get { return new MenuCommand($"{BotCommands.MENU}:{BotCommands.PAGE}"); } }
        public static MenuCommand GoBack { get { return new MenuCommand($"{BotCommands.MENU}:{BotCommands.BACK}"); } }
        public static MenuCommand FindPositions { get { return new MenuCommand($"{BotCommands.MENU}:{BotCommands.FIND}"); } }
        public static MenuCommand DoNothing { get { return new MenuCommand($"{BotCommands.MENU}:{BotCommands.DO_NOTHING}"); } }

        public override string ToString()
        {
            return Command;
        }
    }
}
