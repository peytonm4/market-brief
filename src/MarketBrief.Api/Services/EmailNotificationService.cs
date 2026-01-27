using System.Net;
using System.Net.Mail;
using MarketBrief.Core.Entities;

namespace MarketBrief.Api.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(IConfiguration configuration, ILogger<EmailNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured
    {
        get
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var fromAddress = _configuration["Email:FromAddress"];
            var recipients = _configuration.GetSection("Email:Recipients").Get<string[]>();

            return !string.IsNullOrEmpty(smtpHost)
                   && !string.IsNullOrEmpty(fromAddress)
                   && recipients?.Length > 0;
        }
    }

    public async Task SendBriefNotificationAsync(MarketBriefEntity brief, byte[]? pdfAttachment = null, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Email notifications not configured. Skipping notification for brief {BriefId}", brief.Id);
            return;
        }

        var smtpHost = _configuration["Email:SmtpHost"]!;
        var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
        var smtpUsername = _configuration["Email:SmtpUsername"];
        var smtpPassword = _configuration["Email:SmtpPassword"];
        var useSsl = _configuration.GetValue<bool>("Email:UseSsl", true);
        var fromAddress = _configuration["Email:FromAddress"]!;
        var fromName = _configuration["Email:FromName"] ?? "Market Brief";
        var recipients = _configuration.GetSection("Email:Recipients").Get<string[]>() ?? Array.Empty<string>();

        if (recipients.Length == 0)
        {
            _logger.LogWarning("No email recipients configured");
            return;
        }

        try
        {
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = useSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword))
            {
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = $"Daily Market Brief - {brief.BriefDate:MMMM dd, yyyy}",
                Body = BuildEmailBody(brief),
                IsBodyHtml = true
            };

            foreach (var recipient in recipients)
            {
                message.To.Add(recipient);
            }

            if (pdfAttachment != null && pdfAttachment.Length > 0)
            {
                var stream = new MemoryStream(pdfAttachment);
                var attachment = new Attachment(stream, $"market-brief-{brief.BriefDate:yyyy-MM-dd}.pdf", "application/pdf");
                message.Attachments.Add(attachment);
            }

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email notification sent for brief {BriefId} to {RecipientCount} recipients",
                brief.Id, recipients.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification for brief {BriefId}", brief.Id);
            // Don't throw - email failure shouldn't fail the brief generation
        }
    }

    private string BuildEmailBody(MarketBriefEntity brief)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; }}
        .header {{ background-color: #1e3a8a; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .summary {{ background-color: #f3f4f6; padding: 15px; border-radius: 8px; margin: 15px 0; }}
        .footer {{ background-color: #e5e7eb; padding: 15px; text-align: center; font-size: 12px; color: #6b7280; }}
        h1 {{ margin: 0; font-size: 24px; }}
        h2 {{ color: #1e3a8a; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>MARKET BRIEF</h1>
        <p style=""margin: 5px 0 0 0;"">{brief.BriefDate:MMMM dd, yyyy}</p>
    </div>
    <div class=""content"">
        <h2>{brief.Title}</h2>
        <div class=""summary"">
            <strong>Executive Summary</strong>
            <p>{brief.Summary}</p>
        </div>
        <p>The full market brief is attached as a PDF. You can also view it online in the Market Brief dashboard.</p>
    </div>
    <div class=""footer"">
        <p>This is an automated message from the Market Brief system.</p>
        <p>Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
    </div>
</body>
</html>";
    }
}
