using EcommerceApi.Models;
using EcommerceApi.Models.Repositories;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IUserRepository userRepository;
        private readonly IAccountRepository accountRepository;
        private readonly EmailSender emailSender;

        public AccountController(IConfiguration configuration, IUserRepository userRepository, IAccountRepository accountRepository, EmailSender emailSender)
        {
            this.configuration = configuration;
            this.userRepository = userRepository;
            this.accountRepository = accountRepository;
            this.emailSender = emailSender;
        }


        [HttpPost("Register")]
        public async Task<ActionResult<User>> Register(UserDto userDto)
        {
            // check if the email address is already user or not
            var u = await userRepository.GetUserByEmail(userDto.Email);

            if (u != null)
            {
                ModelState.AddModelError("Email", "This Email address is already used");
                return BadRequest(ModelState);
            }


            // encrypt the password
            var passwordHasher = new PasswordHasher<User>();
            var encryptedPassword = passwordHasher.HashPassword(new User(), userDto.Password);


            // create new account
            User user = new User()
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                Phone = userDto.Phone ?? "",
                Address = userDto.Address,
                Password = encryptedPassword,
                Role = "client",
                CreatedAt = DateTime.Now
            };

            var createdUser = await accountRepository.Register(user);

            var jwt = CreateJWToken(createdUser);

            UserProfileDto userProfileDto = new UserProfileDto()
            {
                Id = createdUser.Id,
                FirstName = createdUser.FirstName,
                LastName = createdUser.LastName,
                Email = createdUser.Email,
                Phone = createdUser.Phone,
                Address = createdUser.Address,
                Role = createdUser.Role,
                CreatedAt= createdUser.CreatedAt
            };

            var response = new
            {
                Token = jwt,
                User = userProfileDto
            };


            return Ok(response);
        }


        [HttpPost("Login")]
        public async Task<ActionResult> Login(string email, string password)
        {
            var user = await userRepository.GetUserByEmail(email);

            if (user == null)
            {
                ModelState.AddModelError("Error", "Email or Password not valid");
                return BadRequest(ModelState);
            }


            // verify the password
            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(new User(), user.Password, password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Password", "Wrong Password");
                return BadRequest(ModelState);
            }


            var jwt = CreateJWToken(user);

            UserProfileDto userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            var response = new
            {
                Token = jwt,
                User = userProfileDto
            };


            return Ok(response);
        }


        [HttpPost("ForgotPassword")]
        public async Task<ActionResult> ForgotPassword(string email)
        {
            var user = await userRepository.GetUserByEmail(email);
            if (user == null)
            {
                return NotFound();
            }

            // delete any old password reset request
            var oldPwdReset = await accountRepository.FindPasswordResetByEmail(email);
            if (oldPwdReset != null)
            {
                // delete old password reset request
                await accountRepository.RemovePasswordReset(oldPwdReset);
            }

            // create Password Reset Token
            string token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();

            var pwdReset = new PasswordReset()
            {
                Email = email,
                Token = token,
                CreatedAt = DateTime.Now
            };

            await accountRepository.AddPasswordReset(pwdReset);


            // send the Password Reset Token by email to the user
            string emailSubject = "Password Reset";
            string username = user.FirstName + " " + user.LastName;
            string emailMessage = "Dear " + username + "\n" +
                "We received your password reset request.\n" +
                "Please copy the following token and paste it in the Password Reset Form:\n" +
                token + "\n\n" +
                "Best Regards\n";


            emailSender.SendEmail(emailSubject, email, username, emailMessage).Wait();

            return Ok();
        }


        [HttpPost("ResetPassword")]
        public async Task<ActionResult> ResetPassword(string token, string password)
        {
            var pwdReset = await accountRepository.FindPasswordResetByToken(token);
            if (pwdReset == null)
            {
                ModelState.AddModelError("Token", "Wrong or Expired Token");
                return BadRequest(ModelState);
            }

            var user = await userRepository.GetUserByEmail(pwdReset.Email);
            if (user == null)
            {
                ModelState.AddModelError("Token", "Wrong or Expired Token");
                return BadRequest(ModelState);
            }

            // encrypt password
            var passwordHasher = new PasswordHasher<User>();
            string encryptedPassword = passwordHasher.HashPassword(new User(), password);


            // save the new encrypted password
            user.Password = encryptedPassword;


            // delete the token
            await accountRepository.RemovePasswordReset(pwdReset);

            return Ok();
        }


        [Authorize]
        [HttpGet("Profile")]
        public async Task<ActionResult> GetProfile()
        {
            int id = JwtReader.GetUserId(User);


            var user = await userRepository.GetUserById(id);
            if (user == null)
            {
                return Unauthorized();
            }

            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Ok(userProfileDto);
        }


        [Authorize]
        [HttpPut("UpdateProfile")]
        public async Task<ActionResult> UpdateProfile(UserProfileUpdateDto userProfileUpdateDto)
        {
            int id = JwtReader.GetUserId(User);

            var user = await userRepository.GetUserById(id);
            if (user == null)
            {
                return Unauthorized();
            }

            // update the user profile
            user.FirstName = userProfileUpdateDto.FirstName;
            user.LastName = userProfileUpdateDto.LastName;
            user.Email = userProfileUpdateDto.Email;
            user.Phone = userProfileUpdateDto.Phone ?? "";
            user.Address = userProfileUpdateDto.Address;

            var updatedUser = await accountRepository.UpdateProfile(user);

            var userProfileDto = new UserProfileDto()
            {
                Id = updatedUser.Id,
                FirstName = updatedUser.FirstName,
                LastName = updatedUser.LastName,
                Email = updatedUser.Email,
                Phone = updatedUser.Phone,
                Address = updatedUser.Address,
                Role = updatedUser.Role,
                CreatedAt = updatedUser.CreatedAt
            };

            return Ok(userProfileDto);
        }



        [Authorize]
        [HttpPut("UpdatePassword")]
        public async Task<ActionResult> UpdatePassword([Required, MinLength(8), MaxLength(100)] string password)
        {
            int id = JwtReader.GetUserId(User);

            var user = await userRepository.GetUserById(id);
            if (user == null)
            {
                return Unauthorized();
            }


            // encrypt password
            var passwordHasher = new PasswordHasher<User>();
            string encryptedPassword = passwordHasher.HashPassword(new User(), password);


            // update the user password
            user.Password = encryptedPassword;

            await accountRepository.UpdateProfile(user);

            return Ok();
        }


        

        /*
        [Authorize]
        [HttpGet("GetTokenClaims")]
        public IActionResult GetTokenClaims()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                Dictionary<string, string> claims = new Dictionary<string, string>();

                foreach (Claim claim in identity.Claims)
                {
                    claims.Add(claim.Type, claim.Value);
                }

                return Ok(claims);
            }

            return Ok();
        }
        */

        /*
        [Authorize]
        [HttpGet("AuthorizeAuthenticatedUsers")]
        public IActionResult AuthorizeAuthenticatedUsers()
        {
            return Ok("You are Authorized");
        }

        [Authorize(Roles = "admin")]
        [HttpGet("AuthorizeAdmin")]
        public IActionResult AuthorizeAdmin()
        {
            return Ok("You are Authorized");
        }

        [Authorize(Roles = "admin, seller")]
        [HttpGet("AuthorizeAdminAndSeller")]
        public IActionResult AuthorizeAdminAndSeller()
        {
            return Ok("You are Authorized");
        }
        */



        /*
        [HttpGet("TestToken")]
        public IActionResult TestToken()
        {
            User user = new User() { Id = 2, Role = "admin" };
            string jwt = CreateJWToken(user);
            var response = new { JWToken = jwt };
            return Ok(response);
        }
        */

        private string CreateJWToken(User user)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim("id", "" + user.Id),
                new Claim("role", user.Role)
            };


            string strKey = configuration["JwtSettings:Key"]!;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(strKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(
                issuer: configuration["JwtSettings:Issuer"],
                audience: configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}
