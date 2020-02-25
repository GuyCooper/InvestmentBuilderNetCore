
namespace InvestmentBuilderCore
{
    /// <summary>
    /// IAuthDataLayer interface. class manages user authentication.
    /// </summary>
    public interface IAuthDataLayer
    {
        int AddNewUser(string userName, string eMail, string salt, string passwordHash, string phoneNumber, bool twoFactorEnabled, string token);
        bool AuthenticateUser(string email, string passwordHash);
        bool PasswordChangeRequest(string email, string token);
        bool ChangePassword(string email, string token, string newPasswordHash, string newSalt);
        void RemoveUser(string email);
        string GetSalt(string email);
        bool ValidateNewUser(string token);
    }
}
