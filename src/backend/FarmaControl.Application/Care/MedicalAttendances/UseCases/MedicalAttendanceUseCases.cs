using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.Appointments.Abstractions;
using FarmaControl.Application.Care.Cid10;
using FarmaControl.Application.Care.MedicalAttendances.Abstractions;
using FarmaControl.Application.Care.MedicalAttendances.Models;
using FarmaControl.Application.Care.Patients.Abstractions;
using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;
using FarmaControl.Domain.Inventory;
using FarmaControl.Domain.Users;

namespace FarmaControl.Application.Care.MedicalAttendances.UseCases;

public sealed record GetMedicalAttendanceRequest(long Id);

public sealed class GetMedicalAttendanceUseCase(IMedicalAttendanceRepository attendances)
    : IUseCase<GetMedicalAttendanceRequest, Result<MedicalAttendanceResponse>>
{
    public async Task<Result<MedicalAttendanceResponse>> ExecuteAsync(
        GetMedicalAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        MedicalAttendance? attendance = await attendances.GetByIdAsync(request.Id, cancellationToken);
        return attendance is null
            ? Result<MedicalAttendanceResponse>.Failure(AppError.NotFound("Ficha de atendimento nao encontrada."))
            : Result<MedicalAttendanceResponse>.Success(MedicalAttendanceModel.FromDomain(attendance));
    }
}

public sealed record GetMedicalAttendanceByAppointmentRequest(long AppointmentId);

public sealed class GetMedicalAttendanceByAppointmentUseCase(IMedicalAttendanceRepository attendances)
    : IUseCase<GetMedicalAttendanceByAppointmentRequest, Result<MedicalAttendanceResponse>>
{
    public async Task<Result<MedicalAttendanceResponse>> ExecuteAsync(
        GetMedicalAttendanceByAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        MedicalAttendance? attendance = await attendances.GetByAppointmentIdAsync(
            request.AppointmentId,
            cancellationToken);

        return attendance is null
            ? Result<MedicalAttendanceResponse>.Failure(AppError.NotFound("Ficha de atendimento nao encontrada."))
            : Result<MedicalAttendanceResponse>.Success(MedicalAttendanceModel.FromDomain(attendance));
    }
}

public sealed record CreateMedicalAttendanceCommand(
    long AppointmentId,
    long ActorUserId,
    CreateMedicalAttendanceRequest Request);

public sealed class CreateMedicalAttendanceUseCase(
    IMedicalAttendanceRepository attendances,
    IAppointmentRepository appointments,
    IPatientRepository patients,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ICid10Catalog cid10Catalog,
    IUnitOfWork unitOfWork)
    : IUseCase<CreateMedicalAttendanceCommand, Result<MedicalAttendanceResponse>>
{
    public async Task<Result<MedicalAttendanceResponse>> ExecuteAsync(
        CreateMedicalAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        User? actor = await users.GetByIdAsync(request.ActorUserId, cancellationToken);
        AppError? actorError = MedicalAttendancePermissionGuard.ValidateClinicalActor(actor);
        if (actorError is not null)
        {
            return Result<MedicalAttendanceResponse>.Failure(actorError);
        }

        AppError? signatureError = MedicalAttendancePermissionGuard.ValidateSignaturePassword(
            actor!,
            request.Request.SignaturePassword,
            passwordHasher);
        if (signatureError is not null)
        {
            return Result<MedicalAttendanceResponse>.Failure(signatureError);
        }

        CreateMedicalAttendanceModel model = CreateMedicalAttendanceModel.FromRequest(
            request.AppointmentId,
            request.Request,
            actor!.Id,
            actor.Name);

        IReadOnlyList<AppError> errors = model.Validate();
        if (errors.HasErrors())
        {
            return Result<MedicalAttendanceResponse>.Failure(errors.FirstOrDefaultError());
        }

        Result<CreateMedicalAttendanceModel> resolvedCid10 =
            await Cid10SelectionResolver.ResolveCreateAsync(model, cid10Catalog, cancellationToken);
        if (!resolvedCid10.IsSuccess || resolvedCid10.Value is null)
        {
            return Result<MedicalAttendanceResponse>.Failure(resolvedCid10.Error!);
        }

        model = resolvedCid10.Value;

        AppError? permissionError = MedicalAttendancePermissionGuard.EnsureCreatePermissions(actor.Role, model);
        if (permissionError is not null)
        {
            return Result<MedicalAttendanceResponse>.Failure(permissionError);
        }

        CareAppointment? appointment = await appointments.GetByIdAsync(model.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result<MedicalAttendanceResponse>.Failure(AppError.NotFound("Atendimento nao encontrado."));
        }

        if (appointment.PatientId != model.PatientId)
        {
            return Result<MedicalAttendanceResponse>.Failure(
                AppError.Validation("Paciente informado nao pertence ao atendimento."));
        }

        Patient? patient = await patients.GetByIdAsync(model.PatientId, cancellationToken);
        if (patient is null)
        {
            return Result<MedicalAttendanceResponse>.Failure(AppError.NotFound("Paciente nao encontrado."));
        }

        bool alreadyExists = await attendances.ExistsForAppointmentAsync(
            model.AppointmentId,
            cancellationToken);
        if (alreadyExists)
        {
            return Result<MedicalAttendanceResponse>.Failure(
                AppError.Conflict("Atendimento ja possui ficha cadastrada."));
        }

        MedicalAttendance attendance = model.ToDomain();
        MedicalAttendanceStageSigner.Sign(attendance, model, actor!);

        await unitOfWork.ExecuteInTransactionAsync(
            async ct => await attendances.AddAsync(attendance, ct),
            cancellationToken);

        return Result<MedicalAttendanceResponse>.Success(MedicalAttendanceModel.FromDomain(attendance));
    }
}

