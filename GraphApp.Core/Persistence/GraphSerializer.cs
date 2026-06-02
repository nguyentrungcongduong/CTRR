using System.Text.Json;
using System.Text.Json.Serialization;
using GraphApp.Core.Models;

namespace GraphApp.Core.Persistence;

/// <summary>
/// Lưu và đọc đồ thị theo định dạng JSON (.graph.json).
/// Sử dụng System.Text.Json (.NET 8 built-in) — không cần NuGet thêm.
/// </summary>
public static class GraphSerializer
{
    // File extension được dùng cho SaveFileDialog / OpenFileDialog
    public const string FileExtension = ".graph.json";
    public const string FileFilter    = "Graph files (*.graph.json)|*.graph.json|JSON files (*.json)|*.json|All files (*.*)|*.*";

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ─── Save ─────────────────────────────────────────────────────────

    /// <summary>
    /// Lưu đồ thị ra file JSON.
    /// Ném <see cref="IOException"/> nếu không ghi được file.
    /// </summary>
    public static void Save(Graph graph, string filePath)
    {
        var dto = new GraphDto
        {
            Version  = 1,
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
        File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
    }

    // ─── Load ─────────────────────────────────────────────────────────

    /// <summary>
    /// Đọc đồ thị từ file JSON.
    /// Trả về <c>null</c> nếu file không tồn tại hoặc không hợp lệ.
    /// Ném <see cref="JsonException"/> nếu JSON sai định dạng nghiêm trọng.
    /// </summary>
    public static Graph? Load(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        var dto  = JsonSerializer.Deserialize<GraphDto>(json, _options);
        if (dto?.Nodes == null || dto.Edges == null) return null;

        var graph = new Graph { Directed = dto.Directed };

        // Restore nodes trực tiếp (không qua AddNode để giữ nguyên Id + Position)
        foreach (var n in dto.Nodes)
        {
            graph.Nodes.Add(new Node
            {
                Id       = n.Id,
                Label    = string.IsNullOrWhiteSpace(n.Label) ? $"N{n.Id}" : n.Label,
                Position = new System.Drawing.PointF(n.X, n.Y)
            });
        }

        // Restore edges trực tiếp
        foreach (var e in dto.Edges)
        {
            // Chỉ thêm cạnh hợp lệ (source/target phải tồn tại)
            bool srcOk = graph.Nodes.Any(n => n.Id == e.Source);
            bool tgtOk = graph.Nodes.Any(n => n.Id == e.Target);
            if (!srcOk || !tgtOk) continue;

            graph.Edges.Add(new Edge
            {
                Id     = e.Id,
                Source = e.Source,
                Target = e.Target,
                Weight = e.Weight
            });
        }

        // ⚠️ Quan trọng: cập nhật lại bộ đếm Id để tránh conflict khi thêm mới
        graph.RestoreCountersFromData();

        return graph;
    }

    // ─── DTOs ─────────────────────────────────────────────────────────

    private class GraphDto
    {
        public int          Version  { get; set; } = 1;
        public bool         Directed { get; set; }
        public List<NodeDto> Nodes   { get; set; } = new();
        public List<EdgeDto> Edges   { get; set; } = new();
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
