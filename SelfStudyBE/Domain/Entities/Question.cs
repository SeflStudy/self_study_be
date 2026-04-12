namespace Domain.Entities;

public class Question
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int? HeadingId { get; set; }
    public int? ContentId { get; set; }
 
    public string QuestionText { get; set; } = string.Empty;
    // MCQ | MultiChoice | FillBlank | Essay
    public string QuestionType { get; set; } = "MCQ";
    public string? Explanation { get; set; }
    public string Difficulty { get; set; } = "Medium";
 
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 
    public Subject Subject { get; set; } = null!;
    public Heading? Heading { get; set; }
    public Content? Content { get; set; }
 
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<AcceptedAnswer> AcceptedAnswers { get; set; } = new List<AcceptedAnswer>();
    public ICollection<FlashcardQuestion> FlashcardQuestions { get; set; } = new List<FlashcardQuestion>();
    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizAnswer> QuizAnswers { get; set; } = new List<QuizAnswer>();
}

public class Answer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
 
    public Question Question { get; set; } = null!;
    public ICollection<QuizAnswer> QuizAnswers { get; set; } = new List<QuizAnswer>();
}

public class AcceptedAnswer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
 
    public Question Question { get; set; } = null!;
}

public class Flashcard
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int? HeadingId { get; set; }
    public int? ContentId { get; set; }
 
    public string FrontText { get; set; } = string.Empty;
    public string BackText { get; set; } = string.Empty;
 
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
    public Subject Subject { get; set; } = null!;
    public Heading? Heading { get; set; }
    public Content? Content { get; set; }
 
    public ICollection<FlashcardProgress> Progresses { get; set; } = new List<FlashcardProgress>();
    public ICollection<FlashcardQuestion> FlashcardQuestions { get; set; } = new List<FlashcardQuestion>();
}
public class FlashcardProgress
{
    public int Id { get; set; }
    public int FlashcardId { get; set; }
    public string UserId { get; set; } = string.Empty;
 
    public bool IsLearned { get; set; }
    public int ReviewCount { get; set; }
    public int CorrectStreak { get; set; }
 
    public DateTime? NextReviewAt { get; set; }
    public DateTime? LastReviewedAt { get; set; }
 
    public Flashcard Flashcard { get; set; } = null!;
}

public class FlashcardQuestion
{
    public int FlashcardId { get; set; }
    public int QuestionId { get; set; }
 
    public Flashcard Flashcard { get; set; } = null!;
    public Question Question { get; set; } = null!;
}