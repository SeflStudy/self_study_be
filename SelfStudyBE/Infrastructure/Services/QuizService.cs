using Application.DTOs.Question;
using Application.DTOs.Quiz;
using Application.Interfaces.Question;
using Application.Interfaces.Quiz;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AnswerOptionDto = Application.DTOs.Quiz.AnswerOptionDto;

namespace Infrastructure.Services;

public class QuizService : IQuizService
{
    private readonly AppDbContext _context;
    private readonly IQuestionService _questionService;   // Inject để gọi AI

    public QuizService(AppDbContext context, IQuestionService questionService)
    {
        _context = context;
        _questionService = questionService;
    }

   public async Task<QuizDto> CreateAiQuizAsync(CreateAiQuizRequest request, string userId)
    {
        // Kiểm tra quyền
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == request.SubjectId && s.CreatedBy == userId);

        if (subject == null)
            throw new UnauthorizedAccessException("Subject not found or access denied.");

        if (request.HeadingId.HasValue)
        {
            var headingValid = await _context.Headings
                .AnyAsync(h => h.Id == request.HeadingId.Value && h.SubjectId == request.SubjectId);
            if (!headingValid)
                throw new InvalidOperationException("Heading does not belong to this subject.");
        }

        // 1. Gọi AI tạo câu hỏi (tái sử dụng service đã có)
        var generateRequest = new GenerateQuestionsRequest(
            SubjectId: request.SubjectId,
            HeadingId: request.HeadingId,
            NumberOfQuestions: request.NumberOfQuestions,
            Difficulty: request.Difficulty,
            Model: request.Model,
            QuestionTypes: new List<string> { "MCQ", "FillBlank" }
        );

        var generatedQuestions = await _questionService.GenerateQuestionsAsync(generateRequest, userId);

        // 2. Lưu câu hỏi vào DB
        var saveResponse = new CreateQuestionsFromAIResponse(
            SubjectId: request.SubjectId,
            HeadingId: request.HeadingId,
            ContentId: null,
            Questions: generatedQuestions
        );

        var savedQuestionIds = await _questionService.SaveGeneratedQuestionsAsync(saveResponse, userId);

        // 3. Tạo Quiz
        var quizTitle = request.HeadingId.HasValue 
            ? $"Quiz {DateTime.UtcNow:dd/MM HH:mm} - Heading {request.HeadingId}" 
            : $"Quiz {DateTime.UtcNow:dd/MM HH:mm} - Subject {request.SubjectId}";

        var quiz = new Quiz
        {
            SubjectId = request.SubjectId,
            Title = quizTitle,
            CreatedBy = userId
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        // 4. Link câu hỏi vào Quiz
        foreach (var qId in savedQuestionIds)
        {
            _context.QuizQuestions.Add(new QuizQuestion
            {
                QuizId = quiz.Id,
                QuestionId = qId
            });
        }
        await _context.SaveChangesAsync();

        return new QuizDto(quiz.Id, quiz.SubjectId, request.HeadingId, quiz.Title, savedQuestionIds.Count, DateTime.UtcNow);
    }

    public async Task<QuizDto> GetQuizByIdAsync(int quizId, string userId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Subject)
            .FirstOrDefaultAsync(q => q.Id == quizId && q.Subject.CreatedBy == userId);

        if (quiz == null)
            throw new UnauthorizedAccessException("Quiz not found or access denied.");

        var total = await _context.QuizQuestions.CountAsync(qq => qq.QuizId == quizId);

