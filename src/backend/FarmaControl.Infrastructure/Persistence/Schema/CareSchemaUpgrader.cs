using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Persistence.Schema;

public static class CareSchemaUpgrader
{
    public static async Task ApplyAsync(
        FarmaControlDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "patients" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_patients" PRIMARY KEY AUTOINCREMENT,
                "name" TEXT NOT NULL,
                "cpf" TEXT NULL,
                "birth_date" TEXT NULL,
                "sex" TEXT NULL,
                "phone" TEXT NULL,
                "address" TEXT NULL,
                "notes" TEXT NULL,
                "comorbidities" TEXT NULL,
                "is_active" INTEGER NOT NULL,
                "created_at" TEXT NOT NULL,
                "updated_at" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "care_appointments" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_care_appointments" PRIMARY KEY AUTOINCREMENT,
                "patient_id" INTEGER NOT NULL,
                "date" TEXT NOT NULL,
                "time" TEXT NULL,
                "type" TEXT NOT NULL,
                "is_emergency" INTEGER NOT NULL,
                "status" TEXT NOT NULL,
                "doctor_name" TEXT NULL,
                "responsible" TEXT NULL,
                "notes" TEXT NULL,
                "created_at" TEXT NOT NULL,
                "updated_at" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "triage_records" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_triage_records" PRIMARY KEY AUTOINCREMENT,
                "appointment_id" INTEGER NOT NULL,
                "blood_pressure" TEXT NULL,
                "temperature" TEXT NULL,
                "weight" TEXT NULL,
                "height" TEXT NULL,
                "heart_rate" TEXT NULL,
                "oxygen_saturation" TEXT NULL,
                "chief_complaint" TEXT NULL,
                "responsible" TEXT NULL,
                "notes" TEXT NULL,
                "created_at" TEXT NOT NULL,
                "updated_at" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "medical_records" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_medical_records" PRIMARY KEY AUTOINCREMENT,
                "appointment_id" INTEGER NOT NULL,
                "patient_id" INTEGER NOT NULL,
                "doctor_name" TEXT NULL,
                "anamnesis" TEXT NULL,
                "physical_exam" TEXT NULL,
                "diagnostic_hypothesis" TEXT NULL,
                "cid10" TEXT NULL,
                "conduct" TEXT NULL,
                "notes" TEXT NULL,
                "created_at" TEXT NOT NULL,
                "updated_at" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "prescriptions" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_prescriptions" PRIMARY KEY AUTOINCREMENT,
                "medical_record_id" INTEGER NOT NULL,
                "patient_id" INTEGER NOT NULL,
                "medication_id" INTEGER NULL,
                "medication_name" TEXT NULL,
                "dosage" TEXT NULL,
                "directions" TEXT NULL,
                "quantity" INTEGER NOT NULL,
                "is_dispensed" INTEGER NOT NULL,
                "notes" TEXT NULL,
                "created_at" TEXT NOT NULL,
                "dispensed_at" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "medical_attendances" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_medical_attendances" PRIMARY KEY AUTOINCREMENT,
                "appointment_id" INTEGER NOT NULL,
                "patient_id" INTEGER NOT NULL,
                "responsible_user_id" INTEGER NULL,
                "responsible_name" TEXT NULL,
                "name" TEXT NOT NULL,
                "age" INTEGER NULL,
                "attendance_date" TEXT NOT NULL,
                "attendance_time" TEXT NULL,
                "city" TEXT NULL,
                "church" TEXT NULL,
                "pastor" TEXT NULL,
                "attendance_type" TEXT NOT NULL,
                "return_number" INTEGER NULL,
                "systolic_pressure" INTEGER NULL,
                "diastolic_pressure" INTEGER NULL,
                "temperature" TEXT NULL,
                "blood_glucose" TEXT NULL,
                "oxygen_saturation" INTEGER NULL,
                "heart_rate" INTEGER NULL,
                "chief_complaint" TEXT NULL,
                "previous_pathological_history" TEXT NULL,
                "current_disease_history" TEXT NULL,
                "physical_exam" TEXT NULL,
                "diagnostic_hypothesis" TEXT NULL,
                "cid10_code" TEXT NULL,
                "cid10_name" TEXT NULL,
                "responsible_signature" TEXT NULL,
                "triage_responsible_user_id" INTEGER NULL,
                "triage_responsible_name" TEXT NULL,
                "triage_responsible_signature" TEXT NULL,
                "medical_responsible_user_id" INTEGER NULL,
                "medical_responsible_name" TEXT NULL,
                "medical_responsible_signature" TEXT NULL,
                "dispensation_responsible_user_id" INTEGER NULL,
                "dispensation_responsible_name" TEXT NULL,
                "dispensation_responsible_signature" TEXT NULL,
                "has_back_side" INTEGER NOT NULL,
                "created_at" TEXT NOT NULL,
                "updated_at" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "medical_attendance_prescriptions" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_medical_attendance_prescriptions" PRIMARY KEY AUTOINCREMENT,
                "medical_attendance_id" INTEGER NOT NULL,
                "order" INTEGER NOT NULL,
                "description" TEXT NULL,
                "medication_id" INTEGER NULL,
                "medication_name" TEXT NULL,
                "dosage" TEXT NULL,
                "directions" TEXT NULL,
                "quantity" INTEGER NULL
            );

