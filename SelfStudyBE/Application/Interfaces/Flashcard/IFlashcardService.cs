using Application.DTOs.Flashcard;

namespace Application.Interfaces.Flashcard;

public interface IFlashcardService
{
    Task<FlashcardDto> CreateAsync(CreateFlashcardDto dto, string userId);
    Task<FlashcardDto> UpdateAsync(int id, UpdateFlashcardDto dto, string userId);
    Task DeleteAsync(int id, string userId);

    Task<FlashcardDto> GetByIdAsync(int id, string userId);
    Task<List<FlashcardDto>> GetBySubjectAsync(int subjectId, string userId);
    Task<List<FlashcardDto>> GetByHeadingAsync(int headingId, string userId);

    Task<List<GeneratedFlashcardDto>> GenerateFlashcardsAsync(
        GenerateFlashcardsRequest request, 
        string userId);

    Task<List<int>> SaveGeneratedFlashcardsAsync(
        CreateFlashcardsFromAIResponse response, 
        string userId);
    
    Task<FlashcardProgressDto> UpdateProgressAsync(int flashcardId, bool isCorrect, string userId);
    Task<List<FlashcardProgressDto>> GetUserProgressAsync(int subjectId, string userId);
}