using System.Text.Json.Serialization;

namespace LoginApi.DTO
{
    public class GoogleAuthDto
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = default!;
    }
}
