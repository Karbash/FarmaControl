using System.Reflection;
using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.Cid10;
using FarmaControl.Application.Care.MedicalAttendances;
using FarmaControl.Application.Care.Appointments.Abstractions;
using FarmaControl.Application.Care.MedicalAttendances.Abstractions;
using FarmaControl.Application.Care.MedicalAttendances.UseCases;
using FarmaControl.Application.Care.Patients.Abstractions;
using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;
using FarmaControl.Domain.Inventory;
using FarmaControl.Domain.Users;

namespace FarmaControl.Tests.Application;

public sealed class MedicalAttendanceUseCaseTests
{
    [Fact]
    public async Task Create_WithPhysician_RegistersResponsibleUser()
    {
        User physician = CreateUser(5, UserRole.Medico);
        var attendances = new FakeMedicalAttendanceRepository();
        var useCase = new CreateMedicalAttendanceUseCase(
            attendances,
            new FakeAppointmentRepository(CareAppointment.Create(1, Today(), null, "consulta", false, null, null)),
            new FakePatientRepository(Patient.Create("Paciente", null, null, null, null, null, null, [])),
            new FakeUserRepository(physician),
            new FakePasswordHasher(),
            new FakeCid10Catalog(),
            new FakeUnitOfWork());

        Result<MedicalAttendanceResponse> result = await useCase.ExecuteAsync(
            new CreateMedicalAttendanceCommand(1, physician.Id, ValidCreateRequest() with
            {
                PhysicalExam = "Exame fisico",
                Prescriptions = [new MedicalAttendancePrescriptionItemRequest(1, "Prescricao")]
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(attendances.Items);
        Assert.Equal(physician.Id, result.Value!.ResponsibleUserId);
        Assert.Equal(physician.Name, result.Value.ResponsibleName);
    }

    [Fact]
    public async Task Create_WithNursePrescription_ReturnsForbidden()
    {
        User nurse = CreateUser(6, UserRole.Enfermeira);
        var useCase = new CreateMedicalAttendanceUseCase(
            new FakeMedicalAttendanceRepository(),
            new FakeAppointmentRepository(CareAppointment.Create(1, Today(), null, "consulta", false, null, null)),
            new FakePatientRepository(Patient.Create("Paciente", null, null, null, null, null, null, [])),
            new FakeUserRepository(nurse),
            new FakePasswordHasher(),
            new FakeCid10Catalog(),
            new FakeUnitOfWork());

        Result<MedicalAttendanceResponse> result = await useCase.ExecuteAsync(
            new CreateMedicalAttendanceCommand(1, nurse.Id, ValidCreateRequest() with
            {
                Prescriptions = [new MedicalAttendancePrescriptionItemRequest(1, "Prescricao")]
            }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.Error!.Code);
    }

    [Fact]
    public async Task Create_WithPendingSignaturePassword_ReturnsForbidden()
    {
        User physician = User.Create("Profissional", "pendente@teste", "hash", UserRole.Medico);
        SetId(physician, 8);
        var useCase = new CreateMedicalAttendanceUseCase(
            new FakeMedicalAttendanceRepository(),
            new FakeAppointmentRepository(CareAppointment.Create(1, Today(), null, "consulta", false, null, null)),
            new FakePatientRepository(Patient.Create("Paciente", null, null, null, null, null, null, [])),
            new FakeUserRepository(physician),
            new FakePasswordHasher(),
            new FakeCid10Catalog(),
            new FakeUnitOfWork());

        Result<MedicalAttendanceResponse> result = await useCase.ExecuteAsync(
            new CreateMedicalAttendanceCommand(1, physician.Id, ValidCreateRequest()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Senha de assinatura pendente de cadastro.", result.Error!.Message);
    }

    [Fact]
    public async Task Create_WithWrongSignaturePassword_ReturnsForbidden()
    {
        User physician = CreateUser(9, UserRole.Medico);
        var useCase = new CreateMedicalAttendanceUseCase(
            new FakeMedicalAttendanceRepository(),
            new FakeAppointmentRepository(CareAppointment.Create(1, Today(), null, "consulta", false, null, null)),
            new FakePatientRepository(Patient.Create("Paciente", null, null, null, null, null, null, [])),
            new FakeUserRepository(physician),
            new FakePasswordHasher(),
            new FakeCid10Catalog(),
            new FakeUnitOfWork());

        Result<MedicalAttendanceResponse> result = await useCase.ExecuteAsync(
            new CreateMedicalAttendanceCommand(1, physician.Id, ValidCreateRequest() with
            {
                SignaturePassword = "errada"
            }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Senha de assinatura incorreta.", result.Error!.Message);
    }

    [Fact]
    public async Task Create_WithCid10Id_NormalizesCodeAndNameFromCatalog()
    {
        User physician = CreateUser(10, UserRole.Medico);
        var useCase = new CreateMedicalAttendanceUseCase(
            new FakeMedicalAttendanceRepository(),
            new FakeAppointmentRepository(CareAppointment.Create(1, Today(), null, "consulta", false, null, null)),
            new FakePatientRepository(Patient.Create("Paciente", null, null, null, null, null, null, [])),
            new FakeUserRepository(physician),
            new FakePasswordHasher(),
            new FakeCid10Catalog(),
            new FakeUnitOfWork());

        Result<MedicalAttendanceResponse> result = await useCase.ExecuteAsync(
            new CreateMedicalAttendanceCommand(1, physician.Id, ValidCreateRequest() with
            {
                PhysicalExam = "Exame fisico",
                Cid10Codes = [new MedicalAttendanceCid10Request(1, 10, "ERR", "Texto adulterado")]
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        MedicalAttendanceCid10Response cid10 = Assert.Single(result.Value!.Cid10Codes);
        Assert.Equal("A09", cid10.Code);
        Assert.Equal("Diarreia e gastroenterite de origem infecciosa presumivel", cid10.Name);
        Assert.Equal("A09", result.Value.Cid10Code);
    }

    [Fact]
    public async Task Create_WithUnknownCid10Id_ReturnsValidation()
    {
        User physician = CreateUser(11, UserRole.Medico);
        var useCase = new CreateMedicalAttendanceUseCase(
            new FakeMedicalAttendanceRepository(),
            new FakeAppointmentRepository(CareAppointment.Create(1, Today(), null, "consulta", false, null, null)),
            new FakePatientRepository(Patient.Create("Paciente", null, null, null, null, null, null, [])),
            new FakeUserRepository(physician),
            new FakePasswordHasher(),
            new FakeCid10Catalog(),
            new FakeUnitOfWork());

        Result<MedicalAttendanceResponse> result = await useCase.ExecuteAsync(
            new CreateMedicalAttendanceCommand(1, physician.Id, ValidCreateRequest() with
            {
                PhysicalExam = "Exame fisico",
                Cid10Codes = [new MedicalAttendanceCid10Request(1, 999, "A09", "CID inexistente")]
            }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation", result.Error!.Code);
    }

    [Fact]
    public async Task Update_WithPharmacistDispensation_ReplacesList()
    {
        User pharmacist = CreateUser(7, UserRole.Farmaceutico);
        MedicalAttendance attendance = ExistingAttendance();
        var attendances = new FakeMedicalAttendanceRepository(attendance);
        var useCase = new UpdateMedicalAttendanceUseCase(
            attendances,
            new FakeAppointmentRepository(CareAppointment.Create(1, Today(), null, "consulta", false, null, null)),
            new FakeUserRepository(pharmacist),
            new FakePasswordHasher(),
            new FakeMedicationRepository([]),
            new FakeStockMovementRepository(),
            new FakeCid10Catalog(),
            new FakeUnitOfWork());

        Result<MedicalAttendanceResponse> result = await useCase.ExecuteAsync(
            new UpdateMedicalAttendanceCommand(attendance.Id, pharmacist.Id, ValidUpdateRequest() with
            {
                Dispensations = [new MedicalAttendanceDispensationItemRequest(1, "Lote B")]
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Dispensations);
        Assert.Equal("Lote B", result.Value.Dispensations[0].Batch);
    }

    [Fact]
    public async Task Update_WithLinkedDispensation_ReducesSelectedLotAndCreatesMovement()
    {
        User pharmacist = CreateUser(7, UserRole.Farmaceutico);
        MedicalAttendance attendance = ExistingAttendance();
        MedicalAttendancePrescriptionItem prescription =
            MedicalAttendancePrescriptionItem.Create(1, "Dipirona | Qtde 2", 10, "Dipirona", "500mg", "Tomar", 2);
        SetId(prescription, 20);
        attendance.ReplacePrescriptions([prescription]);

        Medication medication = CreateMedication(10, quantity: 5);
        CareAppointment appointment = CareAppointment.Create(1, Today(), null, "consulta", false, null, null);
        var movements = new FakeStockMovementRepository();
        var useCase = new UpdateMedicalAttendanceUseCase(
            new FakeMedicalAttendanceRepository(attendance),
            new FakeAppointmentRepository(appointment),
            new FakeUserRepository(pharmacist),
            new FakePasswordHasher(),
            new FakeMedicationRepository([medication]),
            movements,
            new FakeCid10Catalog(),
            new FakeUnitOfWork());

        Result<MedicalAttendanceResponse> result = await useCase.ExecuteAsync(
            new UpdateMedicalAttendanceCommand(attendance.Id, pharmacist.Id, ValidUpdateRequest() with
            {
                Prescriptions =
                [
                    new MedicalAttendancePrescriptionItemRequest(
                        1,
                        "Dipirona | Qtde 2",
                        10,
                        "Dipirona",
                        "500mg",
                        "Tomar",
                        2)
                ],
                Dispensations =
                [
                    new MedicalAttendanceDispensationItemRequest(
                        1,
                        null,
                        20,
                        10,
                        null,
                        2,
                        null,
                        null)
                ]
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, medication.Quantity);
        Assert.Single(movements.Items);
        Assert.Single(result.Value!.Dispensations);
        Assert.Equal("L1", result.Value.Dispensations[0].Batch);
        Assert.Equal("Profissional", result.Value.Dispensations[0].Responsible);
        Assert.Equal(AppointmentStatus.Closed, appointment.Status);
    }

    [Fact]
    public async Task Update_WithPharmacistDispensation_PreservesClinicalSections()
    {
        User pharmacist = CreateUser(7, UserRole.Farmaceutico);
        MedicalAttendance attendance = ExistingAttendance();
        attendance.UpdateClinicalHistory(
            "Queixa",
            "HPP",
            "HDA",
            "Exame fisico preenchido",
            "Hipotese diagnostica");

        MedicalAttendancePrescriptionItem prescription =
            MedicalAttendancePrescriptionItem.Create(1, "Dipirona | Qtde 2", 10, "Dipirona", "500mg", "Tomar", 2);
        SetId(prescription, 20);
        attendance.ReplacePrescriptions([prescription]);

        Medication medication = CreateMedication(10, quantity: 5);
        CareAppointment appointment = CareAppointment.Create(1, Today(), null, "consulta", false, null, null);
        var useCase = new UpdateMedicalAttendanceUseCase(
            new FakeMedicalAttendanceRepository(attendance),
            new FakeAppointmentRepository(appointment),
            new FakeUserRepository(pharmacist),
            new FakePasswordHasher(),
            new FakeMedicationRepository([medication]),
            new FakeStockMovementRepository(),
            new FakeCid10Catalog(),
            new FakeUnitOfWork());

        Result<MedicalAttendanceResponse> result = await useCase.ExecuteAsync(
            new UpdateMedicalAttendanceCommand(attendance.Id, pharmacist.Id, ValidUpdateRequest() with
            {
                PhysicalExam = null,
                DiagnosticHypothesis = null,
                Prescriptions = [],
                Dispensations =
                [
                    new MedicalAttendanceDispensationItemRequest(
                        1,
                        null,
                        20,
                        10,
                        null,
                        2,
                        null,
                        null)
                ]
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Exame fisico preenchido", result.Value!.PhysicalExam);
        Assert.Equal("Hipotese diagnostica", result.Value.DiagnosticHypothesis);
        Assert.Single(result.Value.Prescriptions);
        Assert.Single(result.Value.Dispensations);
    }

    [Fact]
    public async Task Update_WithPharmacistPayloadWithoutNewDispensation_IgnoresClinicalFields()
    {
        User pharmacist = CreateUser(7, UserRole.Farmaceutico);
        MedicalAttendance attendance = ExistingAttendance();
        attendance.UpdateClinicalHistory(
            "Queixa",
            "HPP",
            "HDA",
            "Exame fisico preenchido",
            "Hipotese diagnostica");

        var useCase = new UpdateMedicalAttendanceUseCase(
            new FakeMedicalAttendanceRepository(attendance),
            new FakeAppointmentRepository(CareAppointment.Create(1, Today(), null, "consulta", false, null, null)),
            new FakeUserRepository(pharmacist),
            new FakePasswordHasher(),
            new FakeMedicationRepository([]),
            new FakeStockMovementRepository(),
            new FakeCid10Catalog(),
            new FakeUnitOfWork());

        Result<MedicalAttendanceResponse> result = await useCase.ExecuteAsync(
            new UpdateMedicalAttendanceCommand(attendance.Id, pharmacist.Id, ValidUpdateRequest() with
            {
                PhysicalExam = null,
                DiagnosticHypothesis = null,
                Prescriptions = [],
                Dispensations = []
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Exame fisico preenchido", result.Value!.PhysicalExam);
        Assert.Equal("Hipotese diagnostica", result.Value.DiagnosticHypothesis);
    }

    [Fact]
    public async Task Update_WithRemainingPrescription_DoesNotCloseAppointment()
    {
        User pharmacist = CreateUser(12, UserRole.Farmaceutico);
        MedicalAttendance attendance = ExistingAttendance();
        MedicalAttendancePrescriptionItem firstPrescription =
            MedicalAttendancePrescriptionItem.Create(1, "Dipirona | Qtde 2", 10, "Dipirona", "500mg", "Tomar", 2);
        MedicalAttendancePrescriptionItem secondPrescription =
            MedicalAttendancePrescriptionItem.Create(2, "Dipirona | Qtde 1", 10, "Dipirona", "500mg", "Tomar", 1);
        SetId(firstPrescription, 20);
        SetId(secondPrescription, 21);
        attendance.ReplacePrescriptions([firstPrescription, secondPrescription]);

        Medication medication = CreateMedication(10, quantity: 5);
        CareAppointment appointment = CareAppointment.Create(1, Today(), null, "consulta", false, null, null);
        appointment.ChangeStatus(AppointmentStatus.Dispensation, "Medico");
        var movements = new FakeStockMovementRepository();
        var useCase = new UpdateMedicalAttendanceUseCase(
            new FakeMedicalAttendanceRepository(attendance),
            new FakeAppointmentRepository(appointment),
            new FakeUserRepository(pharmacist),
            new FakePasswordHasher(),
            new FakeMedicationRepository([medication]),
            movements,
            new FakeCid10Catalog(),
            new FakeUnitOfWork());

        Result<MedicalAttendanceResponse> result = await useCase.ExecuteAsync(
            new UpdateMedicalAttendanceCommand(attendance.Id, pharmacist.Id, ValidUpdateRequest() with
            {
                Prescriptions =
                [
                    new MedicalAttendancePrescriptionItemRequest(
                        1,
                        "Dipirona | Qtde 2",
                        10,
                        "Dipirona",
                        "500mg",
                        "Tomar",
                        2),
                    new MedicalAttendancePrescriptionItemRequest(
                        2,
                        "Dipirona | Qtde 1",
                        10,
                        "Dipirona",
                        "500mg",
                        "Tomar",
                        1)
                ],
                Dispensations =
                [
                    new MedicalAttendanceDispensationItemRequest(
                        1,
                        null,
                        20,
                        10,
                        null,
                        2,
                        null,
                        null)
                ]
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, medication.Quantity);
        Assert.Single(movements.Items);
        Assert.Single(result.Value!.Dispensations);
        Assert.Equal(AppointmentStatus.Dispensation, appointment.Status);
    }

    [Fact]
    public async Task GeneratePdf_CallsGeneratorAndReturnsPdfFile()
    {
        MedicalAttendance attendance = ExistingAttendance();
        var generator = new FakePdfGenerator();
        var useCase = new GenerateMedicalAttendancePdfUseCase(
            new FakeMedicalAttendanceRepository(attendance),
            generator);

        Result<MedicalAttendancePdfResponse> result = await useCase.ExecuteAsync(
            new GenerateMedicalAttendancePdfRequest(attendance.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(generator.WasCalled);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(result.Value!.Content));
        Assert.EndsWith(".pdf", result.Value.FileName);
    }

    private static CreateMedicalAttendanceRequest ValidCreateRequest()
    {
        return new CreateMedicalAttendanceRequest(
            1,
            null,
            null,
            "Paciente",
            35,
            Today(),
            new TimeOnly(9, 0),
            "Cidade",
            "Igreja",
            "Pastor",
            "Participante",
            null,
            new VitalSignsRequest(120, 80, 36.5m, 90m, 98, 72),
            "Queixa",
            "HPP",
            "HDA",
            null,
            [],
            [],
            [],
            null,
            false,
            "assinatura");
    }

    private static UpdateMedicalAttendanceRequest ValidUpdateRequest()
    {
        return new UpdateMedicalAttendanceRequest(
            new VitalSignsRequest(120, 80, 36.5m, 90m, 98, 72),
            "Queixa",
            "HPP",
            "HDA",
            null,
            [],
            [],
            [],
            null,
            false,
            "assinatura");
    }

    private static MedicalAttendance ExistingAttendance()
    {
        MedicalAttendance attendance = MedicalAttendance.Create(
            1,
            1,
            5,
            "Medico",
            "Paciente",
            35,
            Today(),
            null,
            null,
            null,
            null,
            AttendanceType.Participante,
            null);

        attendance.UpdateVitalSigns(VitalSigns.Create(120, 80, 36.5m, 90m, 98, 72));
        attendance.UpdateClinicalHistory("Queixa", "HPP", "HDA", null);
        SetId(attendance, 1);
        return attendance;
    }

    private static Medication CreateMedication(long id, int quantity)
    {
        Medication medication = Medication.Create(
            "Dipirona",
            null,
            null,
            null,
            "500mg",
            null,
            null,
            null,
            null,
            null,
            null,
            "L1",
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            quantity,
            "un",
            "Farmacia",
            1,
            1,
            false);

        SetId(medication, id);
        return medication;
    }

    private static User CreateUser(long id, UserRole role)
    {
        User user = User.Create("Profissional", $"user{id}@teste", "hash", role);
        user.ChangeSignaturePasswordHash("assinatura");
        SetId(user, id);
        return user;
    }

    private static DateOnly Today()
    {
        return DateOnly.FromDateTime(DateTime.Today);
    }

    private static void SetId(object entity, long id)
    {
        PropertyInfo property = entity.GetType().BaseType!.GetProperty("Id")!;
        property.SetValue(entity, id);
    }

    private sealed class FakeMedicalAttendanceRepository(params MedicalAttendance[] items)
        : IMedicalAttendanceRepository
    {
        public List<MedicalAttendance> Items { get; } = [.. items];

        public Task<MedicalAttendance?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(item => item.Id == id));

        public Task<MedicalAttendance?> GetByAppointmentIdAsync(long appointmentId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(item => item.AppointmentId == appointmentId));

        public Task<bool> ExistsForAppointmentAsync(long appointmentId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(item => item.AppointmentId == appointmentId));

        public Task AddAsync(MedicalAttendance attendance, CancellationToken cancellationToken)
        {
            SetId(attendance, Items.Count + 1);
            Items.Add(attendance);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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

    private sealed class FakeUserRepository(User user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult<User?>(user.Id == id ? user : null);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult<User?>(null);

        public Task<IReadOnlyList<User>> ListAsync(bool includeDeleted, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<User>>([user]);

        public Task<IReadOnlyList<User>> ListCareTeamAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<User>>([user]);

        public Task<IReadOnlyList<User>> ListResponsibleUsersAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<User>>([user]);

        public Task<bool> EmailExistsAsync(string email, long? exceptUserId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddAsync(User item, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeMedicationRepository(List<Medication> items) : IMedicationRepository
    {
        public Task<IReadOnlyList<Medication>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Medication>>(items);

        public Task<Medication?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(items.FirstOrDefault(item => item.Id == id));

        public Task<bool> TryReduceQuantityAsync(long id, int quantity, CancellationToken cancellationToken)
        {
            Medication? medication = items.FirstOrDefault(item => item.Id == id);
            if (medication is null || medication.Quantity < quantity)
            {
                return Task.FromResult(false);
            }

            medication.ReduceQuantity(quantity);
            return Task.FromResult(true);
        }

        public Task AddAsync(Medication medication, CancellationToken cancellationToken)
        {
            items.Add(medication);
            return Task.CompletedTask;
        }

        public void Remove(Medication medication) => items.Remove(medication);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeStockMovementRepository : IStockMovementRepository
    {
        public List<StockMovement> Items { get; } = [];

        public Task<IReadOnlyList<StockMovement>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StockMovement>>(Items);

        public Task AddAsync(StockMovement movement, CancellationToken cancellationToken)
        {
            SetId(movement, Items.Count + 1);
            Items.Add(movement);
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

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => password;

        public bool Verify(User user, string password) => user.PasswordHash == password;

        public bool VerifyHash(string passwordHash, string password) => passwordHash == password;
    }

    private sealed class FakeCid10Catalog : ICid10Catalog
    {
        private static readonly Cid10Response[] Items =
        [
            new(10, "A09", "Diarreia e gastroenterite de origem infecciosa presumivel")
        ];

        public Task<IReadOnlyList<Cid10Response>> SearchAsync(string? query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Cid10Response>>(Items);

        public Task<IReadOnlyList<Cid10Response>> GetByIdsAsync(
            IReadOnlyCollection<long> ids,
            CancellationToken cancellationToken)
        {
            Cid10Response[] matches = Items
                .Where(item => ids.Contains(item.Id))
                .ToArray();

            return Task.FromResult<IReadOnlyList<Cid10Response>>(matches);
        }

        public Task<Cid10Response?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(item =>
                string.Equals(item.Code, code.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    private sealed class FakePdfGenerator : IMedicalAttendancePdfGenerator
    {
        public bool WasCalled { get; private set; }

        public Task<byte[]> GenerateAsync(
            MedicalAttendance attendance,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(System.Text.Encoding.ASCII.GetBytes("%PDF-1.4"));
        }
    }
}
