using LoginApi.Data;
using LoginApi.Models;
using LoginApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoginApi.Controllers
{
    [Route("api")]
    [ApiController]
    public class AuthController : Controller
    {
        #region Private Members
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext db;
        #endregion

        public AuthController(IConfiguration configuration, ApplicationDbContext db)
        {
            this.configuration = configuration;
            this.db = db;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            System.Diagnostics.Debug.WriteLine("Inside Register");
            if (dto is null)
            {
                return BadRequest("Invalid request!");
            }

            if (dto.Password != dto.PasswordConfirm)
            {
                return Unauthorized("Passwords do not match!");
            }

            User user = new()
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Password = HashService.HashPassword(dto.Password)
            };

            // Save user to database
            db.Users.Add(user);
            db.SaveChanges();

            // Create a User Record without the password to send to the view
            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            if (dto is null)
            {
                return BadRequest("Invalid request!");
            }

            //Fetching the user from database having the same Email address
            User? targetUser = db.Users.Where(u => u.Email == dto.Email).FirstOrDefault();

            if (HashService.HashPassword(dto.Password) != (targetUser?.Password))
            {
                return Unauthorized("Invalid credentials!");
            }

            string accessToken = TokenService.CreateAccessToken(targetUser.Id, configuration.GetSection("Jwt:AccessToken").Value);
            string refreshToken = TokenService.CreateRefreshToken(targetUser.Id, configuration.GetSection("Jwt:RefreshToken").Value);

            CookieOptions cookieOptions = new();
            cookieOptions.HttpOnly = true;
            Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);
            UserToken token = new()
            {
                UserId = targetUser.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.Now.AddDays(7)
            };

            db.UserTokens.Add(token);
            db.SaveChanges();

            return Ok(new
            {
                token = accessToken
            });
        }

        [HttpGet("user")]
        public new IActionResult User()
        {
            string authorizationHeader = Request.Headers["Authorization"];

            if (authorizationHeader is null || authorizationHeader.Length <= 8)
            {
                return Unauthorized("Unauthenticated!");
            }

            string accessToken = authorizationHeader[7..];
            int id = TokenService.DecodeToken(accessToken, out bool hasTokenExpired);
            if (hasTokenExpired)
            {
                return Unauthorized("Unauthenticated!");
            }

            User? user = db.Users.Where(u => u.Id == id).FirstOrDefault();

            return Ok(user);
        }

        [HttpPost("refresh")]
        public IActionResult Refresh()
        {
            if (Request.Cookies["refresh_token"] is null)
            {
                return Unauthorized("Unauthenticated!");
            }

            string? refreshToken = Request.Cookies["refresh_token"];
            int id = TokenService.DecodeToken(refreshToken, out bool hasTokenExpired);

            UserToken? token = db.UserTokens.Where(t => t.Token == refreshToken && t.Id == id).FirstOrDefault();

            if (hasTokenExpired)
            {
                return Unauthorized("Unauthenticated!");
            }

            string accessToken = TokenService.CreateAccessToken(id, configuration.GetSection("Jwt:AccessToken").Value);
            return Ok(new
            {
                token = accessToken
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            string? refreshToken = Request.Cookies["refresh_token"];
            if (refreshToken is null)
            {
                return Ok("Already Logged Out!");
            }

            int id = TokenService.DecodeToken(refreshToken);
            db.UserTokens.RemoveRange(db.UserTokens.Where(t => t.Id == id));
            db.SaveChanges();

            Response.Cookies.Delete("refresh_token");

            return Ok("Logged Out Successfully!");
        }
    }
}
