using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.MedicalAttendances.Abstractions;
using FarmaControl.Application.Care.MedicalRecords.Abstractions;
using FarmaControl.Application.Care.Patients.Abstractions;
using FarmaControl.Application.Care.Prescriptions.Abstractions;
using FarmaControl.Application.Care.Prescriptions.Models;
using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;
using FarmaControl.Domain.Inventory;

namespace FarmaControl.Application.Care.Prescriptions.UseCases;

public sealed record ListPrescriptionsRequest(long? MedicalRecordId, long? PatientId, bool? IsDispensed);

public sealed class ListPrescriptionsUseCase(IPrescriptionRepository prescriptions)
    : IUseCase<ListPrescriptionsRequest, IReadOnlyList<PrescriptionResponse>>
{
    public async Task<IReadOnlyList<PrescriptionResponse>> ExecuteAsync(
        ListPrescriptionsRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Prescription> result = await prescriptions.ListAsync(
            request.MedicalRecordId,
            request.PatientId,
            request.IsDispensed,
            cancellationToken);

        return result.Select(PrescriptionModel.FromDomain).ToArray();
    }
}

public sealed class CreatePrescriptionUseCase(
    IPrescriptionRepository prescriptions,
    IMedicalRecordRepository medicalRecords,
    IPatientRepository patients)
    : IUseCase<PrescriptionInputModel, Result<PrescriptionResponse>>
{
    public async Task<Result<PrescriptionResponse>> ExecuteAsync(
        PrescriptionInputModel request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<PrescriptionResponse>.Failure(errors.FirstOrDefaultError());
        }

        MedicalRecord? medicalRecord = await medicalRecords.GetByIdAsync(request.MedicalRecordId, cancellationToken);
        if (medicalRecord is null)
        {
            return Result<PrescriptionResponse>.Failure(AppError.NotFound("Prontuario nao encontrado."));
        }

        Patient? patient = await patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return Result<PrescriptionResponse>.Failure(AppError.NotFound("Paciente nao encontrado."));
        }

        Prescription prescription = request.ToDomain();
        await prescriptions.AddAsync(prescription, cancellationToken);
        await prescriptions.SaveChangesAsync(cancellationToken);

        return Result<PrescriptionResponse>.Success(PrescriptionModel.FromDomain(prescription));
    }
}

public sealed record DeletePrescriptionCommand(long Id);

