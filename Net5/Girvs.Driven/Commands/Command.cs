﻿using System;
using FluentValidation.Results;
using Girvs.Driven.Events;

namespace Girvs.Driven.Commands
{
    public abstract class Command : Message
    {
        public abstract string CommandDesc { get; set; }
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