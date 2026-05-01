using FluentAssertions;
using Storage.Helpers.DTOs;
using Storage.Helpers.Validators;

namespace Storage.Backend.UnitTests.Validators;

public sealed class CreateProjectRequestValidatorTests
{
    private readonly CreateProjectRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenRequestIsValid_ShouldPass()
    {
        var request = new CreateProjectRequest
        {
            Name = "Project Alpha",
            Code = "ALPHA-1"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
