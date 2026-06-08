using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Persistence.Schema;

public static class InventorySchemaUpgrader
{
    public static async Task ApplyAsync(
        FarmaControlDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "medications" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_medications" PRIMARY KEY AUTOINCREMENT,
                "generic_name" TEXT NULL,
                "commercial_name" TEXT NULL,
                "therapeutic_class" TEXT NULL,
                "pharmaceutical_form" TEXT NULL,
                "dosage" TEXT NULL,
                "entry_date" TEXT NULL,
                "origin" TEXT NULL,
                "origin_id" INTEGER NULL,
                "responsible" TEXT NULL,
                "manufacturer" TEXT NULL,
                "manufacturer_id" INTEGER NULL,
                "batch" TEXT NULL,
                "expiration_date" TEXT NULL,
                "quantity" INTEGER NOT NULL,
                "unit" TEXT NULL,
                "location" TEXT NULL,
                "location_id" INTEGER NULL,
                "minimum_quantity" INTEGER NOT NULL,
                "is_controlled" INTEGER NOT NULL,
                "created_at" TEXT NOT NULL,
                "updated_at" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "donors" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_donors" PRIMARY KEY AUTOINCREMENT,
                "name" TEXT NOT NULL,
                "phone" TEXT NULL,
                "notes" TEXT NULL,
                "created_at" TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "manufacturers" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_manufacturers" PRIMARY KEY AUTOINCREMENT,
                "name" TEXT NOT NULL,
                "cnpj" TEXT NULL,
                "created_at" TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "stock_locations" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_stock_locations" PRIMARY KEY AUTOINCREMENT,
                "name" TEXT NOT NULL,
                "created_at" TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "stock_movements" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_stock_movements" PRIMARY KEY AUTOINCREMENT,
                "type" TEXT NOT NULL,
                "medication_id" INTEGER NOT NULL,
                "quantity" INTEGER NOT NULL,
                "date" TEXT NOT NULL,
                "responsible" TEXT NOT NULL,
                "notes" TEXT NULL,
                "batch" TEXT NULL,
                "reason" TEXT NULL,
                "attendance_id" INTEGER NULL,
                "appointment_id" INTEGER NULL,
                "prescription_id" INTEGER NULL,
                "created_at" TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_medications_generic_name" ON "medications" ("generic_name");
            CREATE INDEX IF NOT EXISTS "IX_medications_commercial_name" ON "medications" ("commercial_name");
            CREATE INDEX IF NOT EXISTS "IX_medications_batch" ON "medications" ("batch");
            CREATE INDEX IF NOT EXISTS "IX_medications_expiration_date" ON "medications" ("expiration_date");
            CREATE INDEX IF NOT EXISTS "IX_donors_name" ON "donors" ("name");
            CREATE INDEX IF NOT EXISTS "IX_manufacturers_name" ON "manufacturers" ("name");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_stock_locations_name" ON "stock_locations" ("name");
            CREATE INDEX IF NOT EXISTS "IX_stock_movements_medication_id" ON "stock_movements" ("medication_id");
            CREATE INDEX IF NOT EXISTS "IX_stock_movements_date" ON "stock_movements" ("date");
            CREATE INDEX IF NOT EXISTS "IX_stock_movements_type" ON "stock_movements" ("type");
            """,
            cancellationToken);

        await AddColumnIfMissingAsync(
            dbContext,
            "origin_id",
            "ALTER TABLE \"medications\" ADD COLUMN \"origin_id\" INTEGER NULL;",
            cancellationToken);

        await AddColumnIfMissingAsync(
            dbContext,
            "manufacturer_id",
            "ALTER TABLE \"medications\" ADD COLUMN \"manufacturer_id\" INTEGER NULL;",
            cancellationToken);

        await AddColumnIfMissingAsync(
            dbContext,
            "location_id",
            "ALTER TABLE \"medications\" ADD COLUMN \"location_id\" INTEGER NULL;",
            cancellationToken);

        await AddStockMovementColumnIfMissingAsync(
            dbContext,
            "attendance_id",
            "ALTER TABLE \"stock_movements\" ADD COLUMN \"attendance_id\" INTEGER NULL;",
            cancellationToken);

        await AddStockMovementColumnIfMissingAsync(
            dbContext,
            "appointment_id",
            "ALTER TABLE \"stock_movements\" ADD COLUMN \"appointment_id\" INTEGER NULL;",
            cancellationToken);

        await AddStockMovementColumnIfMissingAsync(
            dbContext,
            "prescription_id",
            "ALTER TABLE \"stock_movements\" ADD COLUMN \"prescription_id\" INTEGER NULL;",
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_medications_location_id" ON "medications" ("location_id");
            CREATE INDEX IF NOT EXISTS "IX_medications_origin_id" ON "medications" ("origin_id");
            CREATE INDEX IF NOT EXISTS "IX_medications_manufacturer_id" ON "medications" ("manufacturer_id");
            CREATE INDEX IF NOT EXISTS "IX_stock_movements_attendance_id" ON "stock_movements" ("attendance_id");
            CREATE INDEX IF NOT EXISTS "IX_stock_movements_appointment_id" ON "stock_movements" ("appointment_id");
            CREATE INDEX IF NOT EXISTS "IX_stock_movements_prescription_id" ON "stock_movements" ("prescription_id");
            """,
            cancellationToken);

