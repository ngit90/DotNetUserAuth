using SampleApp.Models;

namespace SampleApp.Repository
{
    public interface IUserReg
    {
        Task Add(Registration user);
        Task<IEnumerable<Registration>> GetAll();
        Task<Registration> GetById(int id);
        Task<Registration> GetByEmailAsync(string email);
        Task Update(Registration user);
        Task Delete(int id);
        Task<int> TotalUsers(string searchName, string searchEmail);
        //Task<IEnumerable<Registration>> GetPaginatedUsers(int page, int pageSize);
        Task<IEnumerable<Registration>> ListQuery(string searchName, string searchEmail, int page, int pageSize);
        Task UpdateProfile(Registration user);
        Task savePassword(int id, string password);

        Task<string> GetFilteredUserStatusAsync();
        Task AddForgotToken(PasswordResetRequest reset);
        Task<PasswordResetRequest> GetTokenDetails(string token);
        Task<PasswordResetRequest> GetTokenData(ResetPasswordViewModel model);
        Task UpdateUserPasswordAsync(Registration user, string newPassword, PasswordResetRequest resetRequest);
    }
}
