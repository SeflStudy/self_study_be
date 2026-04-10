using Application.DTOs.Heading;

namespace Application.Interfaces;

public interface IHeadingService
{
    Task<HeadingDto> CreateAsync(CreateHeadingDto dto, string userId);
    Task<HeadingDto> UpdateAsync(int id, UpdateHeadingDto dto, string userId);
    Task DeleteAsync(int id, string userId);

    Task<HeadingDto> GetByIdAsync(int id, string userId);
    Task<List<HeadingDto>> GetBySubjectAsync(int subjectId, string userId);

    
    Task<List<HeadingTreeDto>> GetTreeAsync(int subjectId, string userId);
    Task<HeadingTreeDto> GetTreeNodeAsync(int headingId, string userId);

}