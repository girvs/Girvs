using Test.Domain.Enumerations;

namespace Test.Domain.Commands.User
{
    public class CreateUserCommand : UserCommand
    {
        public CreateUserCommand(string userAccount, string userPassword, string userName, string contactNumber,
            DataState state)

        {
            UserAccount = userAccount;
            UserPassword = userPassword;
            UserName = userName;
            ContactNumber = contactNumber;
            State = state;
        }

        public override bool IsValid()
        {
            throw new System.NotImplementedException();
        }
    }
}