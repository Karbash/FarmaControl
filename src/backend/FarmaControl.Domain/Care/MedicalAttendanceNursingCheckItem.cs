using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Care;

public sealed class MedicalAttendanceNursingCheckItem : Entity
{
    private MedicalAttendanceNursingCheckItem()
    {
    }

    private MedicalAttendanceNursingCheckItem(int order, string? description)
    {
        Order = order;
        Description = description;
    }

    public long MedicalAttendanceId { get; private set; }

    public int Order { get; private set; }

    public string? Description { get; private set; }

    public static MedicalAttendanceNursingCheckItem Create(int order, string? description)
    {
        return new MedicalAttendanceNursingCheckItem(order, description);
    }
}
