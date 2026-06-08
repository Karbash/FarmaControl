using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.MedicalAttendances.Models;

public sealed record Cid10ItemModel(
    int Order,
    long Cid10CodeId,
    string Code,
    string Name)
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (Order <= 0)
        {
            errors.Add(AppError.Validation("Ordem do CID-10 deve ser maior que zero."));
        }

        if (Cid10CodeId <= 0)
        {
            errors.Add(AppError.Validation("CID-10 e obrigatorio."));
        }

        return errors;
    }

    public MedicalAttendanceCid10Item ToDomain()
    {
        return MedicalAttendanceCid10Item.Create(Order, Cid10CodeId, Code, Name);
    }

    public static Cid10ItemModel FromRequest(MedicalAttendanceCid10Request request)
    {
        return new Cid10ItemModel(request.Order, request.Cid10CodeId, request.Code, request.Name);
    }

    public static MedicalAttendanceCid10Response FromDomain(MedicalAttendanceCid10Item item)
    {
        return new MedicalAttendanceCid10Response(
            item.Id,
            item.Order,
            item.Cid10CodeId,
            item.Code,
            item.Name);
    }
}
