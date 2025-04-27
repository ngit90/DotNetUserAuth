using MyApp.DataAccess;

namespace SampleApp.Repository
{
    public class UserApiDal
    {
        private IConfiguration _config;
        private string? _connectionString;

        public UserApiDal(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("InternDB1Entities");
        }

        public sp_GetUserByEmail_Result GetUserByEmail(string email)
        {
            using (var context = new InternDB1Entities(_connectionString))
            {
                var user = context.sp_GetUserByEmail(email).FirstOrDefault();
                return user;
            }
        }
        
        public List<sp_GetUsersWithGoogleId_Result> GetUsersWithGoogleId()
        {
            using (var context = new InternDB1Entities(_connectionString))
            {
                var user = context.sp_GetUsersWithGoogleId().ToList();
                return user;
            }
        }
    }
}
