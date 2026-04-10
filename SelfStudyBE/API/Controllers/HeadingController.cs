using System.Security.Claims;
using Application.DTOs.Heading;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HeadingsController : Controller
{
    private readonly IHeadingService _headingService;

    public HeadingsController(IHeadingService headingService)
    {
        _headingService = headingService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ==================== CRUD ====================

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHeadingDto dto)
    {
        var result = await _headingService.CreateAsync(dto, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHeadingDto dto)
    {
        var result = await _headingService.UpdateAsync(id, dto, CurrentUserId);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _headingService.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _headingService.GetByIdAsync(id, CurrentUserId);
        return Ok(result);
    }

    [HttpGet("subject/{subjectId}")]
    public async Task<IActionResult> GetBySubject(int subjectId)
    {
        var result = await _headingService.GetBySubjectAsync(subjectId, CurrentUserId);
        return Ok(result);
    }

    // ==================== TREE STRUCTURE ====================

    
    // Lấy toàn bộ cấu trúc cây tiêu đề của một Subject
    
    [HttpGet("tree/{subjectId}")]
    public async Task<IActionResult> GetTree(int subjectId)
    {
        var result = await _headingService.GetTreeAsync(subjectId, CurrentUserId);
        return Ok(result);
    }

  
    // Lấy cấu trúc cây bắt đầu từ một Heading cụ thể (subtree)
    
    [HttpGet("tree/node/{headingId}")]
    public async Task<IActionResult> GetTreeNode(int headingId)
    {
        var result = await _headingService.GetTreeNodeAsync(headingId, CurrentUserId);
        return Ok(result);
    }
}