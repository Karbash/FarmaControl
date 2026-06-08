using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Inventory;

public sealed class StockMovement : Entity
{
    private StockMovement()
    {
    }

    private StockMovement(
        string type,
        long medicationId,
        int quantity,
        DateOnly date,
        string responsible,
        string? notes,
        string? batch,
        string? reason,
        long? attendanceId,
        long? appointmentId,
        long? prescriptionId)
    {
        Type = NormalizeRequired(type, nameof(type));
        MedicationId = medicationId;
        Quantity = ValidateQuantity(quantity);
        Date = date;
        Responsible = NormalizeRequired(responsible, nameof(responsible));
        Notes = Normalize(notes);
        Batch = Normalize(batch);
        Reason = Normalize(reason);
        AttendanceId = attendanceId;
        AppointmentId = appointmentId;
        PrescriptionId = prescriptionId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Type { get; private set; } = string.Empty;

    public long MedicationId { get; private set; }

    public int Quantity { get; private set; }

    public DateOnly Date { get; private set; }

    public string Responsible { get; private set; } = string.Empty;

    public string? Notes { get; private set; }

    public string? Batch { get; private set; }

    public string? Reason { get; private set; }

    public long? AttendanceId { get; private set; }

    public long? AppointmentId { get; private set; }

    public long? PrescriptionId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static StockMovement Create(
        string type,
        long medicationId,
        int quantity,
        DateOnly date,
        string responsible,
        string? notes,
        string? batch,
        string? reason,
        long? attendanceId = null,
        long? appointmentId = null,
        long? prescriptionId = null)
    {
        if (medicationId <= 0)
        {
            throw new ArgumentException("Medicamento e obrigatorio.", nameof(medicationId));
        }

        return new StockMovement(
            type,
            medicationId,
            quantity,
            date,
            responsible,
            notes,
            batch,
            reason,
            attendanceId,
            appointmentId,
            prescriptionId);
    }

    private static int ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantity));
        }

        return quantity;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Campo obrigatorio.", paramName);
        }

        return value.Trim();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
