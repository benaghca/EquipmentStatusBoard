using System.IO;
using System.Xml.Linq;
using EquipmentStatusTracker.WPF.Models;

namespace EquipmentStatusTracker.WPF.Services;

public class DrawioImporter
{
    public ProjectData Import(string filePath)
    {
        var content = File.ReadAllText(filePath);
        return ParseDrawioContent(content, Path.GetFileName(filePath));
    }
    
    public ProjectData ParseDrawioContent(string xmlContent, string fileName)
    {
        var project = new ProjectData
        {
            ProjectName = Path.GetFileNameWithoutExtension(fileName)
        };
        
        try
        {
            var doc = XDocument.Parse(xmlContent);
            var root = doc.Root;
            
            var objects = root?.Descendants("object").ToList() ?? new List<XElement>();
            var cells = root?.Descendants("mxCell").ToList() ?? new List<XElement>();
            
            int index = 0;
            
            foreach (var obj in objects)
            {
                var equipment = ParseEquipmentFromObject(obj, index++);
                if (equipment != null)
                {
                    project.Equipment.Add(equipment);
                }
            }
            
            foreach (var cell in cells)
            {
                if (HasEquipmentAttributes(cell))
                {
                    var equipment = ParseEquipmentFromCell(cell, index++);
                    if (equipment != null && !project.Equipment.Any(e => e.Id == equipment.Id))
                    {
                        project.Equipment.Add(equipment);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing drawio file: {ex.Message}");
        }
        
        return project;
    }
    
    private bool HasEquipmentAttributes(XElement element)
    {
        var typeAttr = element.Attribute("equipment_type") ?? element.Attribute("equipmentType");
        return typeAttr != null;
    }
    
    private Equipment? ParseEquipmentFromObject(XElement obj, int index)
    {
        var typeAttr = obj.Attribute("equipment_type") ?? obj.Attribute("equipmentType") ?? obj.Attribute("type");
        if (typeAttr == null) return null;
        
        var typeString = typeAttr.Value.ToLowerInvariant();
        var validTypes = new[] { "valve", "breaker", "pump", "chiller", "generator", "switch", "motor", "ats", "ups", "pdu", "transformer" };
        
        if (!validTypes.Any(t => typeString.Contains(t)))
            return null;
        
        var id = obj.Attribute("id")?.Value ?? $"equipment-{index}";
        var name = obj.Attribute("equipment_name")?.Value ?? 
                   obj.Attribute("equipmentName")?.Value ?? 
                   obj.Attribute("label")?.Value ?? 
                   $"{typeString}-{index}";
        
        var normalPosition = obj.Attribute("normal_position")?.Value ?? 
                            obj.Attribute("normalPosition")?.Value ?? 
                            Equipment.GetDefaultNormalPosition(Equipment.ParseType(typeString));
        
        var cell = obj.Element("mxCell");
        var geometry = cell?.Element("mxGeometry");
        
        double x = 0, y = 0, width = 50, height = 50;
        if (geometry != null)
        {
            double.TryParse(geometry.Attribute("x")?.Value, out x);
            double.TryParse(geometry.Attribute("y")?.Value, out y);
            double.TryParse(geometry.Attribute("width")?.Value ?? "50", out width);
            double.TryParse(geometry.Attribute("height")?.Value ?? "50", out height);
        }
        
        return new Equipment
        {
            Id = id,
            Name = name,
            Type = Equipment.ParseType(typeString),
            NormalPosition = normalPosition,
            CurrentPosition = normalPosition,
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }
    
    private Equipment? ParseEquipmentFromCell(XElement cell, int index)
    {
        var typeAttr = cell.Attribute("equipment_type") ?? cell.Attribute("equipmentType");
        if (typeAttr == null) return null;
        
        var typeString = typeAttr.Value.ToLowerInvariant();
        
        var id = cell.Attribute("id")?.Value ?? $"equipment-{index}";
        var name = cell.Attribute("equipment_name")?.Value ?? 
                   cell.Attribute("equipmentName")?.Value ?? 
                   cell.Attribute("label")?.Value ?? 
                   cell.Attribute("value")?.Value ??
                   $"{typeString}-{index}";
        
        var normalPosition = cell.Attribute("normal_position")?.Value ?? 
                            cell.Attribute("normalPosition")?.Value ?? 
                            Equipment.GetDefaultNormalPosition(Equipment.ParseType(typeString));
        
        var geometry = cell.Element("mxGeometry");
        double x = 0, y = 0, width = 50, height = 50;
        if (geometry != null)
        {
            double.TryParse(geometry.Attribute("x")?.Value, out x);
            double.TryParse(geometry.Attribute("y")?.Value, out y);
            double.TryParse(geometry.Attribute("width")?.Value ?? "50", out width);
            double.TryParse(geometry.Attribute("height")?.Value ?? "50", out height);
        }
        
        return new Equipment
        {
            Id = id,
            Name = name,
            Type = Equipment.ParseType(typeString),
            NormalPosition = normalPosition,
            CurrentPosition = normalPosition,
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }
}

