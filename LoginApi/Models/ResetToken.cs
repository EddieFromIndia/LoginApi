using System.ComponentModel.DataAnnotations;

namespace LoginApi.Models
{
    public class ResetToken
    {
        [Key()]
        public string Email { get; set; } = default!;
        public string Token { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ExpiresAt { get; set; } = default!;
    }
}