namespace LoginApi.Models
{
    public record NewPasswordRecord(string Email, string Password, string PasswordConfirm, string Token);
}