namespace GraphApp.Core.Algorithms.Base;

/// <summary>
/// Data class đại diện cho một "khung" (frame) trong quá trình animation thuật toán.
/// UI sẽ đọc object này để tô màu đỉnh/cạnh tương ứng.
/// </summary>
public class AlgorithmStep
{
    /// <summary>Mô tả hành động ở bước này (hiển thị cho người dùng)</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Tập các đỉnh đã được thăm (tô màu xanh lá)</summary>
    public HashSet<int> VisitedNodes { get; set; } = new();

    /// <summary>Tập các đỉnh đang xét tại bước này (tô màu đỏ)</summary>
    public HashSet<int> ActiveNodes { get; set; } = new();

    /// <summary>Các cạnh được highlight (MST / đường đi ngắn nhất / augmented path)</summary>
    public HashSet<int> HighlightEdges { get; set; } = new();

    /// <summary>Các đỉnh đang nằm trong Queue (BFS) hoặc Stack (DFS) — tô màu cam</summary>
    public HashSet<int> QueueOrStack { get; set; } = new();

    /// <summary>
    /// Nhãn phụ của từng đỉnh (VD: khoảng cách Dijkstra, nhóm A/B của Bipartite).
    /// Key = NodeId, Value = chuỗi nhãn
    /// </summary>
    public Dictionary<int, string> NodeLabels { get; set; } = new();

    /// <summary>
    /// Loại bước, dùng để UI quyết định cách tô màu đặc biệt.
    /// VD: "visit", "add_mst", "augment", "skip_edge", "done", ...
    /// </summary>
    public string StepType { get; set; } = string.Empty;
}
