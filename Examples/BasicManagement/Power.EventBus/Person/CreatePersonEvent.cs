using System;

namespace Power.EventBus.Person
{
    public class CreatePersonEvent : IntegrationEvent
    {
        public string UserAccount { get; set; }
        public Guid PersonId { get; set; }
        public string UserPassword { get; set; }
        public string ContactNumber { get; }

        public CreatePersonEvent(string userAccount, Guid personId, string userPassword, string contactNumber)
        {
            UserAccount = userAccount;
            PersonId = personId;
            UserPassword = userPassword;
            ContactNumber = contactNumber;
        }
    }
}