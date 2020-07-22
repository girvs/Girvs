using System;
using FluentValidation.Results;
using Girvs.Domain.Driven.Events;

namespace Girvs.Domain.Driven.Commands
{
    public abstract class Command : Message
    {
        public DateTime Timestamp { get; private set; }
        
        public ValidationResult ValidationResult { get; set; }

        protected Command()
        {
            Timestamp = DateTime.Now;
        }

        public abstract bool IsValid();
    }
}