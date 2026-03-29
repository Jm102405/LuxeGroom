/*
 * EmailService.cs
 * Handles sending emails for LuxeGroom via Gmail SMTP.
 * Used by the Auth controllers to send OTP reset codes.
 */

using System.Net;
using System.Net.Mail;

namespace LuxeGroom.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        // Inject app configuration for reading SMTP settings
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Send a 6-digit OTP reset code to the user's Gmail
        public void SendResetCodeEmail(string toEmail, string username, string otp)
        {
            // Read SMTP settings from appsettings.json
            string smtpHost = _configuration["EmailSettings:SmtpHost"];
            int smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            string senderEmail = _configuration["EmailSettings:SenderEmail"];
            string senderPassword = _configuration["EmailSettings:SenderPassword"];

            // Configure the SMTP client with SSL and credentials
            using SmtpClient client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(senderEmail, senderPassword)
            };

            // Build the plain text email body with the OTP and expiry notice
            string body = $@"Hello {username}!

Your LuxeGroom password reset code is:

    {otp}

This code expires in 15 minutes.
If you did not request a password reset, please ignore this email.

— LuxeGroom Team";

            // Compose the email with sender, subject, and plain text body
            MailMessage mail = new MailMessage
            {
                From = new MailAddress(senderEmail, "LuxeGroom"),
                Subject = "LuxeGroom Password Reset Code",
                Body = body,
                IsBodyHtml = false
            };

            // Add recipient and send the email
            mail.To.Add(toEmail);
            client.Send(mail);
        }
    }
}
