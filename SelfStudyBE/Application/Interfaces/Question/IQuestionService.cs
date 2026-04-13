using Application.DTOs.Question;

namespace Application.Interfaces.Question;

public interface IQuestionService
{
    Task<List<GeneratedQuestionDto>> GenerateQuestionsAsync(GenerateQuestionsRequest request, string userId);

    Task<DeepReviewSessionDto> StartDeepReviewAsync(DeepReviewFlashcardRequest request, string userId);
    
    // Lưu câu hỏi đã tạo vào DB
    Task<List<int>> SaveGeneratedQuestionsAsync(CreateQuestionsFromAIResponse response, string userId);
}