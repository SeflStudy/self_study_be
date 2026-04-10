using Application.DTOs.Content;

namespace Application.Interfaces.Content;

public interface IContentService
{
    Task<ContentDto> CreateAsync(CreateContentDto dto, string userId);
    Task<ContentDto> UpdateAsync(int id, UpdateContentDto dto, string userId);
    Task DeleteAsync(int id, string userId);

    Task<ContentDto> GetByIdAsync(int id, string userId);
    Task<List<ContentDto>> GetByHeadingAsync(int headingId, string userId);
}