using System.Security.Claims;
using Application.DTOs.Content;
using Application.Interfaces.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContentsController : ControllerBase
{
    private readonly IContentService _contentService;

    public ContentsController(IContentService contentService)
    {
        _contentService = contentService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ==================== CRUD ====================

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContentDto dto)
    {
        var result = await _contentService.CreateAsync(dto, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContentDto dto)
    {
        var result = await _contentService.UpdateAsync(id, dto, CurrentUserId);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _contentService.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _contentService.GetByIdAsync(id, CurrentUserId);
        return Ok(result);
    }

    [HttpGet("heading/{headingId}")]
    public async Task<IActionResult> GetByHeading(int headingId)
    {
        var result = await _contentService.GetByHeadingAsync(headingId, CurrentUserId);
        return Ok(result);
    }
}