        await BackfillMedicationReferencesAsync(dbContext, cancellationToken);
    }

    private static async Task BackfillMedicationReferencesAsync(
        FarmaControlDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "stock_locations" ("name", "created_at")
            SELECT DISTINCT trim("location"), datetime('now')
            FROM "medications" AS medication
            WHERE medication."location" IS NOT NULL
              AND trim(medication."location") <> ''
              AND NOT EXISTS (
                  SELECT 1
                  FROM "stock_locations" AS location
                  WHERE lower(location."name") = lower(trim(medication."location"))
              );

            UPDATE "medications"
            SET "location_id" = (
                SELECT location."id"
                FROM "stock_locations" AS location
                WHERE lower(location."name") = lower(trim("medications"."location"))
                ORDER BY location."id"
                LIMIT 1
            )
            WHERE "location_id" IS NULL
              AND "location" IS NOT NULL
              AND trim("location") <> '';

            INSERT INTO "manufacturers" ("name", "cnpj", "created_at")
            SELECT DISTINCT trim("manufacturer"), NULL, datetime('now')
            FROM "medications" AS medication
            WHERE medication."manufacturer" IS NOT NULL
              AND trim(medication."manufacturer") <> ''
              AND NOT EXISTS (
                  SELECT 1
                  FROM "manufacturers" AS manufacturer
                  WHERE lower(manufacturer."name") = lower(trim(medication."manufacturer"))
              );

            UPDATE "medications"
            SET "manufacturer_id" = (
                SELECT manufacturer."id"
                FROM "manufacturers" AS manufacturer
                WHERE lower(manufacturer."name") = lower(trim("medications"."manufacturer"))
                ORDER BY manufacturer."id"
                LIMIT 1
            )
            WHERE "manufacturer_id" IS NULL
              AND "manufacturer" IS NOT NULL
              AND trim("manufacturer") <> '';

            INSERT INTO "donors" ("name", "phone", "notes", "created_at")
            SELECT DISTINCT trim("origin"), NULL, NULL, datetime('now')
            FROM "medications" AS medication
            WHERE medication."origin" IS NOT NULL
              AND trim(medication."origin") <> ''
              AND NOT EXISTS (
                  SELECT 1
                  FROM "donors" AS donor
                  WHERE lower(donor."name") = lower(trim(medication."origin"))
              );

            UPDATE "medications"
            SET "origin_id" = (
                SELECT donor."id"
                FROM "donors" AS donor
                WHERE lower(donor."name") = lower(trim("medications"."origin"))
                ORDER BY donor."id"
                LIMIT 1
            )
            WHERE "origin_id" IS NULL
              AND "origin" IS NOT NULL
              AND trim("origin") <> '';
            """,
            cancellationToken);
    }

    private static async Task AddColumnIfMissingAsync(
        FarmaControlDbContext dbContext,
        string column,
        string alterSql,
        CancellationToken cancellationToken)
    {
        int exists = await dbContext.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(1) AS Value FROM pragma_table_info('medications') WHERE name = {0}",
                column)
            .SingleAsync(cancellationToken);

        if (exists == 0)
        {
            await dbContext.Database.ExecuteSqlRawAsync(alterSql, cancellationToken);
        }
    }

    private static async Task AddStockMovementColumnIfMissingAsync(
        FarmaControlDbContext dbContext,
        string column,
        string alterSql,
        CancellationToken cancellationToken)
    {
        int exists = await dbContext.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(1) AS Value FROM pragma_table_info('stock_movements') WHERE name = {0}",
                column)
            .SingleAsync(cancellationToken);

        if (exists == 0)
        {
            await dbContext.Database.ExecuteSqlRawAsync(alterSql, cancellationToken);
        }
    }
}
