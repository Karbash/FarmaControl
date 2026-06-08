using FarmaControl.Application.Care.Appointments.Models;
using FarmaControl.Application.Care.Patients.Models;
using FarmaControl.Domain.Care;

namespace FarmaControl.Tests.Application;

public sealed class CareModelTests
{
    [Fact]
    public void PatientInputModel_RequiresName()
    {
        var model = new PatientInputModel("", null, null, null, null, null, null, [], true);

        var errors = model.Validate();

        Assert.Contains(errors, error => error.Message == "Nome e obrigatorio.");
    }

    [Fact]
    public void AppointmentInputModel_RequiresPatient()
    {
        var model = new AppointmentInputModel(0, null, null, null, false, null, null);

        var errors = model.Validate();

        Assert.Contains(errors, error => error.Message == "Paciente e obrigatorio.");
    }

    [Fact]
    public void AppointmentInputModel_CreatesWaitingAppointment()
    {
        var model = new AppointmentInputModel(
            10,
            DateOnly.FromDateTime(DateTime.Today),
            new TimeOnly(8, 30),
            "consulta",
            false,
            "Responsavel",
            null);

        CareAppointment appointment = model.ToDomain();

        Assert.Equal(10, appointment.PatientId);
        Assert.Equal(AppointmentStatus.Waiting, appointment.Status);
        Assert.Equal("consulta", appointment.Type);
    }

    [Fact]
    public void UpdateAppointmentStatusModel_RejectsInvalidStatus()
    {
        var model = new UpdateAppointmentStatusModel("invalido", null);

        var errors = model.Validate();

        Assert.Contains(errors, error => error.Message == "Status invalido.");
    }
}
