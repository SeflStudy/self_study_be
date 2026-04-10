namespace Application.DTOs.Heading;

public record HeadingDto(
    int Id,
    int SubjectId,
    int? ParentId,
    string Title,
    string? Description,
    int Order
);

public record CreateHeadingDto(
    int SubjectId,
    int? ParentId,
    string Title,
    string? Description,
    int Order = 0
);

public record UpdateHeadingDto(
    string Title,
    string? Description,
    int Order
);


public record HeadingTreeDto(
    int Id,
    int SubjectId,
    int? ParentId,
    string Title,
    string? Description,
    int Order,
    List<HeadingTreeDto> Children
);