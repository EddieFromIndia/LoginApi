using LoginApi.Data;
using LoginApi.Models;
using LoginApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoginApi.Controllers
{
    [Route("api")]
    [ApiController]
    public class ForgotController : Controller
    {
        #region Private Members
        private readonly ApplicationDbContext db;
        #endregion

        public ForgotController(ApplicationDbContext db)
        {
            this.db = db;
        }

        [HttpPost("forgot")]
        public IActionResult Forgot(string email)
        {
            ResetToken resetToken = new()
            {
                Email = email,
                Token = Guid.NewGuid().ToString()
            };

            // db.ResetTokens.RemoveRange(db.ResetTokens.Where(t => t.Email == resetToken.Email));
            db.Add(resetToken);
            db.SaveChanges();

            MailService.SendPasswordResetMailAsync(resetToken);
            return Ok("Reset link emailed!");
        }

        [HttpPost("reset")]
        public IActionResult Reset(string token, string password, string passwordConfirm)
        {
            ResetToken? resetToken = db.ResetTokens.Where(t => t.Token == token).FirstOrDefault();
            if (resetToken is null)
            {
                return BadRequest("Invalid Link!");
            }

            User? user = db.Users.Where(u => u.Email == resetToken.Email).FirstOrDefault();

            if (user is null || password != passwordConfirm)
            {
                return BadRequest("Invalid Link!");
            }

            db.Users.Where(u => u.Email == user.Email).FirstOrDefault().Password = HashService.HashPassword(password);

            db.SaveChanges();

            return Ok("Password Changed Successfully!");
        }
    }
}
