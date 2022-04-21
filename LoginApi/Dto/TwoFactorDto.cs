using System.Text.Json.Serialization;

namespace LoginApi.Dto
{
    public class TwoFactorDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = default!;
        [JsonPropertyName("secret")]
        public string Secret { get; set; } = default!;
        [JsonPropertyName("code")]
        public string Code { get; set; } = default!;
    }
}
