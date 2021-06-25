using System;

namespace Power.EventBus.Person
{
    public class DeletePersonEvent : IntegrationEvent
    {
        public DeletePersonEvent(Guid personId)
        {
            PersonId = personId;
        }
        
        /// <summary>
        /// 人员Id
        /// </summary>
        public Guid PersonId { get; set; }
    }
}