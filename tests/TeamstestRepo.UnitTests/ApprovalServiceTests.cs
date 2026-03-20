using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TeamstestRepo.Api.Models;
using TeamstestRepo.Api.Services;
using Xunit;

namespace TeamstestRepo.UnitTests;

public sealed class ApprovalServiceTests
{
    private readonly Mock<ITeamsNotifier> _teamsNotifierMock = new();
    private readonly Mock<HttpMessageHandler> _httpHandlerMock = new();
    private readonly HttpClient _httpClient;
    private readonly ApprovalService _sut;

    public ApprovalServiceTests()
    {
        _httpClient = new HttpClient(_httpHandlerMock.Object);
        _sut = new ApprovalService(
            _teamsNotifierMock.Object,
            _httpClient,
            NullLogger<ApprovalService>.Instance);
    }

    [Fact]
    public async Task InitiateApprovalAsync_ValidRequest_SendsApprovalCard()
    {
        // Arrange
        var request = BuildRequest();
        _teamsNotifierMock
            .Setup(n => n.SendApprovalCardAsync(request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.InitiateApprovalAsync(request);

        // Assert
        _teamsNotifierMock.Verify(
            n => n.SendApprovalCardAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDecisionAsync_ValidApprove_CallsCallbackWithCorrectPayload()
    {
        // Arrange
        var request = BuildRequest();
        await _sut.InitiateApprovalAsync(request); // rejestruje callbackUrl

        ApprovalDecision? capturedDecision = null;
        SetupHttpHandler(HttpStatusCode.OK, req =>
        {
            capturedDecision = req.Content!.ReadFromJsonAsync<ApprovalDecision>().GetAwaiter().GetResult();
        });

        var payload = new AdaptiveCardPayload
        {
            ProcessId = request.ProcessId,
            Decision = "approve",
            Field1 = "wartość 1",
            Field2 = "wartość 2"
        };

        // Act
        await _sut.ProcessDecisionAsync(payload, "user@firma.pl");

        // Assert
        capturedDecision.Should().NotBeNull();
        capturedDecision!.ProcessId.Should().Be(request.ProcessId);
        capturedDecision.Decision.Should().Be(DecisionType.Approve);
        capturedDecision.DecidedBy.Should().Be("user@firma.pl");
        capturedDecision.Field1.Should().Be("wartość 1");
        capturedDecision.Field2.Should().Be("wartość 2");
    }

    [Fact]
    public async Task ProcessDecisionAsync_ValidReject_CallsCallbackWithRejectDecision()
    {
        // Arrange
        var request = BuildRequest("proc-002");
        await _sut.InitiateApprovalAsync(request);

        ApprovalDecision? capturedDecision = null;
        SetupHttpHandler(HttpStatusCode.OK, req =>
        {
            capturedDecision = req.Content!.ReadFromJsonAsync<ApprovalDecision>().GetAwaiter().GetResult();
        });

        var payload = new AdaptiveCardPayload
        {
            ProcessId = request.ProcessId,
            Decision = "reject",
            Field1 = "powód odrzucenia",
            Field2 = ""
        };

        // Act
        await _sut.ProcessDecisionAsync(payload, "user@firma.pl");

        // Assert
        capturedDecision!.Decision.Should().Be(DecisionType.Reject);
    }

    [Fact]
    public async Task ProcessDecisionAsync_UnknownProcessId_DoesNotCallCallback()
    {
        // Arrange
        var payload = new AdaptiveCardPayload
        {
            ProcessId = "nieznany-id",
            Decision = "approve",
            Field1 = "",
            Field2 = ""
        };

        // Act
        await _sut.ProcessDecisionAsync(payload, "user@firma.pl");

        // Assert — brak wywołania HTTP
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDecisionAsync_CallbackReturnsError_ThrowsException()
    {
        // Arrange
        var request = BuildRequest("proc-003");
        await _sut.InitiateApprovalAsync(request);

        SetupHttpHandler(HttpStatusCode.InternalServerError, _ => { });

        var payload = new AdaptiveCardPayload
        {
            ProcessId = request.ProcessId,
            Decision = "approve",
            Field1 = "",
            Field2 = ""
        };

        // Act
        var act = () => _sut.ProcessDecisionAsync(payload, "user@firma.pl");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ApprovalRequest BuildRequest(string processId = "proc-001") => new()
    {
        ProcessId = processId,
        UserId = "user@firma.pl",
        CallbackUrl = "https://external.system/callback",
        Field1Label = "Komentarz",
        Field2Label = "Numer zlecenia"
    };

    private void SetupHttpHandler(HttpStatusCode statusCode, Action<HttpRequestMessage> capture)
    {
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capture(req))
            .ReturnsAsync(new HttpResponseMessage(statusCode));
    }
}
