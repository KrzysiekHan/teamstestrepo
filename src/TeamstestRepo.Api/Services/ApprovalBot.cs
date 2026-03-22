using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using TeamstestRepo.Api.Models;

namespace TeamstestRepo.Api.Services;

/// <summary>
/// Bot obsługuje przychodzące aktywności z Teams — w szczególności
/// zdarzenia "invoke" z wypełnioną Adaptive Card.
/// </summary>
public sealed class ApprovalBot : ActivityHandler
{
    private readonly IApprovalService _approvalService;
    private readonly ILogger<ApprovalBot> _logger;

    public ApprovalBot(IApprovalService approvalService, ILogger<ApprovalBot> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }

    protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
    {
        if (turnContext.Activity.Name != "adaptiveCard/action")
        {
            return await base.OnInvokeActivityAsync(turnContext, cancellationToken);
        }

        var value = JObject.FromObject(turnContext.Activity.Value ?? new object());
        var data = value["action"]?["data"];

        if (data is null)
        {
            _logger.LogWarning("Received adaptiveCard/action without data payload.");
            return CreateInvokeResponse(StatusCodes.Status400BadRequest);
        }

        var payload = new AdaptiveCardPayload
        {
            ProcessId = data["processId"]?.ToString() ?? string.Empty,
            Decision = data["decision"]?.ToString() ?? string.Empty,
            Field1 = data["field1"]?.ToString() ?? string.Empty,
            Field2 = data["field2"]?.ToString() ?? string.Empty
        };

        var decidedByUserId = turnContext.Activity.From?.AadObjectId
                              ?? turnContext.Activity.From?.Id
                              ?? "unknown";

        _logger.LogInformation(
            "Decision '{Decision}' received for process {ProcessId} from {User}",
            payload.Decision, payload.ProcessId, decidedByUserId);

        await _approvalService.ProcessDecisionAsync(payload, decidedByUserId, cancellationToken);

        // Potwierdź akcję kartą z podziękowaniem
        var confirmationMessage = payload.Decision.Equals("approve", StringComparison.OrdinalIgnoreCase)
            ? "Proces został **zatwierdzony**."
            : "Proces został **odrzucony**.";

        await turnContext.SendActivityAsync(
            MessageFactory.Text(confirmationMessage), cancellationToken);

        return CreateInvokeResponse(StatusCodes.Status200OK);
    }

    private static InvokeResponse CreateInvokeResponse(int statusCode)
    {
        return new InvokeResponse { Status = statusCode };
    }
}
