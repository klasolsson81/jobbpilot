namespace JobbPilot.Migrate;

// Postgres-rolnamn — hardcoded const per CLAUDE.md §5.1 (magic strings förbjudna).
// Per ADR 0034: jobbpilot_app är runtime-roll (aldrig CREATE ON DATABASE),
// jobbpilot_migrations äger schemas, jobbpilot_worker är DML-only på hangfire.
internal static class Roles
{
    public const string Migrations = "jobbpilot_migrations";
    public const string App = "jobbpilot_app";
    public const string Worker = "jobbpilot_worker";
}
