using FarmaControl.Application.Audit.Models;

namespace FarmaControl.Tests.Application;

public sealed class AuditModelTests
{
    [Fact]
    public void AuditLogFilterModel_RejectsStartDateAfterEndDate()
    {
        var model = new AuditLogFilterModel(
            null,
            null,
            null,
            new DateOnly(2026, 06, 05),
            new DateOnly(2026, 06, 01));

        var errors = model.Validate();

        Assert.Contains(errors, error => error.Message == "Data inicial nao pode ser maior que data final.");
    }
}
