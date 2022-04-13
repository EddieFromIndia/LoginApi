using System.Text.Json.Serialization;

namespace LoginApi.DTO
{
    public class ForgotDto
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = default!;
    }
}
