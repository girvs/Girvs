using System;
using Girvs.Domain.Driven.Commands;
using Test.Domain.Enumerations;

namespace Test.Domain.Commands.User
{
    public abstract class UserCommand : Command
    {
        public Guid Id { get; set; }
        public string UserAccount { get; protected set; }

        public string UserPassword { get; protected set; }

        public string UserName { get; protected set; }

        public string ContactNumber { get; protected set; }

        public DataState State { get; protected set; }

        public UserType UserType { get; protected set; }
    }
}