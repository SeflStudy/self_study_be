using Application.DTOs.Subject;

namespace  Application.Interfaces;

public interface ISubjectService
{
    Task<SubjectDto> CreateAsync(CreateSubjectDto dto, string userId);
    Task<SubjectDto> GetByIdAsync(int id, string userId);
    Task<List<SubjectDto>> GetAllByUserAsync(string userId);
    Task<SubjectDto> UpdateAsync(int id, UpdateSubjectDto dto, string userId);
    Task DeleteAsync(int id, string userId);
}