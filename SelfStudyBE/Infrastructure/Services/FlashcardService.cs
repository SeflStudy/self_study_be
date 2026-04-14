using System.Text.Json;
using Application.DTOs.Flashcard;
using Application.Interfaces.Flashcard;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class FlashcardService : IFlashcardService
{
    private readonly AppDbContext _context;
    private readonly OllamaService _ollamaService;
    public FlashcardService(AppDbContext context , OllamaService ollamaService)
    {
        _context = context;
        this._ollamaService = ollamaService;
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
    
    public async Task<List<GeneratedFlashcardDto>> GenerateFlashcardsAsync(
    GenerateFlashcardsRequest request, 
    string userId)
{
    // Kiểm tra quyền sở hữu Subject
    var subject = await _context.Subjects
        .FirstOrDefaultAsync(s => s.Id == request.SubjectId && s.CreatedBy == userId);

    if (subject == null)
        throw new UnauthorizedAccessException("Subject not found or access denied.");

    // Lấy nội dung để generate
    var contents = await GetContentsForFlashcardGeneration(request);

    if (!contents.Any())
        throw new InvalidOperationException("No content found to generate flashcards.");

    var combinedText = string.Join("\n\n---\n\n", contents.Select(c =>
        $"Tiêu đề: {c.Heading?.Title ?? "Không có tiêu đề"}\nNội dung:\n{c.Body}"));

    var prompt = $@"
Bạn là chuyên gia tạo Flashcard chất lượng cao bằng tiếng Việt.

Hãy tạo đúng {request.NumberOfFlashcards} flashcards dựa trên nội dung sau.

Yêu cầu:
- FrontText: Câu hỏi hoặc khái niệm ngắn gọn, rõ ràng, dễ hiểu
- BackText: Giải thích chi tiết, công thức, ví dụ (nếu có)
- Nội dung phải chính xác và phù hợp để ôn tập
- Trả về **chỉ** một mảng JSON, không thêm bất kỳ chữ nào khác.

Định dạng JSON:
[
  {{
    ""frontText"": ""Đạo hàm của hàm f(x) = x² là gì?"",
    ""backText"": ""f'(x) = 2x\n\nGiải thích: Áp dụng quy tắc đạo hàm lũy thừa.""
  }}
]

Nội dung tham khảo:
{combinedText}

Bắt đầu tạo flashcards ngay:";

    var rawJson = await _ollamaService.GenerateAsync(request.Model, prompt, temperature: 0.7f);

    try
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var flashcards = JsonSerializer.Deserialize<FlashCardResponse>(rawJson, options);
        
        var generatedFlashCards = flashcards?.FlashCards ?? new List<GeneratedFlashcardDto>();
        
        return generatedFlashCards ?? new List<GeneratedFlashcardDto>();
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Không thể parse JSON từ Ollama: {ex.Message}");
    }
}

private async Task<List<Content>> GetContentsForFlashcardGeneration(GenerateFlashcardsRequest request)
{
    IQueryable<Content> query = _context.Contents
        .Include(c => c.Heading);

    if (request.ContentId.HasValue)
    {
        query = query.Where(c => c.Id == request.ContentId.Value);
    }
    else if (request.HeadingId.HasValue)
    {
        query = query.Where(c => c.HeadingId == request.HeadingId.Value);
    }
    else
    {
        query = query.Where(c => c.Heading.SubjectId == request.SubjectId);
    }

    return await query.ToListAsync();
}

    public async Task<List<int>> SaveGeneratedFlashcardsAsync(
        CreateFlashcardsFromAIResponse response, 
        string userId)
    {
        var savedIds = new List<int>();

        foreach (var fc in response.Flashcards)
        {
            var flashcard = new Flashcard
            {
                SubjectId = response.SubjectId,
                HeadingId = response.HeadingId,
                ContentId = response.ContentId,
                FrontText = fc.FrontText.Trim(),
                BackText = fc.BackText.Trim(),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Flashcards.Add(flashcard);
            await _context.SaveChangesAsync();

            savedIds.Add(flashcard.Id);
        }

        return savedIds;
    }
    
    
    
    public class FlashCardResponse
    {
        public List<GeneratedFlashcardDto> FlashCards { get; set; }
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