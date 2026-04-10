using Application.DTOs.Flashcard;
using Application.Interfaces.Flashcard;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class FlashcardService : IFlashcardService
{
    private readonly AppDbContext _context;

    public FlashcardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FlashcardDto> CreateAsync(CreateFlashcardDto dto, string userId)
    {
        // Kiểm tra quyền sở hữu Subject
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == dto.SubjectId && s.CreatedBy == userId);

        if (subject == null)
            throw new UnauthorizedAccessException("Subject not found or access denied.");

        // Nếu có Heading thì kiểm tra thuộc Subject
        if (dto.HeadingId.HasValue)
        {
            var headingValid = await _context.Headings
                .AnyAsync(h => h.Id == dto.HeadingId.Value && h.SubjectId == dto.SubjectId);

            if (!headingValid)
                throw new InvalidOperationException("Heading does not belong to this subject.");
        }

        var flashcard = new Flashcard
        {
            SubjectId = dto.SubjectId,
            HeadingId = dto.HeadingId,
            ContentId = dto.ContentId,
            FrontText = dto.FrontText.Trim(),
            BackText = dto.BackText.Trim(),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Flashcards.Add(flashcard);
        await _context.SaveChangesAsync();

        return MapToDto(flashcard);
    }

    public async Task<FlashcardDto> UpdateAsync(int id, UpdateFlashcardDto dto, string userId)
    {
        var flashcard = await _context.Flashcards
            .Include(f => f.Subject)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (flashcard == null || flashcard.Subject.CreatedBy != userId)
            throw new UnauthorizedAccessException("Flashcard not found or access denied.");

        flashcard.FrontText = dto.FrontText.Trim();
        flashcard.BackText = dto.BackText.Trim();
        flashcard.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(flashcard);
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var flashcard = await _context.Flashcards
            .Include(f => f.Subject)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (flashcard == null || flashcard.Subject.CreatedBy != userId)
            throw new UnauthorizedAccessException("Flashcard not found or access denied.");

        _context.Flashcards.Remove(flashcard);
        await _context.SaveChangesAsync();
    }

    public async Task<FlashcardDto> GetByIdAsync(int id, string userId)
    {
        var flashcard = await _context.Flashcards
            .Include(f => f.Subject)
            .FirstOrDefaultAsync(f => f.Id == id && f.Subject.CreatedBy == userId);

        if (flashcard == null)
            throw new KeyNotFoundException("Flashcard not found.");

        return MapToDto(flashcard);
    }

    public async Task<List<FlashcardDto>> GetBySubjectAsync(int subjectId, string userId)
    {
        var subjectExists = await _context.Subjects
            .AnyAsync(s => s.Id == subjectId && s.CreatedBy == userId);

        if (!subjectExists)
            throw new UnauthorizedAccessException("Subject not found or access denied.");

        var flashcards = await _context.Flashcards
            .Where(f => f.SubjectId == subjectId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return flashcards.Select(MapToDto).ToList();
    }

    public async Task<List<FlashcardDto>> GetByHeadingAsync(int headingId, string userId)
    {
        var heading = await _context.Headings
            .Include(h => h.Subject)
            .FirstOrDefaultAsync(h => h.Id == headingId);

        if (heading == null || heading.Subject.CreatedBy != userId)
            throw new UnauthorizedAccessException("Heading not found or access denied.");

        var flashcards = await _context.Flashcards
            .Where(f => f.HeadingId == headingId)
            .ToListAsync();

        return flashcards.Select(MapToDto).ToList();
    }

    // ==================== PROGRESS TRACKING ====================
    public async Task<FlashcardProgressDto> UpdateProgressAsync(int flashcardId, bool isCorrect, string userId)
    {
        var flashcard = await _context.Flashcards
            .AnyAsync(f => f.Id == flashcardId);

        if (!flashcard)
            throw new KeyNotFoundException("Flashcard not found.");

        var progress = await _context.FlashcardProgresses
            .FirstOrDefaultAsync(p => p.FlashcardId == flashcardId && p.UserId == userId);

        if (progress == null)
        {
            progress = new FlashcardProgress
            {
                FlashcardId = flashcardId,
                UserId = userId,
                ReviewCount = 0,
                CorrectStreak = 0,
                IsLearned = false
            };
            _context.FlashcardProgresses.Add(progress);
        }

        progress.ReviewCount++;
        progress.LastReviewedAt = DateTime.UtcNow;

        if (isCorrect)
        {
            progress.CorrectStreak++;
            if (progress.CorrectStreak >= 3)
                progress.IsLearned = true;

            // Simple spaced repetition logic
            progress.NextReviewAt = DateTime.UtcNow.AddDays(Math.Min(progress.CorrectStreak * 2, 14));
        }
        else
        {
            progress.CorrectStreak = 0;
            progress.NextReviewAt = DateTime.UtcNow.AddDays(1); // Review lại sớm hơn
        }

        await _context.SaveChangesAsync();

        return MapProgressToDto(progress);
    }

    public async Task<List<FlashcardProgressDto>> GetUserProgressAsync(int subjectId, string userId)
    {
        var progresses = await _context.FlashcardProgresses
            .Include(p => p.Flashcard)
            .Where(p => p.Flashcard.SubjectId == subjectId && p.UserId == userId)
            .ToListAsync();

        return progresses.Select(MapProgressToDto).ToList();
    }

    private static FlashcardDto MapToDto(Flashcard f) =>
        new FlashcardDto(
            f.Id, f.SubjectId, f.HeadingId, f.ContentId,
            f.FrontText, f.BackText, f.CreatedAt
        );

    private static FlashcardProgressDto MapProgressToDto(FlashcardProgress p) =>
        new FlashcardProgressDto(
            p.FlashcardId, p.IsLearned, p.ReviewCount,
            p.CorrectStreak, p.NextReviewAt, p.LastReviewedAt
        );
}