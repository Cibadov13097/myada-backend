using Microsoft.EntityFrameworkCore;

namespace CallService.Api.Persistence;

public static class CallHistorySchemaInitializer
{
    public static async Task InitializeAsync(CallHistoryDbContext dbContext, CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE SCHEMA IF NOT EXISTS call;

            CREATE TABLE IF NOT EXISTS call."CallHistory"
            (
                "Id" uuid NOT NULL,
                "CallId" character varying(64) NOT NULL,
                "RoomId" character varying(96) NOT NULL,
                "CallerUserId" character varying(128) NOT NULL,
                "CallerDisplayName" character varying(256) NOT NULL,
                "DispatcherUserId" character varying(128) NOT NULL,
                "DispatcherDisplayName" character varying(256) NOT NULL,
                "CallerConnectionId" character varying(128) NOT NULL,
                "DispatcherConnectionId" character varying(128),
                "Status" character varying(32) NOT NULL,
                "RequestedAtUtc" timestamp with time zone NOT NULL,
                "ExpiresAtUtc" timestamp with time zone NOT NULL,
                "AcceptedAtUtc" timestamp with time zone,
                "EndedAtUtc" timestamp with time zone,
                "DurationSeconds" bigint,
                "ResolvedAtUtc" timestamp with time zone,
                "ResolvedByUserId" character varying(128),
                "ResolvedByConnectionId" character varying(128),
                "ResolveReason" character varying(512),
                "EndedByUserId" character varying(128),
                "EndedByConnectionId" character varying(128),
                "EndReason" character varying(512),
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_CallHistory" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_CallHistory_CallId"
                ON call."CallHistory" ("CallId");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_CallHistory_RoomId"
                ON call."CallHistory" ("RoomId");

            CREATE INDEX IF NOT EXISTS "IX_CallHistory_CallerUserId"
                ON call."CallHistory" ("CallerUserId");

            CREATE INDEX IF NOT EXISTS "IX_CallHistory_DispatcherUserId"
                ON call."CallHistory" ("DispatcherUserId");

            CREATE INDEX IF NOT EXISTS "IX_CallHistory_Status"
                ON call."CallHistory" ("Status");

            CREATE INDEX IF NOT EXISTS "IX_CallHistory_RequestedAtUtc"
                ON call."CallHistory" ("RequestedAtUtc");
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
