namespace Application.DTOs.Content;

public record ContentDto(
    int Id,
    int HeadingId,
    string Body,
    string SourceType,
    string? SourceUrl,
    DateTime CreatedAt
);

public record CreateContentDto(
    int HeadingId,
    string Body,
    string SourceType = "Text",
    string? SourceUrl = null
);

public record UpdateContentDto(
    string Body,
    string SourceType,
    string? SourceUrl
);