using SendWithBrevo;

namespace AuthenticationDemo.Services {
    public class BrevoEmailSender : IEmailSender {
        private readonly IConfiguration configuration;

        public BrevoEmailSender(IConfiguration configuration) {
            this.configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage) {
            var apiKey = configuration["Brevo:ApiKey"];
            var fromEmail = configuration["Brevo:FromEmail"];
            var fromName = configuration["Brevo:FromName"];

            var client = new BrevoClient(apiKey);

            await client.SendAsync(
                new Sender(fromName, fromEmail),
                new List<Recipient> { new Recipient(toEmail, toEmail) },
                subject,
                htmlMessage,
                true
            );
        }
    }
}