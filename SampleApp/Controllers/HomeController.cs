using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNetCore.ReCaptcha;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SampleApp.Models;
using SampleApp.Repository;
using SampleApp.Services;

namespace SampleApp.Controllers;

//[Authorize(Roles ="User")]
[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUserReg _userreg;
    private readonly SmtpEmailService _emailService;
    private FileUpload _fileupload;

    public HomeController(ILogger<HomeController> logger, IUserReg userreg, SmtpEmailService emailService, FileUpload fileUpload)
    {
        _logger = logger;
        _userreg = userreg;
        _emailService = emailService;
        _fileupload = fileUpload;
    }

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index()
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var user = await _userreg.GetByEmailAsync(userEmail); // Fetch user details

        if (user == null)
        {
            return RedirectToAction("Login", "Account"); // Redirect if user not found
        }

        return View(user); // Pass user data to view
    }

    [HttpGet]
    public IActionResult Sendmail()
    {
        _logger.LogInformation("Email page accessed.");  // Log an informational message
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> Sendmail(string recipientEmail, string subject, string message)
    {
        Console.WriteLine("Entered into Mail controller");
        await _emailService.SendEmailAsync(recipientEmail, subject, message);
        ViewBag.Message = "Email sent successfully!";
        return View();
    }

    [Route("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    [HttpGet("EditProfile")]
    public async Task<IActionResult> EditProfile()
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var user = await _userreg.GetByEmailAsync(userEmail); // Fetch user details

        return View(user);
    }

    [HttpPost("EditProfile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(Registration reg)
    {
        Console.WriteLine("Move to post");
        ModelState.Remove("Password");
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

        if (reg.ProfileImg != null && reg.ProfileImg.Length > 0)
        {
            Console.WriteLine("move to post image verify");
            reg.ProfileImgPath = await _fileupload.UploadFileAsync(reg.ProfileImg);

        }
        Console.WriteLine("ProfilePath" + reg.ProfileImgPath);

        await _userreg.UpdateProfile(reg);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> ChangePassword(int id)
    {
        var user = await _userreg.GetById(id);
        if (user == null)
        {
            return NotFound("User not found.");
        }
        var model = new ChangePasswordModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var user = await _userreg.GetById(model.Id);
        if (user == null)
        {
            return NotFound("User not found.");
        }
        // Create an instance of the password hasher for the Registration type.
        var passwordHasher = new PasswordHasher<Registration>();
        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.Password, model.CurrentPassword);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
            return View(model);
        }

        // Hash the new password and update
        user.Password = passwordHasher.HashPassword(user, model.NewPassword);
        await _userreg.savePassword(user.Id, user.Password);

        // Sign out the user from the cookie authentication scheme.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Redirect the user to the Login page (or any other page).
        return RedirectToAction("Login", "Account");
    }
}
