using Azure.Identity;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Graph;
using TeamstestRepo.Api.Middleware;
using TeamstestRepo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Teams Approval Bot API", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new()
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-Api-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
});

// ── Bot Framework ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, CloudAdapter>();
builder.Services.AddTransient<IBot, ApprovalBot>();

// ── Microsoft Graph (do wyszukiwania użytkowników z Entra ID) ─────────────────
builder.Services.AddSingleton<GraphServiceClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var tenantId = config["BotFramework:TenantId"]
        ?? throw new InvalidOperationException("BotFramework:TenantId is not configured.");
    var clientId = config["BotFramework:AppId"]
        ?? throw new InvalidOperationException("BotFramework:AppId is not configured.");
    var clientSecret = config["BotFramework:AppSecret"]
        ?? throw new InvalidOperationException("BotFramework:AppSecret is not configured.");

    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    return new GraphServiceClient(credential);
});

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<ITeamsNotifier, TeamsNotifier>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();

// HttpClient dla callbacków do zewnętrznego systemu
builder.Services.AddHttpClient<ApprovalService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var timeoutSeconds = config.GetValue("ExternalApi:TimeoutSeconds", 30);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

// ── Problem Details ───────────────────────────────────────────────────────────
builder.Services.AddProblemDetails();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiKeyMiddleware>();

app.UseRouting();
app.MapControllers();

app.Run();

// Umożliwia WebApplicationFactory w testach integracyjnych
public partial class Program { }
