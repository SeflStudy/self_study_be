namespace Application.DTOs.Question;

public record GenerateQuestionsRequest(
    int SubjectId,
    int? HeadingId = null,
    int? ContentId = null,
    int NumberOfQuestions = 10,
    string Difficulty = "Medium",      // Easy, Medium, Hard
    string Model = "llama3",         // hoặc gemma2, qwen2.5, phi4...
    List<string> QuestionTypes = null  // ["MCQ", "FillBlank"] - nếu null thì random
);

public record GeneratedQuestionDto(
    string QuestionText,
    string QuestionType,               // "MCQ" hoặc "FillBlank"
    string? Explanation,
    string Difficulty,
    List<AnswerOptionDto>? Options,    // Dùng cho MCQ
    List<string>? AcceptedAnswers      // Dùng cho FillBlank
);

public record AnswerOptionDto(
    string AnswerText,
    bool IsCorrect
);

public record CreateQuestionsFromAIResponse(
    int SubjectId,
    int? HeadingId,
    int? ContentId,
    List<GeneratedQuestionDto> Questions
);