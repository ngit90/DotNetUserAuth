using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;
using System.Net.Mail;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace SampleApp.Services
{
    public class SmtpEmailService
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string recipientEmail, string subject, string message)
        {
            Console.WriteLine("Entered into mail service");

            var smtpSettings = _configuration.GetSection("SmtpSettings");
            string server = smtpSettings["Server"]; // Example: "smtp.office365.com"
            int port = int.Parse(smtpSettings["Port"]); // Example: 587
            string senderEmail = smtpSettings["SenderEmail"];
            string senderName = smtpSettings["SenderName"];
            string password = smtpSettings["Password"];

            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(senderName, senderEmail));
                email.To.Add(new MailboxAddress("", recipientEmail));
                email.Subject = subject;
                email.Body = new TextPart("html") { Text = message };

                using (var client = new SmtpClient())
                {
                    // Ensure you are using the correct SecureSocketOptions for the port
                    SecureSocketOptions securityOption = (port == 587) ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;

                    await client.ConnectAsync(server, port, securityOption);
                    await client.AuthenticateAsync(senderEmail, password);
                    await client.SendAsync(email);
                    await client.DisconnectAsync(true);
                }

                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email send failed: " + ex.Message);
            }
        }

    }

}
