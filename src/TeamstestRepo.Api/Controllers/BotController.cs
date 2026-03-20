using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using TeamstestRepo.Api.Models;
using TeamstestRepo.Api.Services;

namespace TeamstestRepo.Api.Controllers;

/// <summary>
/// Endpoint odbierający wszystkie aktywności od Bot Framework (w tym odpowiedzi z Adaptive Card).
/// </summary>
[ApiController]
[Route("api/messages")]
public sealed class BotController : ControllerBase
{
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly IBot _bot;

    public BotController(IBotFrameworkHttpAdapter adapter, IBot bot)
    {
        _adapter = adapter;
        _bot = bot;
    }

    [HttpPost]
    public async Task PostAsync(CancellationToken cancellationToken)
        => await _adapter.ProcessAsync(Request, Response, _bot, cancellationToken);
}
