using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using TeamstestRepo.Api.Models;

namespace TeamstestRepo.Api.Services;

public sealed class TeamsNotifier : ITeamsNotifier
{
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly IConfiguration _configuration;
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<TeamsNotifier> _logger;

    public TeamsNotifier(
        IBotFrameworkHttpAdapter adapter,
        IConfiguration configuration,
        GraphServiceClient graphClient,
        ILogger<TeamsNotifier> logger)
    {
        _adapter = adapter;
        _configuration = configuration;
        _graphClient = graphClient;
        _logger = logger;
    }

    public async Task SendApprovalCardAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        var cardJson = BuildCardJson(request);

        var botAppId = _configuration["BotFramework:AppId"]
            ?? throw new InvalidOperationException("BotFramework:AppId is not configured.");
        var tenantId = _configuration["BotFramework:TenantId"]
            ?? throw new InvalidOperationException("BotFramework:TenantId is not configured.");

        // Pobierz Teams userId z Entra ID (UPN → objectId lub bezpośrednio objectId)
        var teamsUserId = await ResolveTeamsUserIdAsync(request.UserId, cancellationToken);

        // Utwórz ConversationReference dla proaktywnej wiadomości
        var conversationReference = await CreateConversationReferenceAsync(
            teamsUserId, botAppId, tenantId, cancellationToken);

        await ((CloudAdapter)_adapter).ContinueConversationAsync(
            botAppId,
            conversationReference,
            async (turnContext, ct) =>
            {
                var card = new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = Newtonsoft.Json.JsonConvert.DeserializeObject(cardJson)
                };

                var message = MessageFactory.Attachment(card);
                await turnContext.SendActivityAsync(message, ct);
            },
            cancellationToken);

        _logger.LogInformation(
            "Approval card sent for process {ProcessId} to user {UserId}",
            request.ProcessId, request.UserId);
    }

    private async Task<string> ResolveTeamsUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        // Jeśli już jest GUID (objectId), użyj bezpośrednio
        if (Guid.TryParse(userId, out _))
            return userId;

        // W przeciwnym razie wyszukaj po UPN w Entra ID
        var user = await _graphClient.Users[userId]
            .GetAsync(cancellationToken: cancellationToken);

        if (user?.Id is null)
            throw new InvalidOperationException($"User '{userId}' not found in Entra ID.");

        return user.Id;
    }

    private static async Task<ConversationReference> CreateConversationReferenceAsync(
        string teamsUserId, string botAppId, string tenantId, CancellationToken cancellationToken)
    {
        // Bot Framework wymaga ServiceUrl dla Teams
        // Dla Microsoft Teams produkcyjny ServiceUrl to: https://smba.trafficmanager.net/emea/
        // Można go też pobrać z pierwszej przychodzącej aktywności i zapisać w cache
        var serviceUrl = "https://smba.trafficmanager.net/emea/";

        return new ConversationReference
        {
            ServiceUrl = serviceUrl,
            Bot = new ChannelAccount { Id = $"28:{botAppId}" },
            User = new ChannelAccount { Id = $"29:{teamsUserId}" },
            Conversation = new ConversationAccount
            {
                TenantId = tenantId,
                IsGroup = false
            },
            ChannelId = "msteams"
        };
    }

    private static string BuildCardJson(ApprovalRequest request)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Cards", "ApprovalCard.json");
        var template = File.ReadAllText(templatePath);

        return template
            .Replace("{{ProcessId}}", request.ProcessId)
            .Replace("{{ProcessTitle}}", request.ProcessTitle ?? $"Proces: {request.ProcessId}")
            .Replace("{{Field1Label}}", request.Field1Label)
            .Replace("{{Field2Label}}", request.Field2Label);
    }
}
