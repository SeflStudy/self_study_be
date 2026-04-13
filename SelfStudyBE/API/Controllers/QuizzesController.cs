using System.Security.Claims;
using Application.DTOs.Quiz;
using Application.Interfaces.Quiz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuizzesController : ControllerBase
{
    private readonly IQuizService _quizService;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public QuizzesController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpPost("create-ai")]
    public async Task<IActionResult> CreateAiQuiz([FromBody] CreateAiQuizRequest request)
    {
        var quiz = await _quizService.CreateAiQuizAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = quiz.Id }, quiz);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var quiz = await _quizService.GetQuizByIdAsync(id, CurrentUserId);
        return Ok(quiz);
    }

    [HttpGet("{id}/questions")]
    public async Task<IActionResult> GetQuestions(int id)
    {
        var questions = await _quizService.GetQuizQuestionsAsync(id, CurrentUserId);
        return Ok(questions);
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitQuizAttemptDto dto)
    {
        var result = await _quizService.SubmitAttemptAsync(dto, CurrentUserId);
        return Ok(result);
    }
    
    [HttpGet("{quizId}/attempts")]
    public async Task<IActionResult> GetAttempts(int quizId)
    {
        var attempts = await _quizService.GetUserQuizAttemptsAsync(quizId, CurrentUserId);
        return Ok(attempts);
    }

    [HttpGet("attempts/{attemptId}")]
    public async Task<IActionResult> GetAttemptDetail(int attemptId)
    {
        var detail = await _quizService.GetAttemptDetailAsync(attemptId, CurrentUserId);
        return Ok(detail);
    }
    
}