using MarketBrief.Core.Entities;

namespace MarketBrief.Api.Services;

public interface IEmailNotificationService
{
    Task SendBriefNotificationAsync(MarketBriefEntity brief, byte[]? pdfAttachment = null, CancellationToken cancellationToken = default);
    bool IsConfigured { get; }
}
