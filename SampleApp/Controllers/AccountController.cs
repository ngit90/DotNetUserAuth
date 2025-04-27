using Microsoft.AspNetCore.Mvc;
using SampleApp.Models;
using SampleApp.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using AspNetCore.ReCaptcha;
using SampleApp.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace SampleApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserReg _userreg;
        private readonly FileUpload _fileUpload;
        private SmtpEmailService _emailService;

        public AccountController(IUserReg userreg, FileUpload fileUpload, SmtpEmailService emailService)
        {
            _userreg = userreg;
            _fileUpload = fileUpload;
            _emailService = emailService;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("Signup")]
        public IActionResult Signup()
        {
            return View(); // Show the form
        }


        [HttpGet]
        public IActionResult Login()
        {
            // Check if the user is already authenticated by verifying if there is an identity with claims.
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                // If the role is "User", redirect to Home/Index.
                if (roleClaim != null && roleClaim.Equals("User", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home");
                }
                // If the role is "Admin", redirect to Admin/Index.
                else if (roleClaim != null && roleClaim.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Admin");
                }
            }
            return View(); // Show the form
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            Console.WriteLine("Access to Login action");
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Message = "Email and password are required.";
                return View("Login");
            }
            // Retrieve the user from the database by email.
            var user = await _userreg.GetByEmailAsync(email);
            if(user.Status == "Block")
            {
                ViewBag.Message = "User acoount Blocked. Contact Cust.Care";
                return View("Login");
            }
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                ViewBag.Message = "Invalid email or password.";
                return View("Login");
            }

            // Create an instance of the password hasher.
            var passwordHasher = new PasswordHasher<Registration>();

            // Verify the provided password against the stored hashed password.
            var result = passwordHasher.VerifyHashedPassword(user, user.Password, password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                ViewBag.Message = "Invalid email or password.";
                return View("Login");
            }

            // Create claims for the authenticated user.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            // Create a claims identity with the cookie authentication scheme.
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Optionally, you can configure additional authentication properties.
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true // Makes the authentication session persistent across browser sessions.
            };

            // Sign in the user by issuing the authentication cookie.
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Check the user's role and redirect accordingly.
            if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Admin");
            }
            else // Assumes any role other than Admin is treated as a regular user.
            {
                return RedirectToAction("Index", "Home");
            }
        }



        [HttpPost("Signup")]
        [ValidateAntiForgeryToken]
        [ValidateReCaptcha]
        public async Task<IActionResult> Signup(Registration reg)
        {
            Console.WriteLine("Move to post");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Error check one");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
                //ModelState.AddModelError("CaptchaError", "CAPTCHA validation failed. Please try again.");
                return View(reg); // Return form with validation errors
            }

            // Check if the email already exists in the database.
            var existingUser = await _userreg.GetByEmailAsync(reg.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already exists. Please use a different email address.");
                return View(reg);
            }
            if (reg.ProfileImg != null && reg.ProfileImg.Length > 0)
            {
                Console.WriteLine("move to post image verify");
                reg.ProfileImgPath = await _fileUpload.UploadFileAsync(reg.ProfileImg);
              
            }
            Console.WriteLine("ProfilePath" + reg.ProfileImgPath);
            // Create an instance of the password hasher for the Registration type.
            var passwordHasher = new PasswordHasher<Registration>();

            // Hash the plain-text password and update the reg.Password field.
            reg.Password = passwordHasher.HashPassword(reg, reg.Password);

            await _userreg.Add(reg);
            TempData["RegSuccess"] = "Your Registration Success. Please Login...";
            return RedirectToAction("Login", "Account");
            //return View("~/Views/Success/Index.cshtml");
            //return Content($"Name: {reg.Name}, Phone: {reg.Phone}, Email: {reg.Email}, Gender: {reg.Gender}");     

        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Sign out the user from the cookie authentication scheme.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
           

            // Redirect the user to the Login page (or any other page).
            return RedirectToAction("Login", "Account");
        }

        [HttpGet("GoogleLogin")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Home")
            };
            // This forces Google to show the account selection screen
            properties.Items["prompt"] = "select_account";

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        #region Forgotpassword

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userreg.GetByEmailAsync(email);
            if (user == null)
            {
                // To avoid exposing valid emails
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var reset = new PasswordResetRequest
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                Expiry = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };
            await _userreg.AddForgotToken(reset);
          
            var link = Url.Action("ResetPassword", "Account", new { token = reset.Token }, Request.Scheme);
            await _emailService.SendEmailAsync(email, "Reset Password", $"Click to reset: <a href='{link}'>Reset</a>");

            return RedirectToAction("ForgotPasswordConfirmation");
        }


        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("InvalidResetLink");

            var resetRequest = await _userreg.GetTokenDetails(token);

            // Check if token is invalid, expired, or already used
            if (resetRequest == null || resetRequest.Expiry < DateTime.UtcNow || resetRequest.IsUsed)
            {
                return RedirectToAction("InvalidResetLink");
            }

            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var resetRequest = await _userreg.GetTokenData(model);
     
            if (resetRequest == null)
            {
                ModelState.AddModelError("", "Invalid or expired token.");
                return View(model);
            }

            await _userreg.UpdateUserPasswordAsync(resetRequest.User, model.Password, resetRequest);

            return RedirectToAction("ResetPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult InvalidResetLink()
        {
            return View();
        }

        #endregion
    }
}
