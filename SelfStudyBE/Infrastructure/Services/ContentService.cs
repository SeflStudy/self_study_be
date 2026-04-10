using Application.DTOs.Content;
using Application.Interfaces.Content;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ContentService : IContentService
{
    private readonly AppDbContext _context;

    public ContentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ContentDto> CreateAsync(CreateContentDto dto, string userId)
    {
        // Kiểm tra Heading có thuộc Subject của user không
        var heading = await _context.Headings
            .Include(h => h.Subject)
            .FirstOrDefaultAsync(h => h.Id == dto.HeadingId);

        if (heading == null || heading.Subject.CreatedBy != userId)
            throw new UnauthorizedAccessException("Heading not found or access denied");

        var content = new Content
        {
            HeadingId = dto.HeadingId,
            Body = dto.Body,
            SourceType = dto.SourceType,
            SourceUrl = dto.SourceUrl,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Contents.Add(content);
        await _context.SaveChangesAsync();

        return MapToDto(content);
    }

    public async Task<ContentDto> UpdateAsync(int id, UpdateContentDto dto, string userId)
    {
        var content = await _context.Contents
            .Include(c => c.Heading)
            .ThenInclude(h => h.Subject)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (content == null || content.Heading.Subject.CreatedBy != userId)
            throw new UnauthorizedAccessException("Content not found or access denied");

        content.Body = dto.Body;
        content.SourceType = dto.SourceType;
        content.SourceUrl = dto.SourceUrl;
        content.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(content);
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var content = await _context.Contents
            .Include(c => c.Heading)
            .ThenInclude(h => h.Subject)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (content == null || content.Heading.Subject.CreatedBy != userId)
            throw new UnauthorizedAccessException("Content not found or access denied");

        _context.Contents.Remove(content);
        await _context.SaveChangesAsync();
    }

    public async Task<ContentDto> GetByIdAsync(int id, string userId)
    {
        var content = await _context.Contents
            .Include(c => c.Heading)
            .ThenInclude(h => h.Subject)
            .FirstOrDefaultAsync(c => c.Id == id && c.Heading.Subject.CreatedBy == userId);

        if (content == null)
            throw new KeyNotFoundException("Content not found");

        return MapToDto(content);
    }

    public async Task<List<ContentDto>> GetByHeadingAsync(int headingId, string userId)
    {
        var heading = await _context.Headings
            .Include(h => h.Subject)
            .FirstOrDefaultAsync(h => h.Id == headingId);

        if (heading == null || heading.Subject.CreatedBy != userId)
            throw new UnauthorizedAccessException("Heading not found or access denied");

        var contents = await _context.Contents
            .Where(c => c.HeadingId == headingId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return contents.Select(MapToDto).ToList();
    }

    private static ContentDto MapToDto(Content c) =>
        new ContentDto(
            c.Id,
            c.HeadingId,
            c.Body,
            c.SourceType,
            c.SourceUrl,
            c.CreatedAt
        );
}