public sealed class DeletePrescriptionUseCase(IPrescriptionRepository prescriptions)
    : IUseCase<DeletePrescriptionCommand, Result<bool>>
{
    public async Task<Result<bool>> ExecuteAsync(
        DeletePrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        Prescription? prescription = await prescriptions.GetByIdAsync(request.Id, cancellationToken);
        if (prescription is null)
        {
            return Result<bool>.Failure(AppError.NotFound("Prescricao nao encontrada."));
        }

        prescriptions.Remove(prescription);
        await prescriptions.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

public sealed class DispensePrescriptionUseCase(
    IPrescriptionRepository prescriptions,
    IMedicationRepository medications,
    IStockMovementRepository movements,
    IMedicalRecordRepository medicalRecords,
    IMedicalAttendanceRepository medicalAttendances,
    IUnitOfWork unitOfWork)
    : IUseCase<DispensePrescriptionModel, Result<DispensePrescriptionResponse>>
{
    public async Task<Result<DispensePrescriptionResponse>> ExecuteAsync(
        DispensePrescriptionModel request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<DispensePrescriptionResponse>.Failure(errors.FirstOrDefaultError());
        }

        Prescription? prescription = await prescriptions.GetByIdAsync(request.PrescriptionId, cancellationToken);
        if (prescription is null)
        {
            return Result<DispensePrescriptionResponse>.Failure(AppError.NotFound("Prescricao nao encontrada."));
        }

        if (prescription.IsDispensed)
        {
            return Result<DispensePrescriptionResponse>.Failure(AppError.Validation("Prescricao ja dispensada."));
        }

        long? templateMedicationId = prescription.MedicationId ?? request.MedicationId;
        if (!templateMedicationId.HasValue)
        {
            return Result<DispensePrescriptionResponse>.Failure(AppError.Validation("Medicamento nao vinculado ao estoque."));
        }

        MedicalRecord? medicalRecord = await medicalRecords.GetByIdAsync(
            prescription.MedicalRecordId,
            cancellationToken);
        if (medicalRecord is null)
        {
            return Result<DispensePrescriptionResponse>.Failure(AppError.NotFound("Prontuario da prescricao nao encontrado."));
        }

        MedicalAttendance? medicalAttendance = await medicalAttendances.GetByAppointmentIdAsync(
            medicalRecord.AppointmentId,
            cancellationToken);

        Medication? selectedMedication = await medications.GetByIdAsync(templateMedicationId.Value, cancellationToken);
        if (selectedMedication is null)
        {
            return Result<DispensePrescriptionResponse>.Failure(AppError.NotFound("Medicamento nao encontrado no estoque."));
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        Medication? medication = null;
        if (request.MedicationId.HasValue)
        {
            Medication? requestedLot = await medications.GetByIdAsync(request.MedicationId.Value, cancellationToken);
            if (requestedLot is null)
            {
                return Result<DispensePrescriptionResponse>.Failure(AppError.NotFound("Lote selecionado nao encontrado."));
            }

            if (!IsCompatibleLot(selectedMedication, requestedLot))
            {
                return Result<DispensePrescriptionResponse>.Failure(
                    AppError.Validation("Lote selecionado nao corresponde ao medicamento prescrito."));
            }

            if (requestedLot.ExpirationDate.HasValue && requestedLot.ExpirationDate.Value < today)
            {
                return Result<DispensePrescriptionResponse>.Failure(AppError.Validation("Lote selecionado esta vencido."));
            }

            medication = requestedLot.Quantity >= prescription.Quantity ? requestedLot : null;
        }
        else
        {
            IReadOnlyList<Medication> medicationLots = await medications.ListAsync(cancellationToken);
            medication = SelectPreferredLot(
                selectedMedication,
                medicationLots,
                prescription.Quantity,
                today);
        }

        if (medication is null)
        {
            return Result<DispensePrescriptionResponse>.Failure(
                AppError.Validation("Nenhum lote valido possui estoque suficiente para dispensacao."));
        }

        DateTimeOffset dispensedAt = DateTimeOffset.UtcNow;
        string responsible = string.IsNullOrWhiteSpace(request.Responsible) ? "Sistema" : request.Responsible.Trim();

        Result<StockMovement> movementResult = await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                bool reduced = await medications.TryReduceQuantityAsync(
                    medication.Id,
                    prescription.Quantity,
                    ct);
                if (!reduced)
                {
                    return Result<StockMovement>.Failure(
                        AppError.Validation("Quantidade insuficiente em estoque."));
                }

                StockMovement movement = StockMovement.Create(
                    "saida",
                    medication.Id,
                    prescription.Quantity,
                    DateOnly.FromDateTime(DateTime.Today),
                    responsible,
                    $"Prescricao #{prescription.Id}",
                    medication.Batch,
                    "Dispensacao",
                    medicalAttendance?.Id,
                    medicalRecord.AppointmentId,
                    prescription.Id);

                await movements.AddAsync(movement, ct);
                prescription.MarkDispensed(dispensedAt);
                medicalAttendance?.AttachPrescriptionDispensation(
                        prescription.Id,
                        medication.Id,
                        prescription.MedicationName ?? medication.GenericName ?? medication.CommercialName,
                        medication.Batch,
                        prescription.Quantity,
                        responsible,
                        dispensedAt);

                return Result<StockMovement>.Success(movement);
            },
            cancellationToken);

        if (!movementResult.IsSuccess || movementResult.Value is null)
        {
            return Result<DispensePrescriptionResponse>.Failure(movementResult.Error!);
        }

        return Result<DispensePrescriptionResponse>.Success(
            new DispensePrescriptionResponse(
                true,
                prescription.Id,
                medication.Id,
                movementResult.Value.Id,
                medicalAttendance?.Id,
                medication.Batch,
                medication.ExpirationDate));
    }

    private static Medication? SelectPreferredLot(
        Medication selectedMedication,
        IReadOnlyList<Medication> lots,
        int quantity,
        DateOnly today)
    {
        return lots
            .Where(lot => IsCompatibleLot(selectedMedication, lot))
            .Where(lot => lot.Quantity >= quantity)
            .Where(lot => !lot.ExpirationDate.HasValue || lot.ExpirationDate.Value >= today)
            .OrderBy(lot => lot.ExpirationDate ?? DateOnly.MaxValue)
            .ThenBy(lot => lot.Id)
            .FirstOrDefault();
    }

    private static bool IsCompatibleLot(Medication selectedMedication, Medication lot)
    {
        bool sameName = Same(selectedMedication.GenericName, lot.GenericName) ||
            Same(selectedMedication.CommercialName, lot.CommercialName) ||
            selectedMedication.Id == lot.Id;

        return sameName &&
            Compatible(selectedMedication.Dosage, lot.Dosage) &&
            Compatible(selectedMedication.PharmaceuticalForm, lot.PharmaceuticalForm);
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
