using System.Text.Json;
using System.Text.Json.Serialization;
using GraphApp.Core.Models;

namespace GraphApp.Core.Persistence;

/// <summary>
/// Lưu và đọc đồ thị theo định dạng JSON.
/// Sử dụng System.Text.Json (.NET 8 built-in).
/// </summary>
public static class GraphSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Lưu đồ thị ra file JSON.</summary>
    public static void Save(Graph graph, string filePath)
    {
        var dto = new GraphDto
        {
            Directed = graph.Directed,
            Nodes    = graph.Nodes.Select(n => new NodeDto
            {
                Id    = n.Id,
                Label = n.Label,
                X     = n.Position.X,
                Y     = n.Position.Y
            }).ToList(),
            Edges = graph.Edges.Select(e => new EdgeDto
            {
                Id     = e.Id,
                Source = e.Source,
                Target = e.Target,
                Weight = e.Weight
            }).ToList()
        };

        var json = JsonSerializer.Serialize(dto, _options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>Đọc đồ thị từ file JSON. Trả về null nếu file không hợp lệ.</summary>
    public static Graph? Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var dto  = JsonSerializer.Deserialize<GraphDto>(json, _options);
        if (dto is null) return null;

        var graph = new Graph { Directed = dto.Directed };

        foreach (var n in dto.Nodes)
        {
            graph.Nodes.Add(new Node
            {
                Id       = n.Id,
                Label    = n.Label,
                Position = new System.Drawing.PointF(n.X, n.Y)
            });
        }

        foreach (var e in dto.Edges)
        {
            graph.Edges.Add(new Edge
            {
                Id     = e.Id,
                Source = e.Source,
                Target = e.Target,
                Weight = e.Weight
            });
        }

        return graph;
    }

    // ─── DTOs (chỉ dùng nội bộ cho JSON) ──────────────────────────────

    private class GraphDto
    {
        public bool          Directed { get; set; }
        public List<NodeDto> Nodes    { get; set; } = new();
        public List<EdgeDto> Edges    { get; set; } = new();
    }

    private class NodeDto
    {
        public int    Id    { get; set; }
        public string Label { get; set; } = string.Empty;
        public float  X     { get; set; }
        public float  Y     { get; set; }
    }

    private class EdgeDto
    {
        public int    Id     { get; set; }
        public int    Source { get; set; }
        public int    Target { get; set; }
        public double Weight { get; set; }
    }
}
