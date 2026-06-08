using FarmaControl.Application.Care.MedicalAttendances.Models;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Tests.Application;

public sealed class MedicalAttendanceModelTests
{
    [Fact]
    public void CreateModel_ValidatesRequiredFields()
    {
        var model = new CreateMedicalAttendanceModel(
            0,
            0,
            null,
            null,
            "",
            null,
            DateOnly.FromDateTime(DateTime.Today),
            null,
            null,
            null,
            null,
            "invalido",
            null,
            VitalSignsModel.Empty(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [],
            [],
            [],
            [],
            null,
            false);

        var errors = model.Validate();

        Assert.Contains(errors, error => error.Message == "Atendimento e obrigatorio.");
        Assert.Contains(errors, error => error.Message == "Paciente e obrigatorio.");
        Assert.Contains(errors, error => error.Message == "Nome e obrigatorio.");
        Assert.Contains(errors, error => error.Message == "Tipo de pessoa invalido.");
    }

    [Fact]
    public void CreateModel_FromRequestConvertsToDomain()
    {
        var request = new CreateMedicalAttendanceRequest(
            7,
            3,
            "Enfermeira",
            "Paciente",
            40,
            DateOnly.FromDateTime(DateTime.Today),
            new TimeOnly(9, 30),
            "Cidade",
            "Igreja",
            "Pastor",
            "PART",
            1,
            new VitalSignsRequest(120, 80, 36.5m, 90m, 98, 72),
            "Queixa",
            "HPP",
            "HDA",
            "Exame",
            [new MedicalAttendancePrescriptionItemRequest(1, "Prescricao")],
            [new MedicalAttendanceNursingCheckItemRequest(1, "Checagem")],
            [new MedicalAttendanceDispensationItemRequest(1, "Lote A")],
            "Assinatura",
            false,
            "senha-assinatura",
            "Hipotese",
            "A09",
            "Diarreia e gastroenterite de origem infecciosa presumivel",
            [new MedicalAttendanceCid10Request(
                1,
                10,
                "A09",
                "Diarreia e gastroenterite de origem infecciosa presumivel")]);

        CreateMedicalAttendanceModel model = CreateMedicalAttendanceModel.FromRequest(5, request);
        MedicalAttendance attendance = model.ToDomain();

        Assert.Empty(model.Validate());
        Assert.Equal(5, attendance.AppointmentId);
        Assert.Equal(7, attendance.PatientId);
        Assert.Equal(AttendanceType.Participante, attendance.AttendanceType);
        Assert.Equal(120, attendance.VitalSigns.SystolicPressure);
        Assert.Equal("A09", attendance.Cid10Code);
        MedicalAttendanceCid10Item cid10 = Assert.Single(attendance.Cid10Codes);
        Assert.Equal(10, cid10.Cid10CodeId);
        Assert.Equal("A09", cid10.Code);
        Assert.Equal("Hipotese", attendance.DiagnosticHypothesis);
        Assert.Single(attendance.Prescriptions);
        Assert.Single(attendance.NursingChecks);
        Assert.Single(attendance.Dispensations);
    }
}
