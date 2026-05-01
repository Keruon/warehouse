using Storage.Helpers.DTOs;

namespace Storage.Services.Projects;

public interface IProjectLocationService
{
    Task<Guid> ResolveShelfIdAsync(LocationKind locationKind, Guid? requestedShelfId, Guid actorId, CancellationToken cancellationToken = default);
    Task EnsureShelfSupportsProjectLocationAsync(Guid shelfId, CancellationToken cancellationToken = default);
    Task<WarehouseLocation> GetProjectLocationAsync(Guid locationId, bool requireActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectLocationSummaryResponse>> ListProjectsAsync(Guid? currentUserId, CancellationToken cancellationToken = default);
    Task<ProjectLocationSummaryResponse?> GetActiveProjectAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ProjectLocationSummaryResponse> SetActiveProjectAsync(Guid userId, Guid locationId, CancellationToken cancellationToken = default);
    Task ClearActiveProjectAsync(Guid userId, CancellationToken cancellationToken = default);
}