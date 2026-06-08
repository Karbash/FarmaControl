using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmaControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<long>(type: "INTEGER", nullable: true),
                    user_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    action = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    entity = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    entity_id = table.Column<long>(type: "INTEGER", nullable: true),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "care_appointments",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    patient_id = table.Column<long>(type: "INTEGER", nullable: false),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    time = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    type = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    is_emergency = table.Column<bool>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    doctor_name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    responsible = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_care_appointments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cid10_codes",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    code = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cid10_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "donors",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "manufacturers",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    cnpj = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manufacturers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "medical_attendances",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    appointment_id = table.Column<long>(type: "INTEGER", nullable: false),
                    patient_id = table.Column<long>(type: "INTEGER", nullable: false),
                    responsible_user_id = table.Column<long>(type: "INTEGER", nullable: true),
                    responsible_name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    age = table.Column<int>(type: "INTEGER", nullable: true),
                    attendance_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    attendance_time = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    city = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    church = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    pastor = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    attendance_type = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    return_number = table.Column<int>(type: "INTEGER", nullable: true),
                    systolic_pressure = table.Column<int>(type: "INTEGER", nullable: true),
                    diastolic_pressure = table.Column<int>(type: "INTEGER", nullable: true),
                    temperature = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    blood_glucose = table.Column<decimal>(type: "TEXT", precision: 7, scale: 2, nullable: true),
                    oxygen_saturation = table.Column<int>(type: "INTEGER", nullable: true),
                    heart_rate = table.Column<int>(type: "INTEGER", nullable: true),
                    chief_complaint = table.Column<string>(type: "TEXT", nullable: true),
                    previous_pathological_history = table.Column<string>(type: "TEXT", nullable: true),
                    current_disease_history = table.Column<string>(type: "TEXT", nullable: true),
                    physical_exam = table.Column<string>(type: "TEXT", nullable: true),
                    diagnostic_hypothesis = table.Column<string>(type: "TEXT", nullable: true),
                    cid10_code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    cid10_name = table.Column<string>(type: "TEXT", nullable: true),
                    responsible_signature = table.Column<string>(type: "TEXT", nullable: true),
                    has_back_side = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medical_attendances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "medical_records",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    appointment_id = table.Column<long>(type: "INTEGER", nullable: false),
                    patient_id = table.Column<long>(type: "INTEGER", nullable: false),
                    doctor_name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    anamnesis = table.Column<string>(type: "TEXT", nullable: true),
                    physical_exam = table.Column<string>(type: "TEXT", nullable: true),
                    diagnostic_hypothesis = table.Column<string>(type: "TEXT", nullable: true),
                    cid10 = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    conduct = table.Column<string>(type: "TEXT", nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medical_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "medications",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    generic_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    commercial_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    therapeutic_class = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    pharmaceutical_form = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    dosage = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    entry_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    origin = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    origin_id = table.Column<long>(type: "INTEGER", nullable: true),
                    responsible = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    manufacturer = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    manufacturer_id = table.Column<long>(type: "INTEGER", nullable: true),
                    batch = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    expiration_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    unit = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    location = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    location_id = table.Column<long>(type: "INTEGER", nullable: true),
                    minimum_quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    is_controlled = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    cpf = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    sex = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    phone = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    address = table.Column<string>(type: "TEXT", nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    comorbidities = table.Column<string>(type: "TEXT", nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prescriptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    medical_record_id = table.Column<long>(type: "INTEGER", nullable: false),
                    patient_id = table.Column<long>(type: "INTEGER", nullable: false),
                    medication_id = table.Column<long>(type: "INTEGER", nullable: true),
                    medication_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    dosage = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    directions = table.Column<string>(type: "TEXT", nullable: true),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    is_dispensed = table.Column<bool>(type: "INTEGER", nullable: false),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    dispensed_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_locations",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_locations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    type = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    medication_id = table.Column<long>(type: "INTEGER", nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    responsible = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    batch = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    reason = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    attendance_id = table.Column<long>(type: "INTEGER", nullable: true),
                    appointment_id = table.Column<long>(type: "INTEGER", nullable: true),
                    prescription_id = table.Column<long>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "triage_records",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    appointment_id = table.Column<long>(type: "INTEGER", nullable: false),
                    blood_pressure = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    temperature = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    weight = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    height = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    heart_rate = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    oxygen_saturation = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    chief_complaint = table.Column<string>(type: "TEXT", nullable: true),
                    responsible = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_triage_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", nullable: false),
                    signature_password_hash = table.Column<string>(type: "TEXT", nullable: true),
                    signature_password_reset_required = table.Column<bool>(type: "INTEGER", nullable: false),
                    role = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    access_revoked_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    access_revoked_by_user_id = table.Column<long>(type: "INTEGER", nullable: true),
                    access_revocation_reason = table.Column<string>(type: "TEXT", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    deleted_by_user_id = table.Column<long>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "medical_attendance_cid10_codes",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    medical_attendance_id = table.Column<long>(type: "INTEGER", nullable: false),
                    order = table.Column<int>(type: "INTEGER", nullable: false),
                    cid10_code_id = table.Column<long>(type: "INTEGER", nullable: false),
                    code = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medical_attendance_cid10_codes", x => x.id);
                    table.ForeignKey(
                        name: "FK_medical_attendance_cid10_codes_cid10_codes_cid10_code_id",
                        column: x => x.cid10_code_id,
                        principalTable: "cid10_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_medical_attendance_cid10_codes_medical_attendances_medical_attendance_id",
                        column: x => x.medical_attendance_id,
                        principalTable: "medical_attendances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medical_attendance_dispensations",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    medical_attendance_id = table.Column<long>(type: "INTEGER", nullable: false),
                    order = table.Column<int>(type: "INTEGER", nullable: false),
                    batch = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    prescription_id = table.Column<long>(type: "INTEGER", nullable: true),
                    medication_id = table.Column<long>(type: "INTEGER", nullable: true),
                    medication_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    responsible = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    dispensed_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medical_attendance_dispensations", x => x.id);
                    table.ForeignKey(
                        name: "FK_medical_attendance_dispensations_medical_attendances_medical_attendance_id",
                        column: x => x.medical_attendance_id,
                        principalTable: "medical_attendances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medical_attendance_nursing_checks",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    medical_attendance_id = table.Column<long>(type: "INTEGER", nullable: false),
                    order = table.Column<int>(type: "INTEGER", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medical_attendance_nursing_checks", x => x.id);
                    table.ForeignKey(
                        name: "FK_medical_attendance_nursing_checks_medical_attendances_medical_attendance_id",
                        column: x => x.medical_attendance_id,
                        principalTable: "medical_attendances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medical_attendance_prescriptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    medical_attendance_id = table.Column<long>(type: "INTEGER", nullable: false),
                    order = table.Column<int>(type: "INTEGER", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    medication_id = table.Column<long>(type: "INTEGER", nullable: true),
                    medication_name = table.Column<string>(type: "TEXT", nullable: true),
                    dosage = table.Column<string>(type: "TEXT", nullable: true),
                    directions = table.Column<string>(type: "TEXT", nullable: true),
                    quantity = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medical_attendance_prescriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_medical_attendance_prescriptions_medical_attendances_medical_attendance_id",
                        column: x => x.medical_attendance_id,
                        principalTable: "medical_attendances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_module_accesses",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<long>(type: "INTEGER", nullable: false),
                    module = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    is_revoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    revoked_by_user_id = table.Column<long>(type: "INTEGER", nullable: true),
                    revocation_reason = table.Column<string>(type: "TEXT", nullable: true),
                    granted_by_user_id = table.Column<long>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_module_accesses", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_module_accesses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity",
                table: "audit_logs",
                column: "entity");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_name",
                table: "audit_logs",
                column: "user_name");

            migrationBuilder.CreateIndex(
                name: "IX_care_appointments_date",
                table: "care_appointments",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_care_appointments_patient_id",
                table: "care_appointments",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_care_appointments_status",
                table: "care_appointments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_cid10_codes_code",
                table: "cid10_codes",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "IX_cid10_codes_code_name",
                table: "cid10_codes",
                columns: new[] { "code", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cid10_codes_name",
                table: "cid10_codes",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_donors_name",
                table: "donors",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_manufacturers_name",
                table: "manufacturers",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_medical_attendance_cid10_codes_cid10_code_id",
                table: "medical_attendance_cid10_codes",
                column: "cid10_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_medical_attendance_cid10_codes_medical_attendance_id_cid10_code_id",
                table: "medical_attendance_cid10_codes",
                columns: new[] { "medical_attendance_id", "cid10_code_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_medical_attendance_cid10_codes_medical_attendance_id_order",
                table: "medical_attendance_cid10_codes",
                columns: new[] { "medical_attendance_id", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_medical_attendance_dispensations_medical_attendance_id_order",
                table: "medical_attendance_dispensations",
                columns: new[] { "medical_attendance_id", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_medical_attendance_dispensations_prescription_id",
                table: "medical_attendance_dispensations",
                column: "prescription_id");

            migrationBuilder.CreateIndex(
                name: "IX_medical_attendance_nursing_checks_medical_attendance_id_order",
                table: "medical_attendance_nursing_checks",
                columns: new[] { "medical_attendance_id", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_medical_attendance_prescriptions_medical_attendance_id_order",
                table: "medical_attendance_prescriptions",
                columns: new[] { "medical_attendance_id", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_medical_attendances_appointment_id",
                table: "medical_attendances",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_medical_attendances_patient_id",
                table: "medical_attendances",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_medical_records_appointment_id",
                table: "medical_records",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_medical_records_patient_id",
                table: "medical_records",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_medications_batch",
                table: "medications",
                column: "batch");

            migrationBuilder.CreateIndex(
                name: "IX_medications_commercial_name",
                table: "medications",
                column: "commercial_name");

            migrationBuilder.CreateIndex(
                name: "IX_medications_expiration_date",
                table: "medications",
                column: "expiration_date");

            migrationBuilder.CreateIndex(
                name: "IX_medications_generic_name",
                table: "medications",
                column: "generic_name");

            migrationBuilder.CreateIndex(
                name: "IX_medications_location_id",
                table: "medications",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_medications_manufacturer_id",
                table: "medications",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "IX_medications_origin_id",
                table: "medications",
                column: "origin_id");

            migrationBuilder.CreateIndex(
                name: "IX_patients_cpf",
                table: "patients",
                column: "cpf");

            migrationBuilder.CreateIndex(
                name: "IX_patients_is_active",
                table: "patients",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_patients_name",
                table: "patients",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_is_dispensed",
                table: "prescriptions",
                column: "is_dispensed");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_medical_record_id",
                table: "prescriptions",
                column: "medical_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_patient_id",
                table: "prescriptions",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_locations_name",
                table: "stock_locations",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_appointment_id",
                table: "stock_movements",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_attendance_id",
                table: "stock_movements",
                column: "attendance_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_date",
                table: "stock_movements",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_medication_id",
                table: "stock_movements",
                column: "medication_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_prescription_id",
                table: "stock_movements",
                column: "prescription_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_type",
                table: "stock_movements",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_triage_records_appointment_id",
                table: "triage_records",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_module_accesses_user_id_module",
                table: "user_module_accesses",
                columns: new[] { "user_id", "module" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true,
                filter: "is_deleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_users_is_deleted",
                table: "users",
                column: "is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "care_appointments");

            migrationBuilder.DropTable(
                name: "donors");

            migrationBuilder.DropTable(
                name: "manufacturers");

            migrationBuilder.DropTable(
                name: "medical_attendance_cid10_codes");

            migrationBuilder.DropTable(
                name: "medical_attendance_dispensations");

            migrationBuilder.DropTable(
                name: "medical_attendance_nursing_checks");

            migrationBuilder.DropTable(
                name: "medical_attendance_prescriptions");

            migrationBuilder.DropTable(
                name: "medical_records");

            migrationBuilder.DropTable(
                name: "medications");

            migrationBuilder.DropTable(
                name: "patients");

            migrationBuilder.DropTable(
                name: "prescriptions");

            migrationBuilder.DropTable(
                name: "stock_locations");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "triage_records");

            migrationBuilder.DropTable(
                name: "user_module_accesses");

            migrationBuilder.DropTable(
                name: "cid10_codes");

            migrationBuilder.DropTable(
                name: "medical_attendances");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
