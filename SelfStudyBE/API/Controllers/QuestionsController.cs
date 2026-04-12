using System.Security.Claims;
using Application.DTOs.Question;
using Application.Interfaces.Question;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public QuestionsController(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateQuestionsRequest request)
    {
        var questions = await _questionService.GenerateQuestionsAsync(request, CurrentUserId);
        return Ok(questions);
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] CreateQuestionsFromAIResponse request)
    {
        var ids = await _questionService.SaveGeneratedQuestionsAsync(request, CurrentUserId);
        return Ok(new { Message = $"{ids.Count} questions saved successfully", QuestionIds = ids });
    }
}