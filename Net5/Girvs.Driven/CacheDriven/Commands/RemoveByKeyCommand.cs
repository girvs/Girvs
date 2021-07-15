using Girvs.Driven.Events;

namespace Girvs.Driven.CacheDriven.Commands
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