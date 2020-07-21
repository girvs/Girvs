using System.Threading.Tasks;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Managers;

namespace Girvs.Domain.Driven.Commands
{
    public class CommandHandler
    {
        private readonly IUnitOfWork _uow;
        private readonly IMediatorHandler _bus;

        public CommandHandler(IUnitOfWork uow, IMediatorHandler bus)
        {
            _uow = uow;
            _bus = bus;
        }


        protected void NotifyValidationErrors(Command message)
        {
            //foreach (var error in message.ValidationResult.Errors)
            //{
            //    //将错误信息提交到事件总线，派发出去
            //    _bus.RaiseEvent(new DomainNotification("", error.ErrorMessage));
            //}
        }

        public async Task<bool> Commit()
        {
            if (await _uow.Commit()) return true;

            return false;
        }
    }
}