public sealed record UpdateMedicalAttendanceCommand(
    long Id,
    long ActorUserId,
    UpdateMedicalAttendanceRequest Request);

public sealed class UpdateMedicalAttendanceUseCase(
    IMedicalAttendanceRepository attendances,
    IAppointmentRepository appointments,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IMedicationRepository medications,
    IStockMovementRepository movements,
    ICid10Catalog cid10Catalog,
    IUnitOfWork unitOfWork)
    : IUseCase<UpdateMedicalAttendanceCommand, Result<MedicalAttendanceResponse>>
{
    public async Task<Result<MedicalAttendanceResponse>> ExecuteAsync(
        UpdateMedicalAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        User? actor = await users.GetByIdAsync(request.ActorUserId, cancellationToken);
        AppError? actorError = MedicalAttendancePermissionGuard.ValidateClinicalActor(actor);
        if (actorError is not null)
        {
            return Result<MedicalAttendanceResponse>.Failure(actorError);
        }

        AppError? signatureError = MedicalAttendancePermissionGuard.ValidateSignaturePassword(
            actor!,
            request.Request.SignaturePassword,
            passwordHasher);
        if (signatureError is not null)
        {
            return Result<MedicalAttendanceResponse>.Failure(signatureError);
        }

        MedicalAttendance? attendance = await attendances.GetByIdAsync(request.Id, cancellationToken);
        if (attendance is null)
        {
            return Result<MedicalAttendanceResponse>.Failure(AppError.NotFound("Ficha de atendimento nao encontrada."));
        }

        UpdateMedicalAttendanceModel model = UpdateMedicalAttendanceModel.FromRequest(request.Request);
        model = MedicalAttendancePermissionGuard.PreserveNonDispensationSectionsForDispensationActor(
            actor!.Role,
            attendance,
            model);

        AppError? permissionError = MedicalAttendancePermissionGuard.EnsureUpdatePermissions(
            actor.Role,
            attendance,
            model);
        if (permissionError is not null)
        {
            return Result<MedicalAttendanceResponse>.Failure(permissionError);
        }

        IReadOnlyList<AppError> errors = model.Validate();
        if (errors.HasErrors())
        {
            return Result<MedicalAttendanceResponse>.Failure(errors.FirstOrDefaultError());
        }

        Result<UpdateMedicalAttendanceModel> resolvedCid10 =
            await Cid10SelectionResolver.ResolveUpdateAsync(model, cid10Catalog, cancellationToken);
        if (!resolvedCid10.IsSuccess || resolvedCid10.Value is null)
        {
            return Result<MedicalAttendanceResponse>.Failure(resolvedCid10.Error!);
        }

        model = resolvedCid10.Value;

        return await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                Result<IReadOnlyList<DispensationItemModel>> dispensations =
                    await PrepareDispensationsAsync(attendance, model, actor, ct);
                if (!dispensations.IsSuccess || dispensations.Value is null)
                {
                    return Result<MedicalAttendanceResponse>.Failure(dispensations.Error!);
                }

                model = model with { Dispensations = dispensations.Value };
                model.ApplyTo(attendance);
                MedicalAttendanceStageSigner.Sign(attendance, model, actor);
                await CloseAppointmentAfterFinalDispensationAsync(attendance, model, actor, ct);

                return Result<MedicalAttendanceResponse>.Success(MedicalAttendanceModel.FromDomain(attendance));
            },
            cancellationToken);
    }

    private async Task<Result<IReadOnlyList<DispensationItemModel>>> PrepareDispensationsAsync(
        MedicalAttendance attendance,
        UpdateMedicalAttendanceModel model,
        User actor,
        CancellationToken cancellationToken)
    {
        var prepared = new List<DispensationItemModel>();

        foreach (DispensationItemModel dispensation in model.Dispensations)
        {
            if (!dispensation.PrescriptionId.HasValue)
            {
                prepared.Add(dispensation);
                continue;
            }

            if (attendance.Dispensations.Any(item => item.PrescriptionId == dispensation.PrescriptionId))
            {
                prepared.Add(dispensation);
                continue;
            }

            if (!dispensation.MedicationId.HasValue)
            {
                return Result<IReadOnlyList<DispensationItemModel>>.Failure(
                    AppError.Validation("Lote da dispensacao e obrigatorio."));
            }

            MedicalAttendancePrescriptionItem? prescription = attendance.Prescriptions
                .FirstOrDefault(item => item.Id == dispensation.PrescriptionId.Value);
            if (prescription is null)
            {
                return Result<IReadOnlyList<DispensationItemModel>>.Failure(
                    AppError.Validation("Prescricao da ficha nao encontrada."));
            }

            int quantity = dispensation.Quantity ?? prescription.Quantity ?? 0;
            if (quantity <= 0)
            {
                return Result<IReadOnlyList<DispensationItemModel>>.Failure(
                    AppError.Validation("Quantidade da dispensacao deve ser maior que zero."));
            }

            Medication? medication = await medications.GetByIdAsync(
                dispensation.MedicationId.Value,
                cancellationToken);
            if (medication is null)
            {
                return Result<IReadOnlyList<DispensationItemModel>>.Failure(
                    AppError.NotFound("Lote selecionado nao encontrado."));
            }

            if (!IsCompatibleLot(prescription, medication))
            {
                return Result<IReadOnlyList<DispensationItemModel>>.Failure(
                    AppError.Validation("Lote selecionado nao corresponde ao medicamento prescrito."));
            }

            if (medication.ExpirationDate.HasValue &&
                medication.ExpirationDate.Value < DateOnly.FromDateTime(DateTime.Today))
            {
                return Result<IReadOnlyList<DispensationItemModel>>.Failure(
                    AppError.Validation("Lote selecionado esta vencido."));
            }

            if (medication.Quantity < quantity)
            {
                return Result<IReadOnlyList<DispensationItemModel>>.Failure(
                    AppError.Validation("Quantidade insuficiente no lote selecionado."));
            }

            bool stockReduced = await medications.TryReduceQuantityAsync(
                medication.Id,
                quantity,
                cancellationToken);
            if (!stockReduced)
            {
                return Result<IReadOnlyList<DispensationItemModel>>.Failure(
                    AppError.Validation("Quantidade insuficiente no lote selecionado."));
            }

            string responsible = string.IsNullOrWhiteSpace(actor.Name) ? "Sistema" : actor.Name.Trim();
            DateTimeOffset dispensedAt = DateTimeOffset.UtcNow;
            string medicationName = prescription.MedicationName ??
                medication.GenericName ??
                medication.CommercialName ??
                $"Medicamento {medication.Id}";

            await movements.AddAsync(
                StockMovement.Create(
                    "saida",
                    medication.Id,
                    quantity,
                    DateOnly.FromDateTime(DateTime.Today),
                    responsible,
                    $"Ficha #{attendance.Id} | Prescricao #{prescription.Id}",
                    medication.Batch,
                    "Dispensacao",
                    attendance.Id,
                    attendance.AppointmentId,
                    prescription.Id),
                cancellationToken);

            prepared.Add(dispensation with
            {
                Batch = medication.Batch,
                MedicationId = medication.Id,
                MedicationName = medicationName,
                Quantity = quantity,
                Responsible = responsible,
                DispensedAt = dispensedAt
            });
        }

        return Result<IReadOnlyList<DispensationItemModel>>.Success(prepared);
    }

    private async Task CloseAppointmentAfterFinalDispensationAsync(
        MedicalAttendance attendance,
        UpdateMedicalAttendanceModel model,
        User actor,
        CancellationToken cancellationToken)
    {
        if (model.Dispensations.All(item => !item.PrescriptionId.HasValue) ||
            HasPendingDispensation(attendance))
        {
            return;
        }

        CareAppointment? appointment = await appointments.GetByIdAsync(attendance.AppointmentId, cancellationToken);
        if (appointment is null ||
            appointment.Status == AppointmentStatus.Closed ||
            appointment.Status == AppointmentStatus.Cancelled)
        {
            return;
        }

        appointment.ChangeStatus(AppointmentStatus.Closed, actor.Name);
    }

    private static bool HasPendingDispensation(MedicalAttendance attendance)
    {
        MedicalAttendancePrescriptionItem[] linkedPrescriptions = attendance.Prescriptions
            .Where(item => item.Id > 0)
            .ToArray();

        return linkedPrescriptions.Length > 0 &&
            linkedPrescriptions.Any(prescription =>
                attendance.Dispensations.All(dispensation => dispensation.PrescriptionId != prescription.Id));
    }

    private static bool IsCompatibleLot(
        MedicalAttendancePrescriptionItem prescription,
        Medication medication)
    {
        bool sameName = Same(prescription.MedicationName, medication.GenericName) ||
            Same(prescription.MedicationName, medication.CommercialName) ||
            prescription.MedicationId == medication.Id ||
            Same(prescription.Description, medication.GenericName) ||
            Same(prescription.Description, medication.CommercialName);

        return sameName &&
            Compatible(prescription.Dosage, medication.Dosage);
    }

    private static bool Compatible(string? selectedValue, string? lotValue)
    {
        return string.IsNullOrWhiteSpace(selectedValue) ||
            string.Equals(selectedValue.Trim(), lotValue?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool Same(string? left, string? right)
    {
        return !string.IsNullOrWhiteSpace(left) &&
            string.Equals(left.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

internal static class MedicalAttendanceStageSigner
{
    public static void Sign(
        MedicalAttendance attendance,
        CreateMedicalAttendanceModel model,
        User actor)
    {
        Sign(
            attendance,
            actor,
            actor.Name,
            HasTriageContent(
                model.VitalSigns,
                model.ChiefComplaint,
                model.PreviousPathologicalHistory,
                model.CurrentDiseaseHistory,
                model.NursingChecks),
            HasMedicalContent(
                model.PhysicalExam,
                model.DiagnosticHypothesis,
                model.Cid10Code,
                model.Cid10Codes,
                model.Prescriptions),
            model.Dispensations.Count > 0);
    }

    public static void Sign(
        MedicalAttendance attendance,
        UpdateMedicalAttendanceModel model,
        User actor)
    {
        Sign(
            attendance,
            actor,
            actor.Name,
            HasTriageContent(
                model.VitalSigns,
                model.ChiefComplaint,
                model.PreviousPathologicalHistory,
                model.CurrentDiseaseHistory,
                model.NursingChecks),
            HasMedicalContent(
                model.PhysicalExam,
                model.DiagnosticHypothesis,
                model.Cid10Code,
                model.Cid10Codes,
                model.Prescriptions),
            model.Dispensations.Count > 0);
    }

    private static void Sign(
        MedicalAttendance attendance,
        User actor,
        string? signature,
        bool hasTriageContent,
        bool hasMedicalContent,
        bool hasDispensationContent)
    {
        if ((actor.Role == UserRole.Farmaceutico ||
             actor.Role == UserRole.Movimentacao ||
             actor.Role == UserRole.Saida) &&
            hasDispensationContent)
        {
            attendance.SignDispensation(actor.Id, actor.Name, signature);
            return;
        }

        if (actor.Role == UserRole.Medico && hasMedicalContent)
        {
            attendance.SignMedical(actor.Id, actor.Name, signature);
            return;
        }

        if (actor.Role == UserRole.Enfermeira && hasTriageContent)
        {
            attendance.SignTriage(actor.Id, actor.Name, signature);
            return;
        }

        if ((actor.Role == UserRole.Admin || actor.Role == UserRole.Gerente) && hasMedicalContent)
        {
            attendance.SignMedical(actor.Id, actor.Name, signature);
            return;
        }

        if ((actor.Role == UserRole.Admin || actor.Role == UserRole.Gerente) && hasDispensationContent)
        {
            attendance.SignDispensation(actor.Id, actor.Name, signature);
            return;
        }

        if (hasTriageContent)
        {
            attendance.SignTriage(actor.Id, actor.Name, signature);
        }
    }

    private static bool HasTriageContent(
        VitalSignsModel vitalSigns,
        string? chiefComplaint,
        string? previousPathologicalHistory,
        string? currentDiseaseHistory,
        IReadOnlyList<NursingCheckItemModel> nursingChecks)
    {
        return vitalSigns.SystolicPressure.HasValue ||
            vitalSigns.DiastolicPressure.HasValue ||
            vitalSigns.Temperature.HasValue ||
            vitalSigns.BloodGlucose.HasValue ||
            vitalSigns.OxygenSaturation.HasValue ||
            vitalSigns.HeartRate.HasValue ||
            !string.IsNullOrWhiteSpace(chiefComplaint) ||
            !string.IsNullOrWhiteSpace(previousPathologicalHistory) ||
            !string.IsNullOrWhiteSpace(currentDiseaseHistory) ||
            nursingChecks.Count > 0;
    }

    private static bool HasMedicalContent(
        string? physicalExam,
        string? diagnosticHypothesis,
        string? cid10Code,
        IReadOnlyList<Cid10ItemModel> cid10Codes,
        IReadOnlyList<PrescriptionItemModel> prescriptions)
    {
        return !string.IsNullOrWhiteSpace(physicalExam) ||
            !string.IsNullOrWhiteSpace(diagnosticHypothesis) ||
            !string.IsNullOrWhiteSpace(cid10Code) ||
            cid10Codes.Count > 0 ||
            prescriptions.Count > 0;
    }
}

internal static class Cid10SelectionResolver
{
    public static async Task<Result<CreateMedicalAttendanceModel>> ResolveCreateAsync(
        CreateMedicalAttendanceModel model,
        ICid10Catalog cid10Catalog,
        CancellationToken cancellationToken)
    {
        Result<Cid10Resolution> result = await ResolveAsync(
            model.Cid10Codes,
            model.Cid10Code,
            cid10Catalog,
            cancellationToken);

        return !result.IsSuccess || result.Value is null
            ? Result<CreateMedicalAttendanceModel>.Failure(result.Error!)
            : Result<CreateMedicalAttendanceModel>.Success(model with
            {
                Cid10Code = result.Value.Code,
                Cid10Name = result.Value.Name,
                Cid10Codes = result.Value.Items
            });
    }

    public static async Task<Result<UpdateMedicalAttendanceModel>> ResolveUpdateAsync(
        UpdateMedicalAttendanceModel model,
        ICid10Catalog cid10Catalog,
        CancellationToken cancellationToken)
    {
        Result<Cid10Resolution> result = await ResolveAsync(
            model.Cid10Codes,
            model.Cid10Code,
            cid10Catalog,
            cancellationToken);

        return !result.IsSuccess || result.Value is null
            ? Result<UpdateMedicalAttendanceModel>.Failure(result.Error!)
            : Result<UpdateMedicalAttendanceModel>.Success(model with
            {
                Cid10Code = result.Value.Code,
                Cid10Name = result.Value.Name,
                Cid10Codes = result.Value.Items
            });
    }

    private static async Task<Result<Cid10Resolution>> ResolveAsync(
        IReadOnlyList<Cid10ItemModel> requestedItems,
        string? legacyCode,
        ICid10Catalog cid10Catalog,
        CancellationToken cancellationToken)
    {
        if (requestedItems.Count == 0)
        {
            return await ResolveLegacyCodeAsync(legacyCode, cid10Catalog, cancellationToken);
        }

        long[] ids = requestedItems
            .Select(item => item.Cid10CodeId)
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        IReadOnlyList<Cid10Response> catalogItems = await cid10Catalog.GetByIdsAsync(ids, cancellationToken);
        var catalogById = catalogItems.ToDictionary(item => item.Id);

        if (catalogById.Count != ids.Length)
        {
            return Result<Cid10Resolution>.Failure(
                AppError.Validation("Um ou mais CIDs selecionados nao existem no catalogo."));
        }

        Cid10ItemModel[] normalizedItems = requestedItems
            .GroupBy(item => item.Cid10CodeId)
            .Select(group => group.OrderBy(item => item.Order).First())
            .OrderBy(item => item.Order)
            .Select((item, index) =>
            {
                Cid10Response catalogItem = catalogById[item.Cid10CodeId];
                return new Cid10ItemModel(
                    index + 1,
                    catalogItem.Id,
                    catalogItem.Code,
                    catalogItem.Name);
            })
            .ToArray();

        Cid10ItemModel? first = normalizedItems.FirstOrDefault();
        return Result<Cid10Resolution>.Success(new Cid10Resolution(
            first?.Code,
            first?.Name,
            normalizedItems));
    }

    private static async Task<Result<Cid10Resolution>> ResolveLegacyCodeAsync(
        string? legacyCode,
        ICid10Catalog cid10Catalog,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(legacyCode))
        {
            return Result<Cid10Resolution>.Success(new Cid10Resolution(null, null, []));
        }

        Cid10Response? catalogItem = await cid10Catalog.GetByCodeAsync(legacyCode, cancellationToken);
        if (catalogItem is null)
        {
            return Result<Cid10Resolution>.Failure(
                AppError.Validation("CID-10 informado nao existe no catalogo."));
        }

        var item = new Cid10ItemModel(
            1,
            catalogItem.Id,
            catalogItem.Code,
            catalogItem.Name);

        return Result<Cid10Resolution>.Success(new Cid10Resolution(
            catalogItem.Code,
            catalogItem.Name,
            [item]));
    }

    private sealed record Cid10Resolution(
        string? Code,
        string? Name,
        IReadOnlyList<Cid10ItemModel> Items);
}

internal static class MedicalAttendancePermissionGuard
{
    public static AppError? ValidateClinicalActor(User? actor)
    {
        if (actor is null || !actor.CanAuthenticate)
        {
            return AppError.Forbidden("Usuario sem acesso clinico ativo.");
        }

        return null;
    }

    public static AppError? ValidateSignaturePassword(
        User actor,
        string? signaturePassword,
        IPasswordHasher passwordHasher)
    {
        if (actor.SignaturePasswordResetRequired || string.IsNullOrWhiteSpace(actor.SignaturePasswordHash))
        {
            return AppError.Forbidden("Senha de assinatura pendente de cadastro.");
        }

        if (string.IsNullOrWhiteSpace(signaturePassword))
        {
            return AppError.Validation("Senha de assinatura e obrigatoria.");
        }

        return passwordHasher.VerifyHash(actor.SignaturePasswordHash, signaturePassword)
            ? null
            : AppError.Forbidden("Senha de assinatura incorreta.");
    }

    public static AppError? EnsureCreatePermissions(UserRole role, CreateMedicalAttendanceModel model)
    {
        AppError? general = Ensure(role, MedicalAttendanceSection.InitialData);
        if (general is not null)
        {
            return general;
        }

        foreach (MedicalAttendanceSection section in SectionsPresentOnCreate(model))
        {
            AppError? error = Ensure(role, section);
            if (error is not null)
            {
                return error;
            }
        }

        return null;
    }

    public static AppError? EnsureUpdatePermissions(
        UserRole role,
        MedicalAttendance current,
        UpdateMedicalAttendanceModel model)
    {
        foreach (MedicalAttendanceSection section in ChangedSections(current, model))
        {
            AppError? error = Ensure(role, section);
            if (error is not null)
            {
                return error;
            }
        }

        return null;
    }

    public static UpdateMedicalAttendanceModel PreserveNonDispensationSectionsForDispensationActor(
        UserRole role,
        MedicalAttendance current,
        UpdateMedicalAttendanceModel model)
    {
        if (!IsDispensationActor(role))
        {
            return model;
        }

        return model with
        {
            VitalSigns = FromDomain(current.VitalSigns),
            ChiefComplaint = current.ChiefComplaint,
            PreviousPathologicalHistory = current.PreviousPathologicalHistory,
            CurrentDiseaseHistory = current.CurrentDiseaseHistory,
            PhysicalExam = current.PhysicalExam,
            DiagnosticHypothesis = current.DiagnosticHypothesis,
            Cid10Code = current.Cid10Code,
            Cid10Name = current.Cid10Name,
            Cid10Codes = current.Cid10Codes
                .OrderBy(item => item.Order)
                .Select(item => new Cid10ItemModel(
                    item.Order,
                    item.Cid10CodeId,
                    item.Code,
                    item.Name))
                .ToArray(),
            Prescriptions = current.Prescriptions
                .OrderBy(item => item.Order)
                .Select(item => new PrescriptionItemModel(
                    item.Order,
                    item.Description,
                    item.MedicationId,
                    item.MedicationName,
                    item.Dosage,
                    item.Directions,
                    item.Quantity))
                .ToArray(),
            NursingChecks = current.NursingChecks
                .OrderBy(item => item.Order)
                .Select(item => new NursingCheckItemModel(item.Order, item.Description))
                .ToArray(),
            ResponsibleSignature = current.ResponsibleSignature,
            HasBackSide = current.HasBackSide
        };
    }

    private static bool IsDispensationActor(UserRole role)
    {
        return role == UserRole.Farmaceutico ||
            role == UserRole.Movimentacao ||
            role == UserRole.Saida;
    }

    private static VitalSignsModel FromDomain(VitalSigns vitalSigns)
    {
        return new VitalSignsModel(
            vitalSigns.SystolicPressure,
            vitalSigns.DiastolicPressure,
            vitalSigns.Temperature,
            vitalSigns.BloodGlucose,
            vitalSigns.OxygenSaturation,
            vitalSigns.HeartRate);
    }

    private static IEnumerable<MedicalAttendanceSection> SectionsPresentOnCreate(
        CreateMedicalAttendanceModel model)
    {
        yield return MedicalAttendanceSection.VitalSigns;

        if (HasText(model.ChiefComplaint) ||
            HasText(model.PreviousPathologicalHistory) ||
            HasText(model.CurrentDiseaseHistory))
        {
            yield return MedicalAttendanceSection.ClinicalHistory;
        }

        if (HasText(model.PhysicalExam) ||
            HasText(model.DiagnosticHypothesis) ||
            HasText(model.Cid10Code) ||
            HasText(model.Cid10Name) ||
            model.Cid10Codes.Count > 0)
        {
            yield return MedicalAttendanceSection.PhysicalExam;
        }

        if (model.Prescriptions.Count > 0)
        {
            yield return MedicalAttendanceSection.Prescription;
        }

        if (model.NursingChecks.Count > 0)
        {
            yield return MedicalAttendanceSection.NursingCheck;
        }

        if (model.Dispensations.Count > 0)
        {
            yield return MedicalAttendanceSection.Dispensation;
        }
    }

    private static IEnumerable<MedicalAttendanceSection> ChangedSections(
        MedicalAttendance current,
        UpdateMedicalAttendanceModel model)
    {
        if (VitalSignsChanged(current.VitalSigns, model.VitalSigns))
        {
            yield return MedicalAttendanceSection.VitalSigns;
        }

        if (!Same(current.ChiefComplaint, model.ChiefComplaint) ||
            !Same(current.PreviousPathologicalHistory, model.PreviousPathologicalHistory) ||
            !Same(current.CurrentDiseaseHistory, model.CurrentDiseaseHistory))
        {
            yield return MedicalAttendanceSection.ClinicalHistory;
        }

        if (!Same(current.PhysicalExam, model.PhysicalExam) ||
            !Same(current.DiagnosticHypothesis, model.DiagnosticHypothesis) ||
            !Same(current.Cid10Code, model.Cid10Code) ||
            !Same(current.Cid10Name, model.Cid10Name) ||
            !SameCid10Codes(current.Cid10Codes, model.Cid10Codes))
        {
            yield return MedicalAttendanceSection.PhysicalExam;
        }

        if (!SamePrescriptions(current.Prescriptions, model.Prescriptions))
        {
            yield return MedicalAttendanceSection.Prescription;
        }

        if (!SameNursingChecks(current.NursingChecks, model.NursingChecks))
        {
            yield return MedicalAttendanceSection.NursingCheck;
        }

        if (!SameDispensations(current.Dispensations, model.Dispensations))
        {
            yield return MedicalAttendanceSection.Dispensation;
        }

        if (!Same(current.ResponsibleSignature, model.ResponsibleSignature) ||
            current.HasBackSide != model.HasBackSide)
        {
            yield return MedicalAttendanceSection.InitialData;
        }
    }

    private static AppError? Ensure(UserRole role, MedicalAttendanceSection section)
    {
        return MedicalAttendancePermissionPolicy.CanEdit(role, section)
            ? null
            : AppError.Forbidden($"Role '{role.Value}' nao pode editar a secao '{section}'.");
    }

    private static bool VitalSignsChanged(VitalSigns current, VitalSignsModel model)
    {
        return current.SystolicPressure != model.SystolicPressure ||
            current.DiastolicPressure != model.DiastolicPressure ||
            current.Temperature != model.Temperature ||
            current.BloodGlucose != model.BloodGlucose ||
            current.OxygenSaturation != model.OxygenSaturation ||
            current.HeartRate != model.HeartRate;
    }

    private static bool SamePrescriptions(
        IReadOnlyCollection<MedicalAttendancePrescriptionItem> current,
        IReadOnlyList<PrescriptionItemModel> next)
    {
        var currentItems = current.OrderBy(item => item.Order).ToArray();
        var nextItems = next.OrderBy(item => item.Order).ToArray();
        return currentItems.Length == nextItems.Length &&
            currentItems.Zip(nextItems).All(pair =>
                pair.First.Order == pair.Second.Order &&
                Same(pair.First.Description, pair.Second.Description) &&
                pair.First.MedicationId == pair.Second.MedicationId &&
                Same(pair.First.MedicationName, pair.Second.MedicationName) &&
                Same(pair.First.Dosage, pair.Second.Dosage) &&
                Same(pair.First.Directions, pair.Second.Directions) &&
                pair.First.Quantity == pair.Second.Quantity);
    }

    private static bool SameNursingChecks(
        IReadOnlyCollection<MedicalAttendanceNursingCheckItem> current,
        IReadOnlyList<NursingCheckItemModel> next)
    {
        var currentItems = current.OrderBy(item => item.Order).ToArray();
        var nextItems = next.OrderBy(item => item.Order).ToArray();
        return currentItems.Length == nextItems.Length &&
            currentItems.Zip(nextItems).All(pair =>
                pair.First.Order == pair.Second.Order &&
                Same(pair.First.Description, pair.Second.Description));
    }

    private static bool SameDispensations(
        IReadOnlyCollection<MedicalAttendanceDispensationItem> current,
        IReadOnlyList<DispensationItemModel> next)
    {
        var currentItems = current.OrderBy(item => item.Order).ToArray();
        var nextItems = next.OrderBy(item => item.Order).ToArray();
        return currentItems.Length == nextItems.Length &&
            currentItems.Zip(nextItems).All(pair =>
                pair.First.Order == pair.Second.Order &&
                Same(pair.First.Batch, pair.Second.Batch) &&
                pair.First.PrescriptionId == pair.Second.PrescriptionId &&
                pair.First.MedicationId == pair.Second.MedicationId &&
                Same(pair.First.MedicationName, pair.Second.MedicationName) &&
                pair.First.Quantity == pair.Second.Quantity &&
                Same(pair.First.Responsible, pair.Second.Responsible));
    }

    private static bool SameCid10Codes(
        IReadOnlyCollection<MedicalAttendanceCid10Item> current,
        IReadOnlyList<Cid10ItemModel> next)
    {
        var currentItems = current.OrderBy(item => item.Order).ToArray();
        var nextItems = next.OrderBy(item => item.Order).ToArray();
        return currentItems.Length == nextItems.Length &&
            currentItems.Zip(nextItems).All(pair =>
                pair.First.Order == pair.Second.Order &&
                pair.First.Cid10CodeId == pair.Second.Cid10CodeId &&
                Same(pair.First.Code, pair.Second.Code) &&
                Same(pair.First.Name, pair.Second.Name));
    }

    private static bool Same(string? current, string? next)
    {
        return string.Equals(current?.Trim(), next?.Trim(), StringComparison.Ordinal);
    }

    private static bool HasText(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
}
