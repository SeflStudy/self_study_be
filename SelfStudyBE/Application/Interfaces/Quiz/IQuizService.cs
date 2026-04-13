using Application.DTOs.Quiz;

namespace Application.Interfaces.Quiz;

public interface IQuizService
{
    
    Task<List<QuizAttemptHistoryDto>> GetUserQuizAttemptsAsync(int quizId, string userId);


    Task<QuizAttemptDetailDto> GetAttemptDetailAsync(int attemptId, string userId);
    
    Task<QuizDto> CreateAiQuizAsync(CreateAiQuizRequest request, string userId);

    // Lấy câu hỏi để làm bài 
    Task<List<QuizQuestionDto>> GetQuizQuestionsAsync(int quizId, string userId);

    // Nộp bài chấm điểm
    Task<QuizAttemptResultDto> SubmitAttemptAsync(SubmitQuizAttemptDto dto, string userId);

    // Lấy thông tin quiz (để retry)
    Task<QuizDto> GetQuizByIdAsync(int quizId, string userId);
}