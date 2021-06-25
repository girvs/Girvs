using System;

namespace Power.EventBus.Organization
{
    public class UpdateOrganizationNameEvent : IntegrationEvent
    {
        public Guid OrganizationId { get; set; }

        public string Name { get; set; }

        public UpdateOrganizationNameEvent(Guid organizationId, string name)
        {
            OrganizationId = organizationId;
            Name = name;
        }
    }
}