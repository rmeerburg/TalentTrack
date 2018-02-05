using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Server.Data;
using Server.Infrastructure.Authentication;
using Server.Models;
using Server.Models.Authentication;

namespace Server.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly KbContext _context;
        private readonly IJwtFactory _jwtFactory;
        private readonly JwtIssuerOptions _jwtOptions;

        public AuthenticationController(KbContext context, UserManager<ApplicationUser> userManager, JwtIssuerOptions jwtOptions, IJwtFactory jwtFactory)
        {
            _context = context;
            _userManager = userManager;
            _jwtOptions = jwtOptions;
            _jwtFactory = jwtFactory;
        }

        [HttpPost("api/auth/login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody]CredentialsViewModel credentials)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var identity = await GetClaimsIdentity(credentials.Email, credentials.Password);
            if (identity == null)
                return BadRequest("Invalid username or password.");

            var jwt = await GenerateJwt(identity);
            return new OkObjectResult(jwt);
        }

        [HttpPost("api/auth/users")]
        [AllowAnonymous]
        public async Task<IActionResult> NewUser([FromBody] CredentialsViewModel registration)
        {
            if (registration == null)
                return BadRequest("no registration was passed");

            var newUser = new ApplicationUser()
            {
                Email = registration.Email,
                UserName = registration.Email,
            };

            var result = await _userManager.CreateAsync(newUser, registration.Password);

            if (!result.Succeeded)
                return BadRequest($"Unable to create user: {string.Join(Environment.NewLine, result.Errors)}" );

            // await _context.Customers.AddAsync(new Customer { IdentityId = userIdentity.Id, Location = model.Location });
            // await _context.SaveChangesAsync();

            return new OkObjectResult("Account created");
        }

        private async Task<string> GenerateJwt(ClaimsIdentity identity)
        {
            var response = new
            {
                id = identity.Claims.Single(c => c.Type == "id").Value,
                auth_token = await _jwtFactory.GenerateEncodedToken(identity.Name, identity),
                expires_in = (int)_jwtOptions.ValidFor.TotalSeconds
            };

            return JsonConvert.SerializeObject(response, new JsonSerializerSettings { Formatting = Formatting.Indented });
        }

        private async Task<ClaimsIdentity> GetClaimsIdentity(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                return await Task.FromResult<ClaimsIdentity>(null);

            // get the user to verifty
            var userToVerify = await _userManager.FindByNameAsync(userName);

            if (userToVerify == null) return await Task.FromResult<ClaimsIdentity>(null);

            // check the credentials
            if (await _userManager.CheckPasswordAsync(userToVerify, password))
            {
                return await Task.FromResult(_jwtFactory.GenerateClaimsIdentity(userName, userToVerify.Id));
            }

            // Credentials are invalid, or account doesn't exist
            return await Task.FromResult<ClaimsIdentity>(null);
        }
    }

    public class CredentialsViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // public class RegistrationViewModel
    // {
    //     public string Email { get; set; }
    //     public string Password { get; set; }
    // }
}