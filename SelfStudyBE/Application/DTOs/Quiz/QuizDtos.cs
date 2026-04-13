namespace Application.DTOs.Quiz;

public record CreateAiQuizRequest(
    int SubjectId,
    int? HeadingId = null,           // null = toàn bộ Subject
    int NumberOfQuestions = 10,
    string Difficulty = "Medium",
    string Model = "llama3.2"
);

public record QuizDto(
    int Id,
    int SubjectId,
    int? HeadingId,
    string Title,
    int TotalQuestions,
    DateTime CreatedAt
);

public record QuizQuestionDto(               // Dùng để hiển thị khi làm bài
    int QuestionId,
    string QuestionText,
    string QuestionType,        // MCQ hoặc FillBlank
    string Difficulty,
    List<AnswerOptionDto>? Options,          // Cho MCQ
    List<string>? AcceptedAnswers            // Cho FillBlank
);

public record AnswerOptionDto(
    int AnswerId,
    string AnswerText
);

public record SubmitQuizAttemptDto(
    int QuizId,
    List<QuizAnswerSubmission> Answers
);

public record QuizAnswerSubmission(
    int QuestionId,
    int? SelectedAnswerId,      // MCQ
    string? AnswerText          // FillBlank
);

public record QuizAttemptResultDto(
    int AttemptId,
    int QuizId,
    float Score,
    int TotalQuestions,
    int CorrectCount,
    DateTime StartedAt,
    DateTime CompletedAt,
    List<QuizAnswerResultDto> Answers
);

public record QuizAnswerResultDto(
    int QuestionId,
    string QuestionText,
    bool IsCorrect,
    string? UserAnswer,
    string? CorrectAnswerPreview,
    string? Explanation
);


public record QuizAttemptHistoryDto(
    int AttemptId,
    int QuizId,
    string QuizTitle,
    float Score,
    int TotalQuestions,
    int CorrectCount,
    DateTime StartedAt,
    DateTime CompletedAt
);

public record QuizAttemptDetailDto(
    int AttemptId,
    int QuizId,
    string QuizTitle,
    float Score,
    int TotalQuestions,
    int CorrectCount,
    DateTime StartedAt,
    DateTime CompletedAt,
    List<QuizAnswerDetailDto> Answers
);

public record QuizAnswerDetailDto(
    int QuestionId,
    string QuestionText,
    string QuestionType,
    bool IsCorrect,
    string? UserAnswer,
    string? CorrectAnswerPreview,
    string? Explanation
);