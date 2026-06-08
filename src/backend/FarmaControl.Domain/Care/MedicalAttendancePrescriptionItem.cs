using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Care;

public sealed class MedicalAttendancePrescriptionItem : Entity
{
    private MedicalAttendancePrescriptionItem()
    {
    }

    private MedicalAttendancePrescriptionItem(
        int order,
        string? description,
        long? medicationId,
        string? medicationName,
        string? dosage,
        string? directions,
        int? quantity)
    {
        Order = order;
        Description = description;
        MedicationId = medicationId;
        MedicationName = Normalize(medicationName);
        Dosage = Normalize(dosage);
        Directions = Normalize(directions);
        Quantity = quantity;
    }

    public long MedicalAttendanceId { get; private set; }

    public int Order { get; private set; }

    public string? Description { get; private set; }

    public long? MedicationId { get; private set; }

    public string? MedicationName { get; private set; }

    public string? Dosage { get; private set; }

    public string? Directions { get; private set; }

    public int? Quantity { get; private set; }

    public static MedicalAttendancePrescriptionItem Create(
        int order,
        string? description,
        long? medicationId = null,
        string? medicationName = null,
        string? dosage = null,
        string? directions = null,
        int? quantity = null)
    {
        return new MedicalAttendancePrescriptionItem(
            order,
            description,
            medicationId,
            medicationName,
            dosage,
            directions,
            quantity);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