            CREATE TABLE IF NOT EXISTS "medical_attendance_nursing_checks" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_medical_attendance_nursing_checks" PRIMARY KEY AUTOINCREMENT,
                "medical_attendance_id" INTEGER NOT NULL,
                "order" INTEGER NOT NULL,
                "description" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "medical_attendance_dispensations" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_medical_attendance_dispensations" PRIMARY KEY AUTOINCREMENT,
                "medical_attendance_id" INTEGER NOT NULL,
                "order" INTEGER NOT NULL,
                "batch" TEXT NULL,
                "prescription_id" INTEGER NULL,
                "medication_id" INTEGER NULL,
                "medication_name" TEXT NULL,
                "quantity" INTEGER NULL,
                "responsible" TEXT NULL,
                "dispensed_at" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "medical_attendance_cid10_codes" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_medical_attendance_cid10_codes" PRIMARY KEY AUTOINCREMENT,
                "medical_attendance_id" INTEGER NOT NULL,
                "order" INTEGER NOT NULL,
                "cid10_code_id" INTEGER NOT NULL,
                "code" TEXT NOT NULL,
                "name" TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_patients_name" ON "patients" ("name");
            CREATE INDEX IF NOT EXISTS "IX_patients_cpf" ON "patients" ("cpf");
            CREATE INDEX IF NOT EXISTS "IX_patients_is_active" ON "patients" ("is_active");
            CREATE INDEX IF NOT EXISTS "IX_care_appointments_patient_id" ON "care_appointments" ("patient_id");
            CREATE INDEX IF NOT EXISTS "IX_care_appointments_date" ON "care_appointments" ("date");
            CREATE INDEX IF NOT EXISTS "IX_care_appointments_status" ON "care_appointments" ("status");
            CREATE INDEX IF NOT EXISTS "IX_triage_records_appointment_id" ON "triage_records" ("appointment_id");
            CREATE INDEX IF NOT EXISTS "IX_medical_records_appointment_id" ON "medical_records" ("appointment_id");
            CREATE INDEX IF NOT EXISTS "IX_medical_records_patient_id" ON "medical_records" ("patient_id");
            CREATE INDEX IF NOT EXISTS "IX_prescriptions_medical_record_id" ON "prescriptions" ("medical_record_id");
            CREATE INDEX IF NOT EXISTS "IX_prescriptions_patient_id" ON "prescriptions" ("patient_id");
            CREATE INDEX IF NOT EXISTS "IX_prescriptions_is_dispensed" ON "prescriptions" ("is_dispensed");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_medical_attendances_appointment_id" ON "medical_attendances" ("appointment_id");
            CREATE INDEX IF NOT EXISTS "IX_medical_attendances_patient_id" ON "medical_attendances" ("patient_id");
            CREATE INDEX IF NOT EXISTS "IX_medical_attendance_prescriptions_attendance_order" ON "medical_attendance_prescriptions" ("medical_attendance_id", "order");
            CREATE INDEX IF NOT EXISTS "IX_medical_attendance_nursing_checks_attendance_order" ON "medical_attendance_nursing_checks" ("medical_attendance_id", "order");
            CREATE INDEX IF NOT EXISTS "IX_medical_attendance_dispensations_attendance_order" ON "medical_attendance_dispensations" ("medical_attendance_id", "order");
            CREATE INDEX IF NOT EXISTS "IX_medical_attendance_dispensations_prescription_id" ON "medical_attendance_dispensations" ("prescription_id");
            CREATE INDEX IF NOT EXISTS "IX_medical_attendance_cid10_codes_attendance_order" ON "medical_attendance_cid10_codes" ("medical_attendance_id", "order");
            CREATE INDEX IF NOT EXISTS "IX_medical_attendance_cid10_codes_cid10_code_id" ON "medical_attendance_cid10_codes" ("cid10_code_id");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_medical_attendance_cid10_codes_attendance_cid10_code" ON "medical_attendance_cid10_codes" ("medical_attendance_id", "cid10_code_id");
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT OR IGNORE INTO "medical_attendance_cid10_codes" (
                "medical_attendance_id",
                "order",
                "cid10_code_id",
                "code",
                "name"
            )
            SELECT
                attendance."id",
                1,
                cid10."id",
                cid10."code",
                cid10."name"
            FROM "medical_attendances" AS attendance
            INNER JOIN "cid10_codes" AS cid10
                ON lower(cid10."code") = lower(trim(attendance."cid10_code"))
                AND lower(cid10."name") = lower(trim(attendance."cid10_name"))
            WHERE attendance."cid10_code" IS NOT NULL
              AND trim(attendance."cid10_code") <> ''
              AND attendance."cid10_name" IS NOT NULL
              AND trim(attendance."cid10_name") <> '';
            """,
            cancellationToken);

        await AddColumnIfMissingAsync(
            dbContext,
            "patients",
            "comorbidities",
            "ALTER TABLE \"patients\" ADD COLUMN \"comorbidities\" TEXT NULL;",
            cancellationToken);

        await AddColumnIfMissingAsync(
            dbContext,
            "medical_attendances",
            "diagnostic_hypothesis",
            "ALTER TABLE \"medical_attendances\" ADD COLUMN \"diagnostic_hypothesis\" TEXT NULL;",
            cancellationToken);

        await AddColumnIfMissingAsync(
            dbContext,
            "medical_attendances",
            "cid10_code",
            "ALTER TABLE \"medical_attendances\" ADD COLUMN \"cid10_code\" TEXT NULL;",
            cancellationToken);

        await AddColumnIfMissingAsync(
            dbContext,
            "medical_attendances",
            "cid10_name",
            "ALTER TABLE \"medical_attendances\" ADD COLUMN \"cid10_name\" TEXT NULL;",
            cancellationToken);

        await AddMedicalAttendanceColumnIfMissingAsync(dbContext, "triage_responsible_user_id", "INTEGER NULL", cancellationToken);
        await AddMedicalAttendanceColumnIfMissingAsync(dbContext, "triage_responsible_name", "TEXT NULL", cancellationToken);
        await AddMedicalAttendanceColumnIfMissingAsync(dbContext, "triage_responsible_signature", "TEXT NULL", cancellationToken);
        await AddMedicalAttendanceColumnIfMissingAsync(dbContext, "medical_responsible_user_id", "INTEGER NULL", cancellationToken);
        await AddMedicalAttendanceColumnIfMissingAsync(dbContext, "medical_responsible_name", "TEXT NULL", cancellationToken);
        await AddMedicalAttendanceColumnIfMissingAsync(dbContext, "medical_responsible_signature", "TEXT NULL", cancellationToken);
        await AddMedicalAttendanceColumnIfMissingAsync(dbContext, "dispensation_responsible_user_id", "INTEGER NULL", cancellationToken);
        await AddMedicalAttendanceColumnIfMissingAsync(dbContext, "dispensation_responsible_name", "TEXT NULL", cancellationToken);
        await AddMedicalAttendanceColumnIfMissingAsync(dbContext, "dispensation_responsible_signature", "TEXT NULL", cancellationToken);

        await AddColumnIfMissingAsync(
            dbContext,
            "medical_attendance_prescriptions",
            "medication_id",
            "ALTER TABLE \"medical_attendance_prescriptions\" ADD COLUMN \"medication_id\" INTEGER NULL;",
            cancellationToken);

        await AddColumnIfMissingAsync(
            dbContext,
            "medical_attendance_prescriptions",
            "medication_name",
            "ALTER TABLE \"medical_attendance_prescriptions\" ADD COLUMN \"medication_name\" TEXT NULL;",
            cancellationToken);

        await AddColumnIfMissingAsync(
            dbContext,
            "medical_attendance_prescriptions",
            "dosage",
            "ALTER TABLE \"medical_attendance_prescriptions\" ADD COLUMN \"dosage\" TEXT NULL;",
            cancellationToken);

        await AddColumnIfMissingAsync(
            dbContext,
            "medical_attendance_prescriptions",
            "directions",
            "ALTER TABLE \"medical_attendance_prescriptions\" ADD COLUMN \"directions\" TEXT NULL;",
            cancellationToken);

        await AddColumnIfMissingAsync(
            dbContext,
            "medical_attendance_prescriptions",
            "quantity",
            "ALTER TABLE \"medical_attendance_prescriptions\" ADD COLUMN \"quantity\" INTEGER NULL;",
            cancellationToken);

        await AddDispensationColumnIfMissingAsync(
            dbContext,
            "prescription_id",
            "ALTER TABLE \"medical_attendance_dispensations\" ADD COLUMN \"prescription_id\" INTEGER NULL;",
            cancellationToken);

        await AddDispensationColumnIfMissingAsync(
            dbContext,
            "medication_id",
            "ALTER TABLE \"medical_attendance_dispensations\" ADD COLUMN \"medication_id\" INTEGER NULL;",
            cancellationToken);

        await AddDispensationColumnIfMissingAsync(
            dbContext,
            "medication_name",
            "ALTER TABLE \"medical_attendance_dispensations\" ADD COLUMN \"medication_name\" TEXT NULL;",
            cancellationToken);

        await AddDispensationColumnIfMissingAsync(
            dbContext,
            "quantity",
            "ALTER TABLE \"medical_attendance_dispensations\" ADD COLUMN \"quantity\" INTEGER NULL;",
            cancellationToken);

        await AddDispensationColumnIfMissingAsync(
            dbContext,
            "responsible",
            "ALTER TABLE \"medical_attendance_dispensations\" ADD COLUMN \"responsible\" TEXT NULL;",
            cancellationToken);

        await AddDispensationColumnIfMissingAsync(
            dbContext,
            "dispensed_at",
            "ALTER TABLE \"medical_attendance_dispensations\" ADD COLUMN \"dispensed_at\" TEXT NULL;",
            cancellationToken);

        await BackfillStockMovementClinicalReferencesAsync(dbContext, cancellationToken);
    }

    private static async Task BackfillStockMovementClinicalReferencesAsync(
        FarmaControlDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            UPDATE "stock_movements"
            SET
                "attendance_id" = (
                    SELECT dispensation."medical_attendance_id"
                    FROM "medical_attendance_dispensations" AS dispensation
                    WHERE dispensation."prescription_id" IS NOT NULL
                      AND dispensation."medication_id" = "stock_movements"."medication_id"
                      AND (dispensation."batch" IS NULL OR dispensation."batch" = "stock_movements"."batch")
                      AND (dispensation."quantity" IS NULL OR dispensation."quantity" = "stock_movements"."quantity")
                    ORDER BY dispensation."id" DESC
                    LIMIT 1
                ),
                "prescription_id" = (
                    SELECT dispensation."prescription_id"
                    FROM "medical_attendance_dispensations" AS dispensation
                    WHERE dispensation."prescription_id" IS NOT NULL
                      AND dispensation."medication_id" = "stock_movements"."medication_id"
                      AND (dispensation."batch" IS NULL OR dispensation."batch" = "stock_movements"."batch")
                      AND (dispensation."quantity" IS NULL OR dispensation."quantity" = "stock_movements"."quantity")
                    ORDER BY dispensation."id" DESC
                    LIMIT 1
                )
            WHERE "stock_movements"."reason" = 'Dispensacao'
              AND ("stock_movements"."attendance_id" IS NULL OR "stock_movements"."prescription_id" IS NULL);

            UPDATE "stock_movements"
            SET "appointment_id" = (
                SELECT attendance."appointment_id"
                FROM "medical_attendances" AS attendance
                WHERE attendance."id" = "stock_movements"."attendance_id"
                LIMIT 1
            )
            WHERE "stock_movements"."appointment_id" IS NULL
              AND "stock_movements"."attendance_id" IS NOT NULL;
            """,
            cancellationToken);
    }

    private static async Task AddDispensationColumnIfMissingAsync(
        FarmaControlDbContext dbContext,
        string column,
        string alterSql,
        CancellationToken cancellationToken)
    {
        await AddColumnIfMissingAsync(
            dbContext,
            "medical_attendance_dispensations",
            column,
            alterSql,
            cancellationToken);
    }

    private static async Task AddMedicalAttendanceColumnIfMissingAsync(
        FarmaControlDbContext dbContext,
        string column,
        string definition,
        CancellationToken cancellationToken)
    {
        await AddColumnIfMissingAsync(
            dbContext,
            "medical_attendances",
            column,
            $"ALTER TABLE \"medical_attendances\" ADD COLUMN \"{column}\" {definition};",
            cancellationToken);
    }

    private static async Task AddColumnIfMissingAsync(
        FarmaControlDbContext dbContext,
        string table,
        string column,
        string alterSql,
        CancellationToken cancellationToken)
    {
        string sql = table switch
        {
            "patients" =>
                "SELECT COUNT(1) AS Value FROM pragma_table_info('patients') WHERE name = {0}",
            "medical_attendance_prescriptions" =>
                "SELECT COUNT(1) AS Value FROM pragma_table_info('medical_attendance_prescriptions') WHERE name = {0}",
            "medical_attendance_dispensations" =>
                "SELECT COUNT(1) AS Value FROM pragma_table_info('medical_attendance_dispensations') WHERE name = {0}",
            "medical_attendances" =>
                "SELECT COUNT(1) AS Value FROM pragma_table_info('medical_attendances') WHERE name = {0}",
            _ => throw new InvalidOperationException("Tabela nao suportada para upgrade de schema.")
        };

        int exists = await dbContext.Database
            .SqlQueryRaw<int>(
                sql,
                column)
            .SingleAsync(cancellationToken);

        if (exists == 0)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                alterSql,
                cancellationToken);
        }
    }
}
