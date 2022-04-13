using LoginApi.Data;
using LoginApi.DTO;
using LoginApi.Models;
using LoginApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoginApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class ForgotController : Controller
    {
        private ApplicationDbContext db;

        public ForgotController(ApplicationDbContext db)
        {
            this.db = db;
        }

        [HttpPost("forgot")]
        public IActionResult Forgot(ForgotDto dto)
        {
            ResetToken resetToken = new()
            {
                Email = dto.Email,
                Token = Guid.NewGuid().ToString()
            };

            db.ResetTokens.Add(resetToken);
            db.SaveChanges();

            MailService.SendPasswordResetMailAsync(resetToken);
            return Ok(new
            {
                message = "Reset link emailed!"
            });
        }

        [HttpPost("reset")]
        public IActionResult Reset(ResetDto dto)
        {
            if (dto.Password != dto.PasswordConfirm)
            {
                return Unauthorized("Passwords do not match!");
            }

            ResetToken? resetToken = db.ResetTokens.Where(t => t.Token == dto.Token).FirstOrDefault();
            if (resetToken is null)
            {
                return BadRequest("Invalid link!");
            }

            User? user = db.Users.Where(u => u.Email == resetToken.Email).FirstOrDefault();
            if (user is null)
            {
                return BadRequest("User not found!");
            }

            db.Users.Where(u => u.Email == user.Email).FirstOrDefault().Password = HashService.HashPassword(dto.Password);
            db.SaveChanges();

            return Ok(new
            {
                message = "Password changed successfully!"
            });
        }
    }
}
