using SampleApp.Models;
using SampleApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Identity;
namespace SampleApp.Repository
{
    public class UserReg : IUserReg
    {
        private readonly ApplicationDBContext _context;
        private IConfiguration _configuration;

        public UserReg(ApplicationDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public async Task Add(Registration user)
        {
            await _context.Database.ExecuteSqlRawAsync(
                 "EXEC sp_InsertRegistration @Name, @Email, @Phone, @Gender, @Password, @Role, @Status, @ProfileImgPath",
                 new SqlParameter("@Name", user.Name),
                 new SqlParameter("@Email", user.Email),
                 new SqlParameter("@Phone", user.Phone),
                 new SqlParameter("@Gender", user.Gender),
                 new SqlParameter("@Password", user.Password),
                  new SqlParameter("@Role", user.Role),
                   new SqlParameter("@Status", user.Status),
                    new SqlParameter("@ProfileImgPath", user.ProfileImgPath)
             );
        }

        public async Task<IEnumerable<Registration>> GetAll()
        {
            return await _context.Registation
                .FromSqlRaw("EXEC sp_GetAllRegistrations")
                .ToListAsync();
        }

        public async Task<Registration> GetById(int id)
        {
             var regis = await _context.Registation
                .FromSqlInterpolated($"EXEC sp_GetRegistrationById @Id = {id}")
                .ToListAsync();

            return regis.FirstOrDefault();
        }

        public async Task<Registration> GetByEmailAsync(string email)
        {
            var regis = await _context.Registation
               .FromSqlInterpolated($"EXEC sp_GetRegistrationByEmail @Email = {email}")
               .ToListAsync();

            return regis.FirstOrDefault();
        }

       
        public async Task Update(Registration user)
        {
            Console.WriteLine(" Move to update");
        
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_UpdateRegistration @Id, @Status",
                new SqlParameter("@Id", user.Id),
                new SqlParameter("@Status", user.Status)           
            );


        }
        public async Task Delete(int id)
        {
           
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_DeleteRegistration @Id",
                    new SqlParameter("@Id", id)
            );
        }

        public async Task<int> TotalUsers(string searchName, string searchEmail)
        {
            var usersQuery = _context.Registation.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                usersQuery = usersQuery.Where(u => u.Name.Contains(searchName));
            }

            if (!string.IsNullOrEmpty(searchEmail))
            {
                usersQuery = usersQuery.Where(u => u.Email.Contains(searchEmail));
            }

            return await usersQuery.Where(r => r.Role == "User").CountAsync();

        }

        public async Task<IEnumerable<Registration>> ListQuery(string searchName, string searchEmail, int page, int pageSize)
        {
            var usersQuery = _context.Registation.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                usersQuery = usersQuery.Where(u => u.Name.Contains(searchName));
            }

            if (!string.IsNullOrEmpty(searchEmail))
            {
                usersQuery = usersQuery.Where(u => u.Email.Contains(searchEmail));
            }

            usersQuery = usersQuery.Where(r => r.Role == "User");

            usersQuery = usersQuery.OrderBy(u => u.Name);

            return await usersQuery
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize)
                 .ToListAsync();

        }

        public async Task UpdateProfile(Registration user)
        {
            Console.WriteLine(" Move to updateprofile");

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_UpdateProfile @Id, @Name, @Phone, @Gender, @ProfileImgPath",
                new SqlParameter("@Id", user.Id),
                new SqlParameter("@Name", user.Name),
                 new SqlParameter("@Phone", user.Phone),
                 new SqlParameter("@Gender", user.Gender),
                    new SqlParameter("@ProfileImgPath", user.ProfileImgPath)
            );


        }

        public async Task savePassword(int id, string password)
        {
            var user = await _context.Registation.FindAsync(id);
            if (user != null)
            {
                user.Password = password;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> GetFilteredUserStatusAsync()
        {
            var filterJson = JsonSerializer.Serialize(new { Role = "User" });

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var command = new SqlCommand("GetUserStatusByRoleJson", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@FilterJson", filterJson);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var result = new StringBuilder();
                    while (await reader.ReadAsync())
                    {
                        result.Append(reader.GetString(0));
                    }
                    return result.ToString(); // raw JSON string
                }
            }
        }

        public async  Task AddForgotToken(PasswordResetRequest reset)
        {
            _context.ResetTokens.Add(reset);
            await _context.SaveChangesAsync();
        }

        public async Task<PasswordResetRequest> GetTokenDetails(string token)
        {
          var details =  await _context.ResetTokens
                .FirstOrDefaultAsync(r => r.Token == token);
            return details;
        }

        public async Task<PasswordResetRequest> GetTokenData(ResetPasswordViewModel model)
        {
            var resetRequest = await _context.ResetTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r =>
                    r.Token == model.Token &&
                    r.Expiry > DateTime.UtcNow &&
                    !r.IsUsed);
            return resetRequest;
        }

        public async Task UpdateUserPasswordAsync(Registration user, string newPassword, PasswordResetRequest resetRequest)
        {
            var passwordHasher = new PasswordHasher<Registration>();
            user.Password = passwordHasher.HashPassword(user, newPassword);

            resetRequest.IsUsed = true;

            await _context.SaveChangesAsync();
        }
    }
}
