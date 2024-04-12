using System;

namespace LegacyApp
{
    public class UserService
    {
        private readonly ClientRepository _clientRepository = new();
        private readonly UserCreditService _userCreditService = new();

        public bool AddUser(string firstName, string lastName, string email, DateTime dateOfBirth, int clientId)
        {
            if (!IsValidUserInput(firstName, lastName, email, dateOfBirth))
            {
                return false;
            }

            var client = _clientRepository.GetById(clientId);
            if (client == null)
            {
                return false;
            }

            var user = CreateUser(firstName, lastName, email, dateOfBirth, client);

            if (ShouldDenyUserBasedOnCreditLimit(user))
            {
                return false;
            }

            UserDataAccess.AddUser(user);
            return true;
        }

        private bool IsValidUserInput(string firstName, string lastName, string email, DateTime dateOfBirth)
        {
            return !string.IsNullOrEmpty(firstName) &&
                   !string.IsNullOrEmpty(lastName) &&
                   email.Contains("@") &&
                   email.Contains(".") &&
                   CalculateAge(dateOfBirth) >= 21;
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            var now = DateTime.Now;
            int age = now.Year - dateOfBirth.Year;
            if (now.Month < dateOfBirth.Month || (now.Month == dateOfBirth.Month && now.Day < dateOfBirth.Day))
            {
                age--;
            }
            return age;
        }

        private User CreateUser(string firstName, string lastName, string email, DateTime dateOfBirth, Client client)
        {
            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                EmailAddress = email,
                DateOfBirth = dateOfBirth,
                Client = client
            };

            switch (client.Type)
            {
                case "VeryImportantClient":
                    user.HasCreditLimit = false;
                    break;
                case "ImportantClient":
                    SetCreditLimit(user, multiplier: 2);
                    break;
                default:
                    SetCreditLimit(user);
                    break;
            }

            return user;
        }

        private void SetCreditLimit(User user, int multiplier = 1)
        {
            var creditLimit = _userCreditService.GetCreditLimit(user.LastName, user.DateOfBirth);
            user.CreditLimit = creditLimit * multiplier;
            user.HasCreditLimit = true;
        }

        private bool ShouldDenyUserBasedOnCreditLimit(User user)
        {
            return user.HasCreditLimit && user.CreditLimit < 500;
        }
    }
}
