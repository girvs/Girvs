using System;

namespace Power.EventBus.Organization
{
    public class DeleteOrganizationEvent : IntegrationEvent
    {
        public Guid OrganizationId { get; set; }

        public DeleteOrganizationEvent(Guid organizationId)
        {
            OrganizationId = organizationId;
        }
    }
}