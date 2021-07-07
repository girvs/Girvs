using System;
using Girvs.Driven.Commands;

namespace BasicManagement.Domain.Commands.Role
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