
namespace SampleApp.Models
{
    public class PasswordResetRequest
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime Expiry { get; set; }

        public bool IsUsed { get; set; }

        public Registration User { get; set; }
    }
}
