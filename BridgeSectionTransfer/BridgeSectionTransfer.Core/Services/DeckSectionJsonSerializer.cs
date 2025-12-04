using System.Text.Json;
using System.Text.Json.Serialization;
using BridgeSectionTransfer.Core.Models;

namespace BridgeSectionTransfer.Core.Services;

public class DeckSectionJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void SerializeToFile(DeckSection section, string filePath)
    {
        var data = new BridgeDeckSectionsData
        {
            ExportInfo = new ExportInfo
            {
                Date = DateTime.Now,
                Tool = "BridgeSectionTransfer",
                Version = "1.0.0",
                Units = "Meters",
                CoordinateSystem = "Local"
            },
            Sections = new List<DeckSectionDto> { MapToDto(section) }
        };

        string json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(filePath, json);
    }

    public void SerializeMultipleToFile(List<DeckSection> sections, string filePath)
    {
        var data = new BridgeDeckSectionsData
        {
            ExportInfo = new ExportInfo
            {
                Date = DateTime.Now,
                Tool = "BridgeSectionTransfer",
                Version = "1.0.0",
                Units = "Meters",
                CoordinateSystem = "Local"
            },
            Sections = sections.Select(MapToDto).ToList()
        };

        string json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(filePath, json);
    }

    public DeckSection DeserializeFromFile(string filePath)
    {
        string json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<BridgeDeckSectionsData>(json, Options);

        if (data?.Sections == null || data.Sections.Count == 0)
            throw new InvalidOperationException("No sections found in JSON file");

        return MapFromDto(data.Sections[0]);
    }

    public List<DeckSection> DeserializeMultipleFromFile(string filePath)
    {
        string json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<BridgeDeckSectionsData>(json, Options);

        if (data?.Sections == null || data.Sections.Count == 0)
            throw new InvalidOperationException("No sections found in JSON file");

        return data.Sections.Select(MapFromDto).ToList();
    }

    private DeckSectionDto MapToDto(DeckSection section)
    {
        return new DeckSectionDto
        {
            Name = section.Name,
            Station = section.Station,
            Area = section.Area,
            Centroid = new Point2DDto { X = section.Centroid.X, Y = section.Centroid.Y },
            ReferencePoint = new ReferencePointDto
            {
                X = section.ReferencePoint.X,
                Y = section.ReferencePoint.Y,
                Description = section.ReferencePoint.Description
            },
            Material = new MaterialPropertiesDto
            {
                ConcreteStrength = section.Material.ConcreteStrength,
                Density = section.Material.Density,
                ElasticModulus = section.Material.ElasticModulus
            },
            ExteriorBoundary = section.ExteriorBoundary.Points
                .Select(p => new Point2DDto { X = p.X, Y = p.Y })
                .ToList(),
            InteriorVoids = section.InteriorVoids
                .Select(v => new VoidDto
                {
                    Name = v.Name,
                    Points = v.Points.Select(p => new Point2DDto { X = p.X, Y = p.Y }).ToList()
                })
                .ToList()
        };
    }

    private DeckSection MapFromDto(DeckSectionDto dto)
    {
        return new DeckSection
        {
            Name = dto.Name,
            Station = dto.Station,
            Area = dto.Area,
            Centroid = new Point2D(dto.Centroid.X, dto.Centroid.Y),
            ReferencePoint = new ReferencePoint
            {
                X = dto.ReferencePoint.X,
                Y = dto.ReferencePoint.Y,
                Description = dto.ReferencePoint.Description
            },
            Material = new MaterialProperties
            {
                ConcreteStrength = dto.Material.ConcreteStrength,
                Density = dto.Material.Density,
                ElasticModulus = dto.Material.ElasticModulus
            },
            ExteriorBoundary = new Polygon
            {
                Name = "Exterior",
                Type = PolygonType.Solid,
                Points = dto.ExteriorBoundary
                    .Select(p => new Point2D(p.X, p.Y))
                    .ToList()
            },
            InteriorVoids = dto.InteriorVoids
                .Select(v => new Polygon
                {
                    Name = v.Name,
                    Type = PolygonType.Opening,
                    Points = v.Points.Select(p => new Point2D(p.X, p.Y)).ToList()
                })
                .ToList()
        };
    }
}
