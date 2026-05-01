using Storage.Helpers.DTOs;

namespace Storage.Services.Search;

public interface ISearchService
{
    Task<PaginatedResponse<ComponentResponse>> SearchComponentsAsync(string? query, ComponentSearchRequest filters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocationResponse>> SearchLocationsAsync(string? query, LocationSearchRequest filters, CancellationToken cancellationToken = default);
}
