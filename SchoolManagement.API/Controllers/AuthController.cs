using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.API.DTOs;
using SchoolManagement.API.Interfaces;
using SchoolManagement.API.Models;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SchoolManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

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
        public async Task<ActionResult<UserDto>> Register([FromBody] Auth authUser)
        {
            Console.WriteLine($"[REGISTER] Petición recibida con email: {authUser.Email}");

            UserDto registeredUser = await _authService.RegisterAsync(authUser);
            return Ok(registeredUser);

        }

        [HttpPost("Login")]
        public async Task<ActionResult<AuthDto>> Login([FromBody] AuthReqDto req)
        {
            LoginResultDto loginResult = await _authService.LoginAsync(req.Email, req.Password);

            var cookies = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(60)
            };

            Response.Cookies.Append("AuthToken", loginResult.Token, cookies);

            return Ok(new AuthDto(loginResult.Id, loginResult.Email, loginResult.Role));
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                UserDto authenticatedUser = await _authService.GetCurrentUserAsync();
                return Ok(authenticatedUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCurrentUser: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("Logout")]
        public IActionResult LogOut()
        {
            Response.Cookies.Delete("AuthToken");
            return Ok();
        }
    }
}
