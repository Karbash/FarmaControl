using FarmaControl.Domain.Care;

namespace FarmaControl.Tests.Domain;

public sealed class MedicalAttendanceTests
{
    [Fact]
    public void Create_RequiresAppointment()
    {
        Assert.Throws<ArgumentException>(() => MedicalAttendance.Create(
            0,
            1,
            null,
            "Responsavel",
            "Paciente",
            30,
            DateOnly.FromDateTime(DateTime.Today),
            null,
            null,
            null,
            null,
            AttendanceType.Participante,
            null));
    }

    [Fact]
    public void Create_RequiresPatient()
    {
        Assert.Throws<ArgumentException>(() => MedicalAttendance.Create(
            1,
            0,
            null,
            "Responsavel",
            "Paciente",
            30,
            DateOnly.FromDateTime(DateTime.Today),
            null,
            null,
            null,
            null,
            AttendanceType.Participante,
            null));
    }

    [Fact]
    public void Create_RequiresName()
    {
        Assert.Throws<ArgumentException>(() => MedicalAttendance.Create(
            1,
            1,
            null,
            "Responsavel",
            "",
            30,
            DateOnly.FromDateTime(DateTime.Today),
            null,
            null,
            null,
            null,
            AttendanceType.Participante,
            null));
    }

    [Fact]
    public void ReplacePrescriptions_OrdersItems()
    {
        MedicalAttendance attendance = CreateAttendance();

        attendance.ReplacePrescriptions(
        [
            MedicalAttendancePrescriptionItem.Create(2, "Item 2"),
            MedicalAttendancePrescriptionItem.Create(1, "Item 1")
        ]);

        int[] orders = attendance.Prescriptions
            .Select(item => item.Order)
            .ToArray();

        Assert.Equal([1, 2], orders);
    }

    private static MedicalAttendance CreateAttendance()
    {
        return MedicalAttendance.Create(
            1,
            1,
            10,
            "Responsavel",
            "Paciente",
            30,
            DateOnly.FromDateTime(DateTime.Today),
            TimeOnly.FromDateTime(DateTime.Now),
            "Cidade",
            "Igreja",
            "Pastor",
            AttendanceType.Participante,
            null);
    }
}
