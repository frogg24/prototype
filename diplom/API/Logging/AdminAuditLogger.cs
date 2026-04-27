using NLog;

namespace API.Logging
{
    public class AdminAuditLogger : IAdminAuditLogger
    {
        private static readonly Logger Logger = LogManager.GetLogger("AdminAudit");
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminAuditLogger(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Log(
            string action,
            int? userId = null,
            string? entityType = null,
            int? entityId = null,
            bool success = true,
            string? details = null)
        {
            var logEvent = new LogEventInfo(NLog.LogLevel.Info, Logger.Name, "Admin audit event");

            logEvent.Properties["Action"] = action;
            logEvent.Properties["UserId"] = userId;
            logEvent.Properties["EntityType"] = entityType;
            logEvent.Properties["EntityId"] = entityId;
            logEvent.Properties["Success"] = success;
            logEvent.Properties["Details"] = details;

            Logger.Log(logEvent);
        }
    }
}