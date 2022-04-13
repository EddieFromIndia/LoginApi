using System.Text.Json.Serialization;

namespace LoginApi.DTO
{
    public class ResetDto
    {
        public string Token { get; set; } = default!;
        public string Password { get; set; } = default!;
        [JsonPropertyName("password_confirm")]
        public string PasswordConfirm { get; set; } = default!;
    }
}
