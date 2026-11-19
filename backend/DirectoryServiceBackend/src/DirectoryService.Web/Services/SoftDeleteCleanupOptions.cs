namespace DirectoryService.Web.Services;

public sealed class SoftDeleteCleanupOptions
{
    public bool Enabled { get; set; } = true;
    public int RetentionDays { get; set; } = 30;
    public int IntervalMinutes { get; set; } = 1440;
}
