using LoginApi.Data;
using LoginApi.DTO;
using LoginApi.Models;
using LoginApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoginApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class AuthController : Controller
    {
        #region Private Members
        private IConfiguration configuration;
        private ApplicationDbContext db;
        #endregion

        public AuthController(IConfiguration configuration, ApplicationDbContext db)
        {
            this.configuration = configuration;
            this.db = db;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
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

            db.Users.Add(user);
            db.SaveChanges();

            return Ok(user);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            User? user = db.Users.Where(u => u.Email == dto.Email).FirstOrDefault();

            if (user == null)
            {
                return Unauthorized("Invalid credentials!");
            }

            if (HashService.HashPassword(dto.Password) != user.Password)
            {
                return Unauthorized("Invalid credentials!");
            }

            string accessToken = TokenService.CreateAccessToken(user.Id, configuration.GetSection("JWT:AccessKey").Value);
            string refreshToken = TokenService.CreateRefreshToken(user.Id, configuration.GetSection("JWT:RefreshKey").Value);

            CookieOptions cookieOptions = new();
            cookieOptions.HttpOnly = true;
            Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);

            UserToken token = new()
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiredAt = DateTime.Now.AddDays(7)
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

            if (user is null)
            {
                return Unauthorized("Unauthenticated!");
            }

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

            if (!db.UserTokens.Where(u => u.Id == id && u.Token == refreshToken && u.ExpiredAt > DateTime.Now).Any())
            {
                return Unauthorized("Unauthenticated!");
            }

            if (hasTokenExpired)
            {
                return Unauthorized("Unauthenticated!");
            }

            string accessToken = TokenService.CreateAccessToken(id, configuration.GetSection("JWT:AccessKey").Value);

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

            db.UserTokens.Remove(db.UserTokens.Where(u => u.Token == refreshToken).First());
            db.SaveChanges();

            Response.Cookies.Delete("refresh_token");

            return Ok("Logged Out Successfully!");
        }
    }
}
