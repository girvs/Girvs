using Girvs.Domain.Driven.Events;

namespace Girvs.Domain.Caching.Commands
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