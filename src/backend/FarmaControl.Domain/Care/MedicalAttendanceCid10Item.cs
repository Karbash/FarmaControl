using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Care;

public sealed class MedicalAttendanceCid10Item : Entity
{
    private MedicalAttendanceCid10Item()
    {
    }

    private MedicalAttendanceCid10Item(int order, long cid10CodeId, string code, string name)
    {
        Order = order;
        Cid10CodeId = cid10CodeId;
        Code = NormalizeRequired(code);
        Name = NormalizeRequired(name);
    }

    public long MedicalAttendanceId { get; private set; }

    public int Order { get; private set; }

    public long Cid10CodeId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public static MedicalAttendanceCid10Item Create(
        int order,
        long cid10CodeId,
        string code,
        string name)
    {
        if (order <= 0)
        {
            throw new ArgumentException("Ordem do CID-10 deve ser maior que zero.", nameof(order));
        }

        if (cid10CodeId <= 0)
        {
            throw new ArgumentException("CID-10 e obrigatorio.", nameof(cid10CodeId));
        }

        return new MedicalAttendanceCid10Item(order, cid10CodeId, code, name);
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Codigo e nome do CID-10 sao obrigatorios.", nameof(value));
        }

        return value.Trim();
    }
}
