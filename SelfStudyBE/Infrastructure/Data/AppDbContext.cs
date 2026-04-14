namespace Infrastructure.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
 
    // ── Content ──────────────────────────────────────────────────────────
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Heading> Headings => Set<Heading>();
    public DbSet<Content> Contents => Set<Content>();
    public DbSet<OcrImage> OcrImages => Set<OcrImage>();
 
    // ── Question / Answer ─────────────────────────────────────────────────
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<AcceptedAnswer> AcceptedAnswers => Set<AcceptedAnswer>();
 
    // ── Flashcard ─────────────────────────────────────────────────────────
    public DbSet<Flashcard> Flashcards => Set<Flashcard>();
    public DbSet<FlashcardProgress> FlashcardProgresses => Set<FlashcardProgress>();
    public DbSet<FlashcardQuestion> FlashcardQuestions => Set<FlashcardQuestion>();
 
    // ── Quiz ──────────────────────────────────────────────────────────────
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
 
    // ── Subscription / Payment ────────────────────────────────────────────
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<Payment> Payments => Set<Payment>();
 
    // ── Auth extras ───────────────────────────────────────────────────────
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
 
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // MUST call — sets up Identity tables
 
        // ── Heading (self-referencing) ───────────────────────────────────
        builder.Entity<Heading>(e =>
        {
            e.HasOne(h => h.Parent)
             .WithMany(h => h.Children)
             .HasForeignKey(h => h.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
 
            e.HasOne(h => h.Subject)
             .WithMany(s => s.Headings)
             .HasForeignKey(h => h.SubjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });
 
        // ── Content ──────────────────────────────────────────────────────
        builder.Entity<Content>(e =>
        {
            e.HasOne(c => c.Heading)
             .WithMany(h => h.Contents)
             .HasForeignKey(c => c.HeadingId)
             .OnDelete(DeleteBehavior.Cascade);
 
            e.Property(c => c.Body).HasColumnType("nvarchar(max)");
        });
 
        // ── Question ─────────────────────────────────────────────────────
        builder.Entity<Question>(e =>
        {
            e.HasOne(q => q.Subject)
             .WithMany(s => s.Questions)
             .HasForeignKey(q => q.SubjectId)
             .OnDelete(DeleteBehavior.Restrict);
 
            e.HasOne(q => q.Heading)
             .WithMany(h => h.Questions)
             .HasForeignKey(q => q.HeadingId)
             .OnDelete(DeleteBehavior.NoAction);
 
            e.HasOne(q => q.Content)
             .WithMany(c => c.Questions)
             .HasForeignKey(q => q.ContentId)
             .OnDelete(DeleteBehavior.SetNull);
 
            e.Property(q => q.QuestionText).HasColumnType("nvarchar(max)");
            e.Property(q => q.Explanation).HasColumnType("nvarchar(max)");
        });
 
        // ── Answer ───────────────────────────────────────────────────────
        builder.Entity<Answer>(e =>
        {
            e.HasOne(a => a.Question)
             .WithMany(q => q.Answers)
             .HasForeignKey(a => a.QuestionId)
             .OnDelete(DeleteBehavior.Cascade);
        });
 
        builder.Entity<AcceptedAnswer>(e =>
        {
            e.HasOne(a => a.Question)
             .WithMany(q => q.AcceptedAnswers)
             .HasForeignKey(a => a.QuestionId)
             .OnDelete(DeleteBehavior.Cascade);
        });
 
        // ── Flashcard ────────────────────────────────────────────────────
        builder.Entity<Flashcard>(e =>
        {
            e.HasOne(f => f.Subject)
             .WithMany(s => s.Flashcards)
             .HasForeignKey(f => f.SubjectId)
             .OnDelete(DeleteBehavior.Restrict);
 
            e.HasOne(f => f.Heading)
             .WithMany(h => h.Flashcards)
             .HasForeignKey(f => f.HeadingId)
             .OnDelete(DeleteBehavior.NoAction);
 
            e.HasOne(f => f.Content)
             .WithMany(c => c.Flashcards)
             .HasForeignKey(f => f.ContentId)
             .OnDelete(DeleteBehavior.NoAction);
        });
 
        builder.Entity<FlashcardProgress>(e =>
        {
            e.HasOne(fp => fp.Flashcard)
             .WithMany(f => f.Progresses)
             .HasForeignKey(fp => fp.FlashcardId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<FlashcardProgress>()
            .HasIndex(fp => new { fp.FlashcardId, fp.UserId })
            .IsUnique();   // Mỗi user chỉ có 1 progress cho 1 flashcard
        
        // ── FlashcardQuestion (composite PK, many-to-many) ───────────────
        builder.Entity<FlashcardQuestion>(e =>
        {
            e.HasKey(fq => new { fq.FlashcardId, fq.QuestionId });
 
            e.HasOne(fq => fq.Flashcard)
             .WithMany(f => f.FlashcardQuestions)
             .HasForeignKey(fq => fq.FlashcardId)
             .OnDelete(DeleteBehavior.Cascade);
 
            e.HasOne(fq => fq.Question)
             .WithMany(q => q.FlashcardQuestions)
             .HasForeignKey(fq => fq.QuestionId)
             .OnDelete(DeleteBehavior.Restrict);
        });
 
        // ── Quiz ─────────────────────────────────────────────────────────
        builder.Entity<Quiz>(e =>
        {
            e.HasOne(q => q.Subject)
             .WithMany(s => s.Quizzes)
             .HasForeignKey(q => q.SubjectId)
             .OnDelete(DeleteBehavior.Restrict);
        });
 
        // ── QuizQuestion (composite PK, many-to-many) ────────────────────
        builder.Entity<QuizQuestion>(e =>
        {
            e.HasKey(qq => new { qq.QuizId, qq.QuestionId });
 
            e.HasOne(qq => qq.Quiz)
             .WithMany(q => q.QuizQuestions)
             .HasForeignKey(qq => qq.QuizId)
             .OnDelete(DeleteBehavior.Cascade);
 
            e.HasOne(qq => qq.Question)
             .WithMany(q => q.QuizQuestions)
             .HasForeignKey(qq => qq.QuestionId)
             .OnDelete(DeleteBehavior.Cascade);
        });
 
        // ── QuizAttempt ──────────────────────────────────────────────────
        builder.Entity<QuizAttempt>(e =>
        {
            e.HasOne(a => a.Quiz)
             .WithMany(q => q.Attempts)
             .HasForeignKey(a => a.QuizId)
             .OnDelete(DeleteBehavior.Cascade);
        });
 
        // ── QuizAnswer ───────────────────────────────────────────────────
        builder.Entity<QuizAnswer>(e =>
        {
            e.HasOne(qa => qa.Attempt)
             .WithMany(a => a.Answers)
             .HasForeignKey(qa => qa.AttemptId)
             .OnDelete(DeleteBehavior.Cascade);
 
            e.HasOne(qa => qa.Question)
             .WithMany(q => q.QuizAnswers)
             .HasForeignKey(qa => qa.QuestionId)
             .OnDelete(DeleteBehavior.Restrict);
 
            e.HasOne(qa => qa.SelectedAnswer)
             .WithMany(a => a.QuizAnswers)
             .HasForeignKey(qa => qa.SelectedAnswerId)
             .OnDelete(DeleteBehavior.SetNull);
        });
 
        // ── Subscription ─────────────────────────────────────────────────
        builder.Entity<UserSubscription>(e =>
        {
            e.HasOne(us => us.Plan)
             .WithMany(p => p.UserSubscriptions)
             .HasForeignKey(us => us.PlanId)
             .OnDelete(DeleteBehavior.Restrict);
        });
 
        builder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            e.HasOne(p => p.Plan)
             .WithMany(sp => sp.Payments)
             .HasForeignKey(p => p.PlanId)
             .OnDelete(DeleteBehavior.Restrict);
        });
 
        builder.Entity<SubscriptionPlan>(e =>
        {
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
        });
 
        builder.Entity<SubscriptionPlan>().HasData(
            new SubscriptionPlan {
                Id=1,
                Name= "VIP Monthly",
                Price= 2000,
                DurationDays= 30,
                MaxFlashcardsPerDay= 100,
                MaxQuizPerDay= 20,
                MaxAIUsagePerDay= 50
            },
            new SubscriptionPlan {
                Id=2,
                Name= "VIP Monthly",
                Price= 2000,
                DurationDays= 30,
                MaxFlashcardsPerDay= 100,
                MaxQuizPerDay= 20,
                MaxAIUsagePerDay= 50
            }
        );
        
        
        // ── RefreshToken ─────────────────────────────────────────────────
        builder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(t => t.Token).IsUnique();
        });
    }
}