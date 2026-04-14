using System.Security.Claims;
using Application.DTOs.Payment;
using Application.Interfaces.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("create-link")]
    public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentRequest request)
    {
        var result = await _paymentService.CreatePaymentLinkAsync(request, CurrentUserId);
        return Ok(result);
    }

    [HttpGet("current-subscription")]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        var result = await _paymentService.GetCurrentSubscriptionAsync(CurrentUserId);
        return Ok(result);
    }
}