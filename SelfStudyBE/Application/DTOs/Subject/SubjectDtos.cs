namespace Application.DTOs.Subject;

public record SubjectDto(
    int Id,
    string Name,
    string Description,
    string CreatedBy,
    DateTime CreatedAt
);

public record CreateSubjectDto(
    string Name,
    string? Description
);

public record UpdateSubjectDto(
    string Name,
    string? Description
);