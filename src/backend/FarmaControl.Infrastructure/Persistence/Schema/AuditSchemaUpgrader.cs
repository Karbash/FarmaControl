using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Persistence.Schema;

public static class AuditSchemaUpgrader
{
    public static async Task ApplyAsync(
        FarmaControlDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "audit_logs" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_audit_logs" PRIMARY KEY AUTOINCREMENT,
                "user_id" INTEGER NULL,
                "user_name" TEXT NOT NULL,
                "action" TEXT NOT NULL,
                "entity" TEXT NOT NULL,
                "entity_id" INTEGER NULL,
                "description" TEXT NOT NULL,
                "created_at" TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_audit_logs_created_at" ON "audit_logs" ("created_at");
            CREATE INDEX IF NOT EXISTS "IX_audit_logs_action" ON "audit_logs" ("action");
            CREATE INDEX IF NOT EXISTS "IX_audit_logs_entity" ON "audit_logs" ("entity");
            CREATE INDEX IF NOT EXISTS "IX_audit_logs_user_name" ON "audit_logs" ("user_name");
            """,
            cancellationToken);
    }
}
