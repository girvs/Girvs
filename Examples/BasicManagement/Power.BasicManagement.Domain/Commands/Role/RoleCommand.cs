using System;
using Girvs.Domain.Driven.Commands;

namespace Power.BasicManagement.Domain.Commands.Role
{
    public abstract class RoleCommand : Command
    {
        public Guid Id { get; set; }
        public string Name { get; protected set; }
        public string Desc { get; protected set; }
        
        public Guid[] UserIds { get; protected set; }

        public override bool IsValid()
        {
            throw new System.NotImplementedException();
        }
    }
}