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

public record GenerateFlashcardsRequest(
    int SubjectId,
    int? HeadingId = null,      // null = toàn bộ Subject
    int? ContentId = null,      // null = theo Heading hoặc Subject
    int NumberOfFlashcards = 10,
    string Model = "llama3"
);

public record GeneratedFlashcardDto(
    string FrontText,
    string BackText
);

public record CreateFlashcardsFromAIResponse(
    int SubjectId,
    int? HeadingId,
    int? ContentId,
    List<GeneratedFlashcardDto> Flashcards
);