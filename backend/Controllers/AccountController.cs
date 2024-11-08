﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using web_api.DTOs;
using web_api.Error;
using web_api.Extentions;
using web_api.Interfaces;
using web_api.Models;

namespace web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : BaseController
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IConfiguration configuration;

        public AccountController(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            this.unitOfWork = unitOfWork;
            this.configuration = configuration;
        }

        //api/acount/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegistrationDto registrationDto)
        {
            ApiError apiError = new ApiError();

            if (registrationDto.Username.IsEmpty() ||
                registrationDto.Password.IsEmpty() ||
                registrationDto.Email.IsEmpty() ||
                registrationDto.Mobile.IsEmpty())
            {
                apiError.ErrorMessage = "Username, password, email or mobile can not be blank";
                apiError.ErrorCode = BadRequest().StatusCode;
                return BadRequest(apiError);
            }


            if (await unitOfWork.userRepository.UserAlreadyExist(registrationDto.Username))
            {
                apiError.ErrorMessage = "User already exists, Please try something else";
                apiError.ErrorCode = BadRequest().StatusCode;
                return BadRequest(apiError);
            }

            unitOfWork.userRepository.Register(registrationDto);
            await unitOfWork.SaveAsync();
            return StatusCode(201);
        }

        //api/acount/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginReqDto loginReqDto)
        {
            var user = await unitOfWork.userRepository.Authenticate(loginReqDto.Username, loginReqDto.Password);

            ApiError apiError = new ApiError();


            if (user == null)
            {
                apiError.ErrorCode = Unauthorized().StatusCode;
                apiError.ErrorMessage = "Invalid UserId or Password";
                apiError.ErrorDetails = "This error appear when provided user id or password does not exists";
                return Unauthorized(apiError);
            }

            var loginResDto = new LoginResDto();
            loginResDto.UserName = user.UserName;
            loginResDto.Token = CreateJWT(user);

            return Ok(loginResDto);
        }

        private string CreateJWT(User user)
        {
            var secreateKey = configuration.GetSection("AppSettings:Key").Value;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secreateKey));

            var clams = new Claim[]
            {
                new Claim(ClaimTypes.Name,user.UserName),
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
            };

            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(clams),
                Expires = DateTime.UtcNow.AddMinutes(10),
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
