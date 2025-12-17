using EquipmentStatusTracker.WPF.Models;

namespace EquipmentStatusTracker.WPF.Services;

/// <summary>
/// Interface for project persistence operations.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Saves the project to a file.
    /// </summary>
    void SaveStatus(ProjectData project, string filePath);

    /// <summary>
    /// Loads a project from a file.
    /// </summary>
    ProjectData? LoadStatus(string filePath);

    /// <summary>
    /// Auto-saves the project to the application data folder.
    /// </summary>
    void AutoSave(ProjectData project);

    /// <summary>
    /// Loads the auto-saved project.
    /// </summary>
    ProjectData? LoadAutoSave();

    /// <summary>
    /// Exports change history to a CSV file.
    /// </summary>
    void ExportHistory(List<HistoryEntry> history, string filePath);

    /// <summary>
    /// Exports equipment list to a CSV file.
    /// </summary>
    void ExportEquipmentCsv(List<Equipment> equipment, List<Layer> layers, string filePath);

    /// <summary>
    /// Creates a demo project with sample equipment.
    /// </summary>
    ProjectData CreateDemoProject();
}
