using GraphApp.Core.Models;

namespace GraphApp.Core.Converters;

/// <summary>
/// Chuyển đổi qua lại giữa 3 dạng biểu diễn đồ thị:
/// Ma trận kề (Adjacency Matrix) ↔ Danh sách kề (Adjacency List) ↔ Danh sách cạnh (Edge List).
/// Hỗ trợ cả đồ thị có hướng và vô hướng.
/// </summary>
public static class GraphConverter
{
    // ─── Graph → Representations ──────────────────────────────────────

    /// <summary>
    /// Chuyển Graph sang ma trận kề.
    /// Trả về (matrix[n,n], labels[n]) — trọng số 0 = không có cạnh.
    /// </summary>
    public static (double[,] Matrix, string[] Labels) ToAdjMatrix(Graph graph)
    {
        // TODO: TASK-10
        throw new NotImplementedException("ToAdjMatrix — sẽ implement ở TASK-10.");
    }

    /// <summary>
    /// Chuyển Graph sang danh sách kề.
    /// Key = label đỉnh, Value = danh sách (label kề, trọng số).
    /// </summary>
    public static Dictionary<string, List<(string Neighbor, double Weight)>> ToAdjList(Graph graph)
    {
        // TODO: TASK-10
        throw new NotImplementedException("ToAdjList — sẽ implement ở TASK-10.");
    }

    /// <summary>
    /// Chuyển Graph sang danh sách cạnh.
    /// Mỗi tuple: (label nguồn, label đích, trọng số).
    /// </summary>
    public static List<(string Source, string Target, double Weight)> ToEdgeList(Graph graph)
    {
        // TODO: TASK-10
        throw new NotImplementedException("ToEdgeList — sẽ implement ở TASK-10.");
    }

    // ─── Representations → Graph ──────────────────────────────────────

    /// <summary>
    /// Tạo Graph từ ma trận kề.
    /// </summary>
    public static Graph FromAdjMatrix(double[,] matrix, string[] labels, bool directed = false)
    {
        // TODO: TASK-10
        throw new NotImplementedException("FromAdjMatrix — sẽ implement ở TASK-10.");
    }

    /// <summary>
    /// Tạo Graph từ danh sách kề.
    /// </summary>
    public static Graph FromAdjList(
        Dictionary<string, List<(string Neighbor, double Weight)>> adjList,
        bool directed = false)
    {
        // TODO: TASK-10
        throw new NotImplementedException("FromAdjList — sẽ implement ở TASK-10.");
    }

    /// <summary>
    /// Tạo Graph từ danh sách cạnh.
    /// </summary>
    public static Graph FromEdgeList(
        List<(string Source, string Target, double Weight)> edgeList,
        bool directed = false)
    {
        // TODO: TASK-10
        throw new NotImplementedException("FromEdgeList — sẽ implement ở TASK-10.");
    }
}
