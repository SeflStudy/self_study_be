namespace Application.DTOs.Flashcard;

public record FlashcardDto(
    int Id,
    int SubjectId,
    int? HeadingId,
    int? ContentId,
    string FrontText,
    string BackText,
    DateTime CreatedAt
);

public record CreateFlashcardDto(
    int SubjectId,
    int? HeadingId,
    int? ContentId,
    string FrontText,
    string BackText
);

public record UpdateFlashcardDto(
    string FrontText,
    string BackText
);

public record FlashcardProgressDto(
    int FlashcardId,
    bool IsLearned,
    int ReviewCount,
    int CorrectStreak,
    DateTime? NextReviewAt,
    DateTime? LastReviewedAt
);