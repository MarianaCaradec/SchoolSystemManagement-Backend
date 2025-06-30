using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.API.DTOs;
using SchoolManagement.API.Interfaces;
using SchoolManagement.API.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SchoolManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService, IUserService userService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;
        private readonly IUserService _userService = userService;

        //[HttpPost("Authenticate")]
        //public async Task<ActionResult<string>> Authenticate([FromBody] AuthReq req)
        //{
        //    string token = await _authService.AuthenticateAsync(req.Email, req.Password, req.UserId);

        //    var cookies = new CookieOptions
        //    {
        //        HttpOnly = true,
        //        Secure = true,
        //        SameSite = SameSiteMode.Strict,
        //        Expires = DateTime.UtcNow.AddMinutes(60)
        //    };

        //    Response.Cookies.Append("AuthToken", token, cookies);

        //    return Ok(new { Message = "Authentication successful" });
        //}

        [HttpPost("Register")]
        public async Task<ActionResult<UserDto>> Register([FromBody] Auth authUser, [FromQuery] int? userId)
        {
            return await _authService.RegisterAsync(authUser, userId);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<AuthDto>> Login([FromBody] AuthReq req)
        {
            AuthDto token = await _authService.LoginAsync(req.Email, req.Password, req.UserId);

            var cookies = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(60)
            };

            Response.Cookies.Append("AuthToken", token.ToString(), cookies);

            UserDto user = await _userService.GetUserByEmailAsync(req.Email, req.UserId);

            return Ok(new AuthDto(user.Email, user.Role));
        }

        [HttpPost("LogOut")]
        public IActionResult LogOut()
        {
            Response.Cookies.Delete("AuthToken");
            return Ok();
        }
    }
}
