using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TeamstestRepo.Api.Models;
using TeamstestRepo.Api.Services;
using Xunit;

namespace TeamstestRepo.IntegrationTests;

public sealed class ApprovalsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IApprovalService> _approvalServiceMock = new();

    public ApprovalsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Zastąp serwisy mockami
                services.AddScoped<IApprovalService>(_ => _approvalServiceMock.Object);
            });
            builder.UseSetting("ApiKey:InboundSecret", "test-api-key");
            builder.UseSetting("BotFramework:AppId", "test-app-id");
            builder.UseSetting("BotFramework:TenantId", "test-tenant-id");
            builder.UseSetting("BotFramework:AppSecret", "test-secret");
        });
    }

    [Fact]
    public async Task PostApprovals_WithValidApiKeyAndBody_Returns202()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key");

        var request = new ApprovalRequest
        {
            ProcessId = "proc-001",
            UserId = "user@firma.pl",
            CallbackUrl = "https://external.system/callback",
            Field1Label = "Komentarz",
            Field2Label = "Numer zlecenia"
        };

        _approvalServiceMock
            .Setup(s => s.InitiateApprovalAsync(It.IsAny<ApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await client.PostAsJsonAsync("/approvals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await response.Content.ReadFromJsonAsync<ApprovalCreatedResponse>();
        body!.ProcessId.Should().Be("proc-001");
    }

    [Fact]
    public async Task PostApprovals_WithoutApiKey_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new ApprovalRequest
        {
            ProcessId = "proc-002",
            UserId = "user@firma.pl",
            CallbackUrl = "https://external.system/callback",
            Field1Label = "Komentarz",
            Field2Label = "Numer zlecenia"
        };

        // Act
        var response = await client.PostAsJsonAsync("/approvals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostApprovals_WithWrongApiKey_Returns403()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var request = new ApprovalRequest
        {
            ProcessId = "proc-003",
            UserId = "user@firma.pl",
            CallbackUrl = "https://external.system/callback",
            Field1Label = "Komentarz",
            Field2Label = "Numer zlecenia"
        };

        // Act
        var response = await client.PostAsJsonAsync("/approvals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostApprovals_WithMissingRequiredFields_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key");

        var incompleteBody = new { processId = "proc-004" }; // brak wymaganych pól

        // Act
        var response = await client.PostAsJsonAsync("/approvals", incompleteBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
