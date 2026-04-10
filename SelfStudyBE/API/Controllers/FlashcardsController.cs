using System.Security.Claims;
using Application.DTOs.Flashcard;
using Application.Interfaces.Flashcard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FlashcardsController : ControllerBase
{
    private readonly IFlashcardService _flashcardService;

    public FlashcardsController(IFlashcardService flashcardService)
    {
        _flashcardService = flashcardService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFlashcardDto dto)
    {
        var result = await _flashcardService.CreateAsync(dto, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFlashcardDto dto)
    {
        var result = await _flashcardService.UpdateAsync(id, dto, CurrentUserId);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _flashcardService.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _flashcardService.GetByIdAsync(id, CurrentUserId);
        return Ok(result);
    }

    [HttpGet("subject/{subjectId}")]
    public async Task<IActionResult> GetBySubject(int subjectId)
    {
        var result = await _flashcardService.GetBySubjectAsync(subjectId, CurrentUserId);
        return Ok(result);
    }

    [HttpGet("heading/{headingId}")]
    public async Task<IActionResult> GetByHeading(int headingId)
    {
        var result = await _flashcardService.GetByHeadingAsync(headingId, CurrentUserId);
        return Ok(result);
    }

    // Progress
    [HttpPost("{flashcardId}/progress")]
    public async Task<IActionResult> UpdateProgress(int flashcardId, [FromBody] bool isCorrect)
    {
        var result = await _flashcardService.UpdateProgressAsync(flashcardId, isCorrect, CurrentUserId);
        return Ok(result);
    }

    [HttpGet("progress/{subjectId}")]
    public async Task<IActionResult> GetProgress(int subjectId)
    {
        var result = await _flashcardService.GetUserProgressAsync(subjectId, CurrentUserId);
        return Ok(result);
    }
}