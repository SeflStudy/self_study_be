using Application.Interfaces.Payment;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;

namespace API.Controllers;

[ApiController]
[Route("api/webhook")]
public class PayOSWebhookController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PayOSWebhookController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("payos")]
    public async Task<IActionResult> HandleWebhook([FromBody] Webhook webhook)
    {
        await _paymentService.HandlePaymentWebhookAsync(webhook);
        return Ok(new { Code = "00", Desc = "Success" });
    }
}