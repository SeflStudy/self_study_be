using Application.DTOs.Subject;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class SubjectService : ISubjectService
{
    private readonly AppDbContext _context;

    public SubjectService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SubjectDto> CreateAsync(CreateSubjectDto dto, string userId)
    {
        var subject = new Subject
        {
            Name = dto.Name,
            Description = dto.Description ?? "",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        return MapToDto(subject);
    }

    public async Task<SubjectDto> GetByIdAsync(int id, string userId)
    {
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == id && s.CreatedBy == userId)
            ?? throw new KeyNotFoundException("Subject not found");

        return MapToDto(subject);
    }

    public async Task<List<SubjectDto>> GetAllByUserAsync(string userId)
    {
        var subjects = await _context.Subjects
            .Where(s => s.CreatedBy == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return subjects.Select(MapToDto).ToList();
    }

    public async Task<SubjectDto> UpdateAsync(int id, UpdateSubjectDto dto, string userId)
    {
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == id && s.CreatedBy == userId)
            ?? throw new KeyNotFoundException("Subject not found");

        subject.Name = dto.Name;
        subject.Description = dto.Description ?? subject.Description;
        subject.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(subject);
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var subject = await _context.Subjects
            .Include(s => s.Headings)   // Cascade sẽ xóa con
            .FirstOrDefaultAsync(s => s.Id == id && s.CreatedBy == userId)
            ?? throw new KeyNotFoundException("Subject not found");

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();
    }

    private static SubjectDto MapToDto(Subject s) =>
        new SubjectDto(s.Id, s.Name, s.Description, s.CreatedBy, s.CreatedAt);
}