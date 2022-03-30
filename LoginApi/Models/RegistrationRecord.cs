namespace LoginApi.Models
{
    public record RegistrationRecord(string FirstName, string LastName, string Email, string Password, string PasswordConfirm);
}