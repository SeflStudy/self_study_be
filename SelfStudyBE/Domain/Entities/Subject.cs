namespace Domain.Entities;

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
 
    public ICollection<Heading> Headings { get; set; } = new List<Heading>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}


public class Heading
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
 
    public Subject Subject { get; set; } = null!;
    public Heading? Parent { get; set; }
    public ICollection<Heading> Children { get; set; } = new List<Heading>();
    public ICollection<Content> Contents { get; set; } = new List<Content>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
}

public class Content
{
    public int Id { get; set; }
    public int HeadingId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
 
    public Heading Heading { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
}