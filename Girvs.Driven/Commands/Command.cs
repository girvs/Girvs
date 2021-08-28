using System;
using System.Linq;
using FluentValidation.Results;
using Girvs.Driven.Events;
using Girvs.Extensions;
using Girvs.Infrastructure;

namespace Girvs.Driven.Commands
{
    public abstract class Command : Message
    {
        public abstract string CommandDesc { get; set; }

        public virtual string OperateIpAddress
        {
            get
            {
                if (EngineContext.Current.HttpContext != null)
                {
                    return EngineContext.Current.HttpContext.Request.GetApiGateWayRemoteIpAddress();
                }

                var addressList = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
                var ip = addressList
                    .FirstOrDefault(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    ?.ToString();
                return ip;
            }
        }

        public DateTime Timestamp { get; private set; }

        public ValidationResult ValidationResult { get; set; }

        protected Command()
        {
            Timestamp = DateTime.Now;
        }

        public virtual bool IsValid()
        {
            return true;
        }
    }
}