namespace Storage.Helpers.DTOs;

public sealed class ProjectLocationSummaryResponse
{
    public Guid Id { get; set; }
    public Guid ShelfId { get; set; }
    public Guid AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsCurrentActiveProject { get; set; }
}

public sealed class CloseProjectRequest
{
    public bool Confirm { get; set; }
}

public sealed class CloseProjectResponse
{
    public Guid ProjectLocationId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int ReturnedLineCount { get; set; }
    public int ReturnedQuantity { get; set; }
    public bool Closed { get; set; }
}

public sealed class ActiveProjectResponse
{
    public ProjectLocationSummaryResponse? ActiveProject { get; set; }
}

public sealed class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}