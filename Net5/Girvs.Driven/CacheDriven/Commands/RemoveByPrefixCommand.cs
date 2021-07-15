using Girvs.Driven.Events;

namespace Girvs.Driven.CacheDriven.Commands
{
    public class RemoveByPrefixCommand:Message
    {
        public RemoveByPrefixCommand(string prefix)
        {
            Prefix = prefix;
        }

        public string Prefix { get;private set; }
    }
}