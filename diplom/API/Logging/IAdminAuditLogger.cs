namespace API.Logging
{
    public interface IAdminAuditLogger
    {
        void Log(
            string action,
            int? userId = null,
            string? entityType = null,
            int? entityId = null,
            bool success = true,
            string? details = null);
    }
}