using System.Threading.Tasks;
using Girvs.BusinessBasis;
using Girvs.Driven.Commands;

namespace Girvs.Driven.Behaviors
{
    public interface ICommandOperateHandler:IManager
    {
        Task Handle(Command command);
    }
}