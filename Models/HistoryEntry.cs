namespace EquipmentStatusTracker.WPF.Models;

public class HistoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EquipmentId { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string FromPosition { get; set; } = string.Empty;
    public string ToPosition { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

