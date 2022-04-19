using Google.Apis.Auth;
using Google.Authenticator;
using LoginApi.Data;
using LoginApi.Dto;
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
        private string appName = "LoginDemo";
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

            if (user is null)
            {
                return Unauthorized("Invalid credentials!");
            }

            if (HashService.HashPassword(dto.Password) != user.Password)
            {
                return Unauthorized("Invalid credentials!");
            }

            if (user.TfaSecret is not null)
            {
                return Ok(new
                {
                    id = user.Id
                });
            }

            Random random = new();
            string secret = new(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567", 32).Select(s => s[random.Next(s.Length)]).ToArray());

            string otpAuthUrl = $"otpauth://totp/{appName}:Secret?secret={secret}&issuer={appName}".Replace(" ", "%20");

            return Ok(new
            {
                id = user.Id,
                secret = secret,
                otpauth_url = otpAuthUrl
            });
        }

        [HttpPost("two-factor")]
        public IActionResult TwoFactor(TwoFactorDto dto)
        {
            User? user = db.Users.Where(u => u.Id == dto.Id).FirstOrDefault();

            if (user is null)
            {
                return Unauthorized("Invalid credentials!");
            }

            string secret = user.TfaSecret is null ? dto.Secret : user.TfaSecret;

            TwoFactorAuthenticator tfa = new();
            if (!tfa.ValidateTwoFactorPIN(secret, dto.Code))
            {
                return Unauthorized("Invalid credentials!");
            }

            if (user.TfaSecret is null)
            {
                db.Users.Where(u => u.Email == user.Email).FirstOrDefault()!.TfaSecret = dto.Secret;
                db.SaveChanges();
            }

            string accessToken = TokenService.CreateAccessToken(dto.Id, configuration.GetSection("JWT:AccessKey").Value);
            string refreshToken = TokenService.CreateRefreshToken(dto.Id, configuration.GetSection("JWT:RefreshKey").Value);

            CookieOptions cookieOptions = new();
            cookieOptions.HttpOnly = true;
            Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);

            UserToken token = new()
            {
                UserId = dto.Id,
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

        [HttpPost("google-auth")]
        public async Task<IActionResult> GoogleAuth(GoogleAuthDto dto)
        {
            var googleUser = await GoogleJsonWebSignature.ValidateAsync(dto.Token);

            if (googleUser is null)
            {
                return Unauthorized("Unauthenticated!");
            }

            User? user = db.Users.Where(u => u.Email == googleUser.Email).FirstOrDefault();

            if (user is null)
            {
                user = new()
                {
                    FirstName = googleUser.GivenName,
                    LastName = googleUser.FamilyName,
                    Email = googleUser.Email,
                    Password = dto.Token
                };

                db.Users.Add(user);
                db.SaveChanges();

                user.Id = db.Users.Where(u => u.Email == user.Email).FirstOrDefault()!.Id;
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
