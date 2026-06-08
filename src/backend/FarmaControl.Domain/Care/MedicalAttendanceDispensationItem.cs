using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Care;

public sealed class MedicalAttendanceDispensationItem : Entity
{
    private MedicalAttendanceDispensationItem()
    {
    }

    private MedicalAttendanceDispensationItem(
        int order,
        string? batch,
        long? prescriptionId,
        long? medicationId,
        string? medicationName,
        int? quantity,
        string? responsible,
        DateTimeOffset? dispensedAt)
    {
        Order = order;
        Batch = Normalize(batch);
        PrescriptionId = prescriptionId;
        MedicationId = medicationId;
        MedicationName = Normalize(medicationName);
        Quantity = quantity;
        Responsible = Normalize(responsible);
        DispensedAt = dispensedAt;
    }

    public long MedicalAttendanceId { get; private set; }

    public int Order { get; private set; }

    public string? Batch { get; private set; }

    public long? PrescriptionId { get; private set; }

    public long? MedicationId { get; private set; }

    public string? MedicationName { get; private set; }

    public int? Quantity { get; private set; }

    public string? Responsible { get; private set; }

    public DateTimeOffset? DispensedAt { get; private set; }

    public static MedicalAttendanceDispensationItem Create(int order, string? batch)
    {
        return new MedicalAttendanceDispensationItem(order, batch, null, null, null, null, null, null);
    }

    public static MedicalAttendanceDispensationItem CreateFromPrescription(
        int order,
        string? batch,
        long prescriptionId,
        long medicationId,
        string? medicationName,
        int quantity,
        string responsible,
        DateTimeOffset dispensedAt)
    {
        if (prescriptionId <= 0)
        {
            throw new ArgumentException("Prescricao e obrigatoria.", nameof(prescriptionId));
        }

        if (medicationId <= 0)
        {
            throw new ArgumentException("Medicamento e obrigatorio.", nameof(medicationId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantity));
        }

        return new MedicalAttendanceDispensationItem(
            order,
            batch,
            prescriptionId,
            medicationId,
            medicationName,
            quantity,
            responsible,
            dispensedAt);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
