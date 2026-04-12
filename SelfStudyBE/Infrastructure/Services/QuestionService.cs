using System.Text.Json;
using Application.DTOs.Question;
using Application.Interfaces.Question;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class QuestionService : IQuestionService
{
    private readonly AppDbContext _context;
    private readonly OllamaService _ollamaService;

    public QuestionService(AppDbContext context, OllamaService ollamaService)
    {
        _context = context;
        _ollamaService = ollamaService;
    }

    public async Task<List<GeneratedQuestionDto>> GenerateQuestionsAsync(GenerateQuestionsRequest request, string userId)
{
    var subject = await _context.Subjects
        .FirstOrDefaultAsync(s => s.Id == request.SubjectId && s.CreatedBy == userId);

    if (subject == null)
        throw new UnauthorizedAccessException("Subject not found or access denied.");

    var contents = await GetContentsForGeneration(request);

    if (!contents.Any())
        throw new InvalidOperationException("No content found to generate questions.");

    var combinedText = string.Join("\n\n---\n\n", contents.Select(c => 
        $"Tiêu đề: {c.Heading?.Title ?? "Không có tiêu đề"}\nNội dung:\n{c.Body}"));

    var types = request.QuestionTypes != null && request.QuestionTypes.Any()
        ? string.Join(", ", request.QuestionTypes)
        : "MCQ và FillBlank (điền từ)";

    var prompt = $@"
Bạn là giáo viên chuyên tạo câu hỏi chất lượng cao bằng tiếng Việt.

Hãy tạo đúng {request.NumberOfQuestions} câu hỏi dựa trên nội dung dưới đây.

Yêu cầu:
- Độ khó: {request.Difficulty}
- Loại câu hỏi: {types} (trộn lẫn cả 2 loại nếu có thể)
- Mỗi câu hỏi phải liên quan trực tiếp đến nội dung và tiêu đề
- Trả về **chỉ** một mảng JSON, không thêm bất kỳ chữ nào khác.

Định dạng JSON cho từng câu hỏi:

1. Loại MCQ (Multiple Choice):
{{
  ""questionText"": ""Câu hỏi ở đây?"",
  ""questionType"": ""MCQ"",
  ""explanation"": ""Giải thích chi tiết tại sao đáp án đúng"",
  ""difficulty"": ""{request.Difficulty}"",
  ""options"": [
    {{ ""answerText"": ""A. Lựa chọn 1"", ""isCorrect"": false }},
    {{ ""answerText"": ""B. Lựa chọn 2"", ""isCorrect"": true }},
    {{ ""answerText"": ""C. Lựa chọn 3"", ""isCorrect"": false }},
    {{ ""answerText"": ""D. Lựa chọn 4"", ""isCorrect"": false }}
  ],
  ""acceptedAnswers"": null
}}

2. Loại FillBlank (Điền từ):
{{
  ""questionText"": ""Câu hỏi có chỗ trống ví dụ: Thủ đô của Việt Nam là _____."",
  ""questionType"": ""FillBlank"",
  ""explanation"": ""Giải thích..."",
  ""difficulty"": ""{request.Difficulty}"",
  ""options"": null,
  ""acceptedAnswers"": [""Hà Nội"", ""Ha Noi"", ""Hanoi""]
}}

Nội dung tham khảo:
{combinedText}

Bây giờ hãy tạo câu hỏi:";
    
    var rawJson = await _ollamaService.GenerateAsync(request.Model, prompt, temperature: 0.65f);

    try
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        // var questions = JsonSerializer.Deserialize<List<GeneratedQuestionDto>>(rawJson, options);

        var data = JsonSerializer.Deserialize<QuestionResponse>(
            rawJson,
            options
        );

        var generatedQuestions = data?.Questions ?? new List<GeneratedQuestionDto>();
        
        return generatedQuestions ?? new List<GeneratedQuestionDto>();
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Không thể parse JSON từ Ollama: {ex.Message}. Raw: {rawJson}");
    }
}
    private async Task<List<Content>> GetContentsForGeneration(GenerateQuestionsRequest request)
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

    public async Task<List<int>> SaveGeneratedQuestionsAsync(CreateQuestionsFromAIResponse response, string userId)
    {
        var savedIds = new List<int>();

        foreach (var q in response.Questions)
        {
            var question = new Question
            {
                SubjectId = response.SubjectId,
                HeadingId = response.HeadingId,
                ContentId = response.ContentId,
                QuestionText = q.QuestionText.Trim(),
                QuestionType = q.QuestionType,
                Explanation = q.Explanation?.Trim(),
                Difficulty = q.Difficulty ?? "Medium",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            // === Xử lý theo loại câu hỏi ===
            if (q.QuestionType == "MCQ" && q.Options != null)
            {
                foreach (var opt in q.Options)
                {
                    var answer = new Answer
                    {
                        QuestionId = question.Id,
                        AnswerText = opt.AnswerText.Trim(),
                        IsCorrect = opt.IsCorrect
                    };
                    _context.Answers.Add(answer);
                }
            }
            else if (q.QuestionType == "FillBlank" && q.AcceptedAnswers != null)
            {
                foreach (var ans in q.AcceptedAnswers)
                {
                    if (string.IsNullOrWhiteSpace(ans)) continue;

                    var accepted = new AcceptedAnswer
                    {
                        QuestionId = question.Id,
                        AnswerText = ans.Trim()
                    };
                    _context.AcceptedAnswers.Add(accepted);
                }
            }

            await _context.SaveChangesAsync();
            savedIds.Add(question.Id);
        }

        return savedIds;
    }
    
    public class QuestionResponse
    {
        public List<GeneratedQuestionDto> Questions { get; set; }
    }
}