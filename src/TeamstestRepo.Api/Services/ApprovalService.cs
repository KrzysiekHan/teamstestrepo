using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeamstestRepo.Api.Models;

namespace TeamstestRepo.Api.Services;

public sealed class ApprovalService : IApprovalService
{
    // Przechowuje callbackUrl dla processId w pamięci.
    // W produkcji zastąp trwałym storage (np. Azure Table Storage, Redis, DB).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _callbackRegistry = new();

    private readonly ITeamsNotifier _teamsNotifier;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApprovalService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ApprovalService(
        ITeamsNotifier teamsNotifier,
        HttpClient httpClient,
        ILogger<ApprovalService> logger)
    {
        _teamsNotifier = teamsNotifier;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task InitiateApprovalAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        _callbackRegistry[request.ProcessId] = request.CallbackUrl;

        await _teamsNotifier.SendApprovalCardAsync(request, cancellationToken);

        _logger.LogInformation(
            "Approval initiated for process {ProcessId}, awaiting decision from {UserId}",
            request.ProcessId, request.UserId);
    }

    public async Task ProcessDecisionAsync(
        AdaptiveCardPayload payload,
        string decidedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (!_callbackRegistry.TryRemove(payload.ProcessId, out var callbackUrl))
        {
            _logger.LogWarning(
                "No callback URL found for process {ProcessId}. Decision ignored.",
                payload.ProcessId);
            return;
        }

        if (!Enum.TryParse<DecisionType>(payload.Decision, ignoreCase: true, out var decision))
        {
            _logger.LogWarning(
                "Unknown decision value '{Decision}' for process {ProcessId}.",
                payload.Decision, payload.ProcessId);
            return;
        }

        var decisionPayload = new ApprovalDecision
        {
            ProcessId = payload.ProcessId,
            Decision = decision,
            DecidedBy = decidedByUserId,
            DecidedAt = DateTimeOffset.UtcNow,
            Field1 = payload.Field1,
            Field2 = payload.Field2
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                callbackUrl, decisionPayload, _jsonOptions, cancellationToken);

            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Decision '{Decision}' for process {ProcessId} sent to callback {CallbackUrl}",
                decision, payload.ProcessId, callbackUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send decision for process {ProcessId} to {CallbackUrl}",
                payload.ProcessId, callbackUrl);
            throw;
        }
    }
}
