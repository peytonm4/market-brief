namespace MarketBrief.Infrastructure.External.Gdelt;

public record GdeltArticleDto(
    string Url,
    string Title,
    string? Seendate,
    string? Domain,
    string? Language,
    string? Sourcecountry,
    double? Tone
);

public record GdeltVolumeDataPoint(
    string Date,
    int Value
);

public record GdeltArticleResponse(
    IEnumerable<GdeltArticleDto>? Articles
);

public record GdeltTimelineResponse(
    IEnumerable<GdeltTimelineSeries>? Timeline
);

public record GdeltTimelineSeries(
    string? Series,
    IEnumerable<GdeltVolumeDataPoint>? Data
);

public record QueryBucket(
    string Name,
    string Query,
    string DisplayName
);

public record BucketArticlesResult(
    string BucketName,
    IEnumerable<GdeltArticleDto> Articles,
    IEnumerable<GdeltVolumeDataPoint> VolumeTimeline
);
