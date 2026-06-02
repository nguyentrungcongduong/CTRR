namespace GraphApp.Core.Algorithms.Base;

/// <summary>
/// Data class đại diện cho một "khung" (frame) trong quá trình animation thuật toán.
/// UI sẽ đọc object này để tô màu đỉnh/cạnh tương ứng sau mỗi bước.
/// </summary>
/// <remarks>
/// Quy ước màu (theo ARCHITECTURE.md):
/// <list type="bullet">
///   <item>VisitedNodes  → xanh lá #27AE60</item>
///   <item>ActiveNodes   → đỏ     #E74C3C</item>
///   <item>QueueOrStack  → cam    #F39C12</item>
///   <item>HighlightEdges→ tím    #8E44AD (MST/path) | cam đậm #E67E22 (augmented)</item>
/// </list>
/// </remarks>
public class AlgorithmStep
{
    /// <summary>Mô tả hành động ở bước này — hiển thị cho người dùng (tiếng Việt).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tập các đỉnh đã được thăm (tô màu xanh lá).
    /// Một khi đỉnh vào tập này thì giữ màu xanh cho đến hết animation.
    /// </summary>
    public HashSet<int> VisitedNodes { get; set; } = new();

    /// <summary>
    /// Tập các đỉnh đang được xét tại bước này (tô màu đỏ).
    /// Thường chỉ có 1 đỉnh tại một thời điểm.
    /// </summary>
    public HashSet<int> ActiveNodes { get; set; } = new();

    /// <summary>
    /// Các cạnh được tô màu đặc biệt:
    /// - MST edges (Prim/Kruskal): màu tím
    /// - Shortest path (Dijkstra): màu tím
    /// - Augmented path (Ford-Fulkerson): màu cam đậm
    /// - Euler path (Fleury/Hierholzer): màu tím theo thứ tự
    /// </summary>
    public HashSet<int> HighlightEdges { get; set; } = new();

    /// <summary>
    /// Các đỉnh đang nằm trong Queue (BFS) hoặc Stack (DFS) — tô màu cam.
    /// Phân biệt với VisitedNodes để thể hiện cấu trúc dữ liệu đang dùng.
    /// </summary>
    public HashSet<int> QueueOrStack { get; set; } = new();

    /// <summary>
    /// Nhãn phụ hiển thị dưới mỗi đỉnh trên canvas.
    /// Key = NodeId, Value = chuỗi nhãn.
    /// Ví dụ:
    /// - Dijkstra:   { 1: "∞", 2: "5", 3: "3" }    ← khoảng cách
    /// - Bipartite:  { 1: "A", 2: "B" }             ← nhóm
    /// - Prim:       { 1: "key=2", 2: "key=∞" }     ← key value
    /// - Ford-Fulkerson: { edgeId: "2/5" }          ← flow/capacity (dùng EdgeLabels)
    /// </summary>
    public Dictionary<int, string> NodeLabels { get; set; } = new();

    /// <summary>
    /// Nhãn phụ hiển thị trên cạnh (dùng cho Ford-Fulkerson: "flow/capacity").
    /// Key = EdgeId, Value = chuỗi nhãn.
    /// </summary>
    public Dictionary<int, string> EdgeLabels { get; set; } = new();

    /// <summary>
    /// Cạnh bị loại bỏ (tô màu đỏ nhạt) — dùng cho Kruskal khi bỏ qua cạnh tạo chu trình.
    /// </summary>
    public HashSet<int> RejectedEdges { get; set; } = new();

    /// <summary>
    /// Cạnh đang được xét tại bước này (tô màu vàng) — dùng cho Fleury kiểm tra cầu.
    /// </summary>
    public HashSet<int> ConsideredEdges { get; set; } = new();

    /// <summary>
    /// Loại bước — dùng để UI quyết định cách hiển thị đặc biệt.
    /// Các giá trị có thể:
    /// "init", "visit", "dequeue", "enqueue", "already_visited",
    /// "add_mst", "update_key", "select_min_edge",
    /// "find_path", "augment_flow", "update_residual", "no_path",
    /// "consider_edge", "skip_edge",
    /// "choose_start", "check_bridge", "move",
    /// "start_circuit", "extend_path", "merge_circuit",
    /// "result_true", "result_false", "done"
    /// </summary>
    public string StepType { get; set; } = string.Empty;

    // ─── Factory helpers ───────────────────────────────────────────────

    /// <summary>Tạo step đơn giản chỉ có Description và StepType.</summary>
    public static AlgorithmStep Create(string description, string stepType = "visit") =>
        new() { Description = description, StepType = stepType };

    /// <summary>Tạo bản sao của step hiện tại (để tích lũy trạng thái qua các bước).</summary>
    public AlgorithmStep DeepCopy() => new()
    {
        Description    = Description,
        StepType       = StepType,
        VisitedNodes   = new HashSet<int>(VisitedNodes),
        ActiveNodes    = new HashSet<int>(ActiveNodes),
        HighlightEdges = new HashSet<int>(HighlightEdges),
        QueueOrStack   = new HashSet<int>(QueueOrStack),
        RejectedEdges  = new HashSet<int>(RejectedEdges),
        ConsideredEdges= new HashSet<int>(ConsideredEdges),
        NodeLabels     = new Dictionary<int, string>(NodeLabels),
        EdgeLabels     = new Dictionary<int, string>(EdgeLabels),
    };
}
