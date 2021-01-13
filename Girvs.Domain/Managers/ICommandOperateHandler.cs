using System.Threading.Tasks;
using Girvs.Domain.Driven.Commands;

namespace Girvs.Domain.Managers
{
    public interface ICommandOperateHandler:IManager
    {
        Task Handle(Command command);
    }
}