﻿using System;
using System.Threading;
using Newtonsoft.Json;

namespace Girvs.Cap
{
    public class IntegrationEvent
    {
        public const string IntegrationEventIdentityName = "";
        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }

        public IntegrationEvent(Guid id, DateTime createDate)
        {
            Id = id;
            CreationDate = createDate;
        }

        public Guid Id { get; private set; }

        public DateTime CreationDate { get; private set; }

        [JsonIgnore]
        public CancellationToken CancellationToken { get; set; }
    }
}