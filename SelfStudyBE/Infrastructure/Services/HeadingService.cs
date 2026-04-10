using Application.DTOs.Heading;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class HeadingService : IHeadingService
{
    private readonly AppDbContext _context;

    public HeadingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HeadingDto> CreateAsync(CreateHeadingDto dto, string userId)
    {
        // Kiểm tra Subject có thuộc về user không
        var subjectExists = await _context.Subjects
            .AnyAsync(s => s.Id == dto.SubjectId && s.CreatedBy == userId);

        if (!subjectExists)
            throw new UnauthorizedAccessException("Subject not found or access denied");

        // Nếu có ParentId, kiểm tra Parent cùng Subject và thuộc user
        if (dto.ParentId.HasValue)
        {
            var parentExists = await _context.Headings
                .AnyAsync(h => h.Id == dto.ParentId.Value 
                            && h.SubjectId == dto.SubjectId);

            if (!parentExists)
                throw new InvalidOperationException("Parent heading not found or invalid");
        }

        var heading = new Heading
        {
            SubjectId = dto.SubjectId,
            ParentId = dto.ParentId,
            Title = dto.Title,
            Description = dto.Description,
            Order = dto.Order,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Headings.Add(heading);
        await _context.SaveChangesAsync();

        return MapToDto(heading);
    }

    public async Task<HeadingDto> UpdateAsync(int id, UpdateHeadingDto dto, string userId)
    {
        var heading = await _context.Headings
            .Include(h => h.Subject)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (heading == null || heading.Subject.CreatedBy != userId)
            throw new UnauthorizedAccessException("Heading not found or access denied");

        heading.Title = dto.Title;
        heading.Description = dto.Description;
        heading.Order = dto.Order;
        heading.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(heading);
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var heading = await _context.Headings
            .Include(h => h.Subject)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (heading == null || heading.Subject.CreatedBy != userId)
            throw new UnauthorizedAccessException("Heading not found or access denied");

        _context.Headings.Remove(heading);
        await _context.SaveChangesAsync();
    }

    public async Task<HeadingDto> GetByIdAsync(int id, string userId)
    {
        var heading = await _context.Headings
            .Include(h => h.Subject)
            .FirstOrDefaultAsync(h => h.Id == id && h.Subject.CreatedBy == userId);

        if (heading == null)
            throw new KeyNotFoundException("Heading not found");

        return MapToDto(heading);
    }

    public async Task<List<HeadingDto>> GetBySubjectAsync(int subjectId, string userId)
    {
        var subjectExists = await _context.Subjects
            .AnyAsync(s => s.Id == subjectId && s.CreatedBy == userId);

        if (!subjectExists)
            throw new UnauthorizedAccessException("Subject not found or access denied");

        var headings = await _context.Headings
            .Where(h => h.SubjectId == subjectId)
            .OrderBy(h => h.Order)
            .ThenBy(h => h.Id)
            .ToListAsync();

        return headings.Select(MapToDto).ToList();
    }

    // ==================== HEADING TREE STRUCTURE ====================
    public async Task<List<HeadingTreeDto>> GetTreeAsync(int subjectId, string userId)
    {
        var subjectExists = await _context.Subjects
            .AnyAsync(s => s.Id == subjectId && s.CreatedBy == userId);

        if (!subjectExists)
            throw new UnauthorizedAccessException("Subject not found or access denied");

        // Lấy tất cả headings của subject
        var allHeadings = await _context.Headings
            .Where(h => h.SubjectId == subjectId)
            .OrderBy(h => h.Order)
            .ThenBy(h => h.Id)
            .ToListAsync();

        // Xây dựng cây
        return BuildTree(allHeadings);
    }

    public async Task<HeadingTreeDto> GetTreeNodeAsync(int headingId, string userId)
    {
        var heading = await _context.Headings
            .Include(h => h.Subject)
            .FirstOrDefaultAsync(h => h.Id == headingId);

        if (heading == null || heading.Subject.CreatedBy != userId)
            throw new UnauthorizedAccessException("Heading not found or access denied");

        // Lấy tất cả headings cùng subject để build subtree
        var allHeadings = await _context.Headings
            .Where(h => h.SubjectId == heading.SubjectId)
            .OrderBy(h => h.Order)
            .ThenBy(h => h.Id)
            .ToListAsync();

        var tree = BuildTree(allHeadings);
        return FindNodeInTree(tree, headingId) 
            ?? throw new KeyNotFoundException("Node not found in tree");
    }

    // Helper: Xây dựng cây từ danh sách phẳng
    private List<HeadingTreeDto> BuildTree(List<Heading> headings)
    {
        var lookup = headings.ToLookup(h => h.ParentId);
        var rootHeadings = lookup[null].ToList(); // ParentId = null

        return rootHeadings.Select(h => MapToTreeDto(h, lookup)).ToList();
    }

    private HeadingTreeDto MapToTreeDto(Heading heading, ILookup<int?, Heading> lookup)
    {
        var children = lookup[heading.Id]
            .OrderBy(h => h.Order)
            .ThenBy(h => h.Id)
            .Select(h => MapToTreeDto(h, lookup))
            .ToList();

        return new HeadingTreeDto(
            Id: heading.Id,
            SubjectId: heading.SubjectId,
            ParentId: heading.ParentId,
            Title: heading.Title,
            Description: heading.Description,
            Order: heading.Order,
            Children: children
        );
    }

    private HeadingTreeDto? FindNodeInTree(List<HeadingTreeDto> tree, int headingId)
    {
        foreach (var node in tree)
        {
            if (node.Id == headingId) return node;
            var found = FindNodeInTree(node.Children, headingId);
            if (found != null) return found;
        }
        return null;
    }

    private static HeadingDto MapToDto(Heading h) =>
        new HeadingDto(h.Id, h.SubjectId, h.ParentId, h.Title, h.Description, h.Order);
}