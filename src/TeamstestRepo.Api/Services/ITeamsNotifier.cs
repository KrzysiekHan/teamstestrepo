using TeamstestRepo.Api.Models;

namespace TeamstestRepo.Api.Services;

public interface ITeamsNotifier
{
    /// <summary>
    /// Wysyła Adaptive Card z formularzem zatwierdzenia do wskazanego użytkownika w Teams.
    /// </summary>
    Task SendApprovalCardAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
}
