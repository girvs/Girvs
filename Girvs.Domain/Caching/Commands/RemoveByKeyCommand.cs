using Girvs.Domain.Driven.Events;

namespace Girvs.Domain.Caching.Commands
{
    public class RemoveByKeyCommand : Message
    {
        public RemoveByKeyCommand(string key)
        {
            Key = key;
        }

        public string Key { get; private set; }
    }
}