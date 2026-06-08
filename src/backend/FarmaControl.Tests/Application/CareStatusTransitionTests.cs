using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.Appointments.Abstractions;
using FarmaControl.Application.Care.MedicalRecords.Abstractions;
using FarmaControl.Application.Care.MedicalRecords.Models;
using FarmaControl.Application.Care.MedicalRecords.UseCases;
using FarmaControl.Application.Care.Patients.Abstractions;
using FarmaControl.Application.Care.Triage.Abstractions;
using FarmaControl.Application.Care.Triage.Models;
using FarmaControl.Application.Care.Triage.UseCases;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Tests.Application;

public sealed class CareStatusTransitionTests
{
    [Fact]
    public async Task CreateTriage_WhenAppointmentWaiting_ChangesStatusToTriage()
    {
        CareAppointment appointment = CareAppointment.Create(1, DateOnly.FromDateTime(DateTime.Today), null, "consulta", false, null, null);
        var appointments = new FakeAppointmentRepository(appointment);
        var triages = new FakeTriageRecordRepository();
        var useCase = new CreateTriageRecordUseCase(triages, appointments, new FakeUnitOfWork());

        Result<TriageRecordResponse> result = await useCase.ExecuteAsync(
            new TriageRecordInputModel(1, "120x80", null, null, null, null, null, "Dor", "Enfermeira", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Triage, appointment.Status);
        Assert.Single(triages.Items);
    }

    [Fact]
    public async Task CreateMedicalRecord_ChangesAppointmentStatusToInCare()
    {
        CareAppointment appointment = CareAppointment.Create(1, DateOnly.FromDateTime(DateTime.Today), null, "consulta", false, null, null);
        var appointments = new FakeAppointmentRepository(appointment);
        var patients = new FakePatientRepository(Patient.Create("Paciente", null, null, null, null, null, null, []));
        var records = new FakeMedicalRecordRepository();
        var useCase = new CreateMedicalRecordUseCase(records, appointments, patients, new FakeUnitOfWork());

        Result<MedicalRecordResponse> result = await useCase.ExecuteAsync(
            new MedicalRecordInputModel(1, 1, "Dr Teste", "Anamnese", "Exame", null, "A09", "Conduta", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.InCare, appointment.Status);
        Assert.Equal("Dr Teste", appointment.DoctorName);
        Assert.Single(records.Items);
    }

    private sealed class FakeAppointmentRepository(CareAppointment appointment) : IAppointmentRepository
    {
        public Task<IReadOnlyList<CareAppointment>> ListAsync(DateOnly? date, string? status, long? patientId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CareAppointment>>([appointment]);

        public Task<CareAppointment?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult<CareAppointment?>(appointment);

        public Task AddAsync(CareAppointment item, CancellationToken cancellationToken) => Task.CompletedTask;

        public void Remove(CareAppointment item) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePatientRepository(Patient patient) : IPatientRepository
    {
        public Task<IReadOnlyList<Patient>> ListAsync(string? search, bool? isActive, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Patient>>([patient]);

        public Task<Patient?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult<Patient?>(patient);

        public Task AddAsync(Patient item, CancellationToken cancellationToken) => Task.CompletedTask;

        public void Remove(Patient item) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTriageRecordRepository : ITriageRecordRepository
    {
        public List<TriageRecord> Items { get; } = [];

        public Task<IReadOnlyList<TriageRecord>> ListByAppointmentAsync(long appointmentId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TriageRecord>>(Items);

        public Task<TriageRecord?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault());

        public Task AddAsync(TriageRecord triage, CancellationToken cancellationToken)
        {
            Items.Add(triage);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeMedicalRecordRepository : IMedicalRecordRepository
    {
        public List<MedicalRecord> Items { get; } = [];

        public Task<IReadOnlyList<MedicalRecord>> ListAsync(long? appointmentId, long? patientId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MedicalRecord>>(Items);

        public Task<MedicalRecord?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault());

        public Task AddAsync(MedicalRecord record, CancellationToken cancellationToken)
        {
            Items.Add(record);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken) =>
            action(cancellationToken);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken) =>
            action(cancellationToken);
    }
}
