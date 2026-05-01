using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storage.Helpers.DTOs;
using Storage.Services.Search;

namespace Storage.Controllers.Items;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet("components")]
    public async Task<ActionResult<PaginatedResponse<ComponentResponse>>> SearchComponents(
        [FromQuery(Name = "q")] string? query,
        [FromQuery(Name = "type")] Guid? typeId,
        [FromQuery(Name = "manufacturer")] string? manufacturer,
        [FromQuery(Name = "category")] Guid? categoryId,
        [FromQuery] Guid? supplierId,
        [FromQuery] string? partNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filters = new ComponentSearchRequest
        {
            TypeId = typeId,
            CategoryId = categoryId,
            SupplierId = supplierId,
            Manufacturer = manufacturer,
            PartNumber = partNumber,
            Page = page,
            PageSize = pageSize
        };

        var result = await _searchService.SearchComponentsAsync(query, filters, cancellationToken);
        return Ok(result);
    }

    [HttpGet("locations")]
    public async Task<ActionResult<IReadOnlyList<LocationResponse>>> SearchLocations(
        [FromQuery(Name = "q")] string? query,
        [FromQuery] Guid? areaId,
        [FromQuery] Guid? shelfId,
        [FromQuery] bool? hasStock,
        CancellationToken cancellationToken = default)
    {
        var filters = new LocationSearchRequest
        {
            AreaId = areaId,
            ShelfId = shelfId,
            HasStock = hasStock
        };

        var result = await _searchService.SearchLocationsAsync(query, filters, cancellationToken);
        return Ok(result);
    }
}
