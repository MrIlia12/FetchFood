namespace FetchFood.Commands
{
    public abstract class CommandsBase
    {
        public const string Separator = ":";
        public string Command;

        public CommandsBase(string command) 
        {
            Command = command;
        }
    }
}
