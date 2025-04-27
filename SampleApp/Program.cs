using Microsoft.EntityFrameworkCore;
using SampleApp.Repository;
using SampleApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Rotativa.AspNetCore;
using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Authentication.Google;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Security.Claims;
using SampleApp.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IUserReg, UserReg>();
builder.Services.AddScoped<UserApiDal>();
builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});
// Add reCAPTCHA service (read keys from appsettings.json)
builder.Services.AddReCaptcha(builder.Configuration.GetSection("ReCaptcha"));

// Configure authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";  // Redirect to login if not authenticated
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true; // Prevent JS access
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Use Secure cookie in production
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Cookie expiration time
        options.SlidingExpiration = true; // Extend session if active
    })
    .AddGoogle(options =>
    {
        options.ClientId = "";
        options.ClientSecret = "";
        options.CallbackPath = "/signin-Google"; // default

        options.Events.OnCreatingTicket = (async context =>
        {
            var email = context.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = context.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var googleId = context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Inject your DB context or user service
            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDBContext>();

            // Check if user exists
            var user = db.Registation.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                // Create new user
                user = new Registration
                {
                    Email = email,
                    Name = name,
                    GoogleId = googleId
                };

                db.Registation.Add(user);
                await db.SaveChangesAsync();
            }
        });
    });


builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<SmtpEmailService>();
builder.Services.AddScoped<FileUpload>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
var wkhtmltopdfFolder = Path.Combine(app.Environment.WebRootPath);

RotativaConfiguration.Setup(wkhtmltopdfFolder);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Index}/{id?}");


app.Run();
