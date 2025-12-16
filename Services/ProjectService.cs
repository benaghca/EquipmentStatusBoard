using System.IO;
using Newtonsoft.Json;
using EquipmentStatusTracker.WPF.Models;

namespace EquipmentStatusTracker.WPF.Services;

public class ProjectService
{
    private const string AutoSaveFileName = "autosave.json";
    
    public void SaveStatus(ProjectData project, string filePath)
    {
        var json = JsonConvert.SerializeObject(project, Formatting.Indented);
        File.WriteAllText(filePath, json);
        project.LastSaved = DateTime.Now;
    }
    
    public ProjectData? LoadStatus(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        
        var json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<ProjectData>(json);
    }
    
    public void AutoSave(ProjectData project)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var saveDir = Path.Combine(appData, "Adjutant");
        Directory.CreateDirectory(saveDir);

        var savePath = Path.Combine(saveDir, AutoSaveFileName);
        SaveStatus(project, savePath);
    }

    public ProjectData? LoadAutoSave()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var savePath = Path.Combine(appData, "Adjutant", AutoSaveFileName);
        return LoadStatus(savePath);
    }
    
    public void ExportHistory(List<HistoryEntry> history, string filePath)
    {
        var lines = new List<string> { "Timestamp,Equipment,From,To" };

        lines.AddRange(history.Select(h =>
            $"\"{h.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{h.EquipmentName}\",\"{h.FromPosition}\",\"{h.ToPosition}\""
        ));

        File.WriteAllLines(filePath, lines);
    }

    public void ExportEquipmentCsv(List<Equipment> equipment, List<Layer> layers, string filePath)
    {
        var header = "Name,Type,Status,Current Position,Normal Position,Energized,LOTO,Layer,Notes,Last Updated";
        var lines = new List<string> { header };

        foreach (var eq in equipment)
        {
            var layerName = layers.FirstOrDefault(l => l.Id == eq.LayerId)?.Name ?? "Default";
            var notes = (eq.Notes ?? "").Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
            var lastUpdated = eq.LastUpdated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";

            lines.Add($"\"{eq.Name}\",\"{eq.Type}\",\"{eq.Status}\",\"{eq.CurrentPosition}\"," +
                      $"\"{eq.NormalPosition}\",\"{(eq.IsEnergized ? "Yes" : "No")}\"," +
                      $"\"{(eq.IsLOTO ? "Yes" : "No")}\",\"{layerName}\",\"{notes}\",\"{lastUpdated}\"");
        }

        File.WriteAllLines(filePath, lines);
    }

    public static ProjectData CreateDemoProject()
    {
        var project = new ProjectData
        {
            ProjectName = "Demo - Data Center Mechanical Room"
        };
        
        var demoEquipment = new (string id, string name, EquipmentType type, string normalPos, double x, double y, double w, double h)[]
        {
            ("chw-v-001", "CHW-V-001", EquipmentType.Valve, "open", 100, 200, 50, 50),
            ("chw-v-002", "CHW-V-002", EquipmentType.Valve, "open", 300, 200, 50, 50),
            ("chw-v-003", "CHW-V-003", EquipmentType.Valve, "closed", 500, 200, 50, 50),
            ("chw-p-001", "CHW-P-001", EquipmentType.Pump, "on", 200, 320, 60, 60),
            ("chw-p-002", "CHW-P-002", EquipmentType.Pump, "off", 400, 320, 60, 60),
            ("ch-001", "CHILLER-001", EquipmentType.Chiller, "on", 280, 80, 100, 70),
            ("msb-001", "MSB-001", EquipmentType.Breaker, "closed", 620, 80, 70, 50),
            ("msb-002", "MSB-002", EquipmentType.Breaker, "closed", 620, 160, 70, 50),
            ("msb-003", "MSB-003", EquipmentType.Breaker, "open", 620, 240, 70, 50),
            ("gen-001", "GEN-001", EquipmentType.Generator, "off", 750, 130, 80, 80),
            ("ats-001", "ATS-001", EquipmentType.ATS, "normal", 720, 260, 70, 60),
            ("ups-001", "UPS-001", EquipmentType.UPS, "on", 100, 420, 70, 60),
        };
        
        foreach (var (id, name, type, normalPos, x, y, w, h) in demoEquipment)
        {
            project.Equipment.Add(new Equipment
            {
                Id = id,
                Name = name,
                Type = type,
                NormalPosition = normalPos,
                CurrentPosition = normalPos,
                X = x,
                Y = y,
                Width = w,
                Height = h
            });
        }

        // Simulate some abnormal conditions
        var abnormalIds = new[] { "chw-v-003", "chw-p-002", "msb-003" };
        foreach (var eq in project.Equipment.Where(e => abnormalIds.Contains(e.Id)))
        {
            eq.CurrentPosition = GetOppositePosition(eq.NormalPosition);
        }
        
        return project;
    }
    
    private static string GetOppositePosition(string position)
    {
        return position.ToLower() switch
        {
            "open" => "closed",
            "closed" => "open",
            "on" => "off",
            "off" => "on",
            "normal" => "emergency",
            "emergency" => "normal",
            "energized" => "de-energized",
            "de-energized" => "energized",
            _ => position
        };
    }
}

