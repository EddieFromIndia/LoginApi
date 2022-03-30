using LoginApi.Models;
using System.Net.Mail;

namespace LoginApi.Services
{
    public class MailService
    {
        #region Private Members
        private static readonly string smtpClient = "localhost";
        private static readonly int smtpPort = 1025;
        private static readonly string smtpUsername = "mail@gmail.com";
        private static readonly string smtpName = "Scalable Scripts";
        #endregion

        public static async void SendPasswordResetMailAsync(ResetToken token)
        {
            // Creating SMTP Client
            SmtpClient client = new(smtpClient)
            {
                Port = smtpPort,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = true,
                // Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = false
            };

            // Creating Email
            MailMessage email = new()
            {
                From = new MailAddress(smtpUsername, smtpName),
                Subject = "Reset your password",
                Body = $"Click <a href=\"http://localhost:5195/reset/{token.Token}\">here</a> to reset your password.",
                IsBodyHtml = true
            };
            email.To.Add(new MailAddress(token.Email));

            await client.SendMailAsync(email);

            email.Dispose();
        }
    }
}