        return new QuizDto(quiz.Id, quiz.SubjectId, null, quiz.Title, total, DateTime.UtcNow);
    }
    
    public async Task<List<QuizQuestionDto>> GetQuizQuestionsAsync(int quizId, string userId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Subject)
            .FirstOrDefaultAsync(q => q.Id == quizId && q.Subject.CreatedBy == userId);

        if (quiz == null)
            throw new UnauthorizedAccessException("Quiz not found or access denied.");

        var questions = await _context.QuizQuestions
            .Where(qq => qq.QuizId == quizId)
            .Include(qq => qq.Question)
            .ThenInclude(q => q.Answers)
            .Include(qq => qq.Question)
            .ThenInclude(q => q.AcceptedAnswers)
            .Select(qq => qq.Question)
            .ToListAsync();

        return questions.Select(q => new QuizQuestionDto(
            q.Id,
            q.QuestionText,
            q.QuestionType,
            q.Difficulty,
            q.QuestionType == "MCQ" 
                ? q.Answers.Select(a => new AnswerOptionDto(a.Id, a.AnswerText)).ToList()
                : null,
            q.QuestionType == "FillBlank" 
                ? q.AcceptedAnswers.Select(a => a.AnswerText).ToList()
                : null
        )).ToList();
    }

    public async Task<QuizAttemptResultDto> SubmitAttemptAsync(SubmitQuizAttemptDto dto, string userId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.QuizQuestions)
                .ThenInclude(qq => qq.Question)
                    .ThenInclude(q => q.Answers)
            .Include(q => q.QuizQuestions)
                .ThenInclude(qq => qq.Question)
                    .ThenInclude(q => q.AcceptedAnswers)
            .FirstOrDefaultAsync(q => q.Id == dto.QuizId);

        if (quiz == null)
            throw new UnauthorizedAccessException("Quiz not found.");

        var attempt = new QuizAttempt
        {
            QuizId = dto.QuizId,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _context.QuizAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        int correctCount = 0;
        var answerResults = new List<QuizAnswerResultDto>();

        foreach (var sub in dto.Answers)
        {
            var question = quiz.QuizQuestions.FirstOrDefault(qq => qq.QuestionId == sub.QuestionId)?.Question;
            if (question == null) continue;

            bool isCorrect = false;
            string? userAnswerDisplay = null;
            string? correctPreview = null;

            if (question.QuestionType == "MCQ" && sub.SelectedAnswerId.HasValue)
            {
                var selectedAnswer = question.Answers.FirstOrDefault(a => a.Id == sub.SelectedAnswerId.Value);
                if (selectedAnswer != null)
                {
                    isCorrect = selectedAnswer.IsCorrect;
                    userAnswerDisplay = selectedAnswer.AnswerText;
                    correctPreview = question.Answers.FirstOrDefault(a => a.IsCorrect)?.AnswerText;
                }
            }
            else if (question.QuestionType == "FillBlank" && !string.IsNullOrWhiteSpace(sub.AnswerText))
            {
                var userInput = sub.AnswerText.Trim().ToLower();
                var accepted = question.AcceptedAnswers
                    .Any(aa => aa.AnswerText.Trim().ToLower() == userInput);

                isCorrect = accepted;
                userAnswerDisplay = sub.AnswerText;
                correctPreview = string.Join(" / ", question.AcceptedAnswers.Select(a => a.AnswerText));
            }

            if (isCorrect) correctCount++;

            var quizAnswer = new QuizAnswer
            {
                AttemptId = attempt.Id,
                QuestionId = sub.QuestionId,
                SelectedAnswerId = sub.SelectedAnswerId,
                AnswerText = sub.AnswerText,
                IsCorrect = isCorrect
            };

            _context.QuizAnswers.Add(quizAnswer);

            answerResults.Add(new QuizAnswerResultDto(
                sub.QuestionId,
                question.QuestionText,
                isCorrect,
                userAnswerDisplay,
                correctPreview,
                question.Explanation
            ));
        }

        await _context.SaveChangesAsync();

        float score = (float)Math.Round((correctCount * 100.0) / dto.Answers.Count, 2);

        attempt.Score = score;
        await _context.SaveChangesAsync();

        return new QuizAttemptResultDto(
            attempt.Id,
            quiz.Id,
            score,
            dto.Answers.Count,
            correctCount,
            attempt.StartedAt,
            attempt.CompletedAt ?? new DateTime(),
            answerResults
        );
    }

    

  public async Task<List<QuizAttemptHistoryDto>> GetUserQuizAttemptsAsync(int quizId, string userId)
{
    var quiz = await _context.Quizzes
        .FirstOrDefaultAsync(q => q.Id == quizId && q.Subject.CreatedBy == userId);

    if (quiz == null)
        throw new UnauthorizedAccessException("Quiz not found or access denied.");

    var attempts = await _context.QuizAttempts
        .Where(a => a.QuizId == quizId && a.UserId == userId)
        .OrderByDescending(a => a.CompletedAt)
        .Select(a => new QuizAttemptHistoryDto(
            a.Id,
            a.QuizId,
            quiz.Title,
            a.Score,
            a.Quiz.QuizQuestions.Count,   // Tổng số câu hỏi của quiz
            a.Answers.Count(ans => ans.IsCorrect),
            a.StartedAt,
            a.CompletedAt ?? a.StartedAt
        ))
        .ToListAsync();

    return attempts;
}

public async Task<QuizAttemptDetailDto> GetAttemptDetailAsync(int attemptId, string userId)
{
    var attempt = await _context.QuizAttempts
        .Include(a => a.Quiz)
            .ThenInclude(q => q.Subject)
        .Include(a => a.Answers)
            .ThenInclude(ans => ans.Question)
                .ThenInclude(q => q.Answers)           // Để lấy đáp án đúng
        .Include(a => a.Answers)
            .ThenInclude(ans => ans.Question)
                .ThenInclude(q => q.AcceptedAnswers)   // Cho FillBlank
        .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId);

    if (attempt == null)
        throw new UnauthorizedAccessException("Attempt not found or access denied.");

    var answerDetails = new List<QuizAnswerDetailDto>();

    foreach (var ans in attempt.Answers)
    {
        var question = ans.Question;
        string? correctPreview = null;

        if (question.QuestionType == "MCQ")
        {
            var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect);
            correctPreview = correctAnswer?.AnswerText;
        }
        else if (question.QuestionType == "FillBlank")
        {
            correctPreview = string.Join(" / ", question.AcceptedAnswers.Select(aa => aa.AnswerText));
        }

        answerDetails.Add(new QuizAnswerDetailDto(
            question.Id,
            question.QuestionText,
            question.QuestionType,
            ans.IsCorrect,
            ans.AnswerText ?? ans.SelectedAnswer?.AnswerText,
            correctPreview,
            question.Explanation
        ));
    }

    return new QuizAttemptDetailDto(
        attempt.Id,
        attempt.QuizId,
        attempt.Quiz.Title,
        attempt.Score,
        attempt.Answers.Count,
        attempt.Answers.Count(a => a.IsCorrect),
        attempt.StartedAt,
        attempt.CompletedAt ?? attempt.StartedAt,
        answerDetails
    );
}
}