# Graph Application — Architecture Design
> WinForms C# | GDI+ Visualization | Step-by-step Algorithm Animation

---

## 1. Tổng quan hệ thống

```
GraphApp (Solution)
├── GraphApp.Core          ← Class library: model + algorithms
├── GraphApp.UI            ← WinForms project: canvas + forms
└── GraphApp.Tests         ← (optional) Unit tests
```

Tách `Core` và `UI` thành 2 project riêng giúp test thuật toán độc lập, không phụ thuộc WinForms.

---

## 2. Cấu trúc thư mục chi tiết

```
GraphApp/
│
├── GraphApp.Core/
│   ├── Models/
│   │   ├── Node.cs                  ← Đỉnh đồ thị
│   │   ├── Edge.cs                  ← Cạnh đồ thị
│   │   └── Graph.cs                 ← Model chính, chứa Nodes + Edges
│   │
│   ├── Converters/
│   │   └── GraphConverter.cs        ← adj matrix ↔ adj list ↔ edge list
│   │
│   ├── Algorithms/
│   │   ├── Base/
│   │   │   ├── AlgorithmStep.cs     ← Data class: 1 frame animation
│   │   │   └── IGraphAlgorithm.cs   ← Interface chung
│   │   ├── Traversal/
│   │   │   ├── BFS.cs
│   │   │   └── DFS.cs
│   │   ├── ShortestPath/
│   │   │   └── Dijkstra.cs
│   │   ├── Properties/
│   │   │   └── BipartiteChecker.cs
│   │   └── Advanced/
│   │       ├── Prim.cs
│   │       ├── Kruskal.cs
│   │       ├── FordFulkerson.cs
│   │       ├── Fleury.cs
│   │       └── Hierholzer.cs
│   │
│   └── Persistence/
│       └── GraphSerializer.cs       ← Save/Load JSON
│
└── GraphApp.UI/
    ├── Controls/
    │   ├── GraphCanvas.cs           ← UserControl: vẽ GDI+ + mouse events
    │   ├── AnimationEngine.cs       ← Timer + step navigation
    │   └── RepresentationPanel.cs   ← Hiển thị 3 dạng biểu diễn
    │
    ├── Forms/
    │   ├── MainForm.cs              ← Form chính
    │   ├── MainForm.Designer.cs
    │   ├── InputMatrixForm.cs       ← Nhập adj matrix dạng DataGridView
    │   └── EdgeWeightDialog.cs      ← Dialog nhập trọng số khi thêm edge
    │
    └── Helpers/
        └── DrawingHelper.cs         ← Vẽ mũi tên, tính toán geometry
```

---

## 3. Models

### 3.1 `Node.cs`
```csharp
public class Node
{
    public int     Id       { get; set; }
    public string  Label    { get; set; }  // tên hiển thị
    public PointF  Position { get; set; }  // tọa độ trên canvas
}
```

### 3.2 `Edge.cs`
```csharp
public class Edge
{
    public int    Id     { get; set; }
    public int    Source { get; set; }  // Node.Id
    public int    Target { get; set; }  // Node.Id
    public double Weight { get; set; }
}
```

### 3.3 `Graph.cs` — trách nhiệm
| Method | Mô tả |
|--------|-------|
| `AddNode(x, y, label?)` | Thêm đỉnh, tự tăng Id |
| `RemoveNode(id)` | Xóa đỉnh + tất cả cạnh liên quan |
| `AddEdge(src, tgt, weight?)` | Thêm cạnh |
| `RemoveEdge(id)` | Xóa cạnh theo Id |
| `Neighbors(nodeId)` | Trả về danh sách (NodeId, EdgeId, Weight) kề |
| `Clone()` | Deep copy — dùng cho animation |
| `Clear()` | Xóa toàn bộ |

---

## 4. AlgorithmStep — data contract giữa Core và UI

```csharp
public class AlgorithmStep
{
    public string           Description   { get; set; }  // text hiển thị
    public HashSet<int>     VisitedNodes  { get; set; }  // tô màu xanh
    public HashSet<int>     ActiveNodes   { get; set; }  // tô màu đỏ (đang xét)
    public HashSet<int>     HighlightEdges{ get; set; }  // MST / path edges
    public HashSet<int>     QueueOrStack  { get; set; }  // nodes trong queue/stack
    public Dictionary<int,string> NodeLabels { get; set; } // dist/key label phụ
    public string           StepType      { get; set; }  // "visit","add_mst","augment",...
}
```

> **Rule**: Mọi thuật toán đều trả về `List<AlgorithmStep>`. UI không biết gì về logic thuật toán.

---

## 5. GraphConverter — 6 chiều chuyển đổi

```
Graph ──► AdjMatrix    Graph ──► AdjList    Graph ──► EdgeList
AdjMatrix ──► Graph    AdjList ──► Graph    EdgeList ──► Graph
```

Input/Output:
- **AdjMatrix**: `int[,]` + `string[] labels`
- **AdjList**: `Dictionary<string, List<(string neighbor, double weight)>>`
- **EdgeList**: `List<(string src, string tgt, double weight)>`

---

## 6. GraphCanvas — luồng sự kiện chuột

```
MouseDown
  ├── Mode = AddNode  → tạo node tại vị trí click
  ├── Mode = AddEdge  → chọn node 1, click node 2 → tạo edge
  ├── Mode = Delete   → click node/edge → xóa
  └── Mode = Select   → bắt đầu drag node

MouseMove
  └── đang drag → cập nhật node.Position → Invalidate()

MouseUp
  └── kết thúc drag
```

---

## 7. AnimationEngine — state machine

```
[Idle] ──Load(steps)──► [Ready]
[Ready] ──Play()──► [Playing] ──Pause()──► [Paused]
[Playing] ──cuối steps──► [Done]
[Paused/Done] ──Next()/Prev()──► cập nhật frame thủ công
```

Speed control: `Timer.Interval` = `1200 - speedSlider.Value * 10` (ms)

---

## 8. Màu sắc quy ước

| Trạng thái | Màu node | Màu cạnh |
|-----------|----------|----------|
| Default | `#4A90D9` (xanh dương) | `Gray` 1.5px |
| Visited | `#27AE60` (xanh lá) | — |
| Active (đang xét) | `#E74C3C` (đỏ) | — |
| In Queue/Stack | `#F39C12` (cam) | — |
| MST / Shortest path | — | `#8E44AD` (tím) 3px |
| Augmented (Ford-Fulkerson) | — | `#E67E22` (cam đậm) 3px |

---

## 9. Persistence — định dạng lưu file

```json
{
  "directed": false,
  "nodes": [
    { "id": 1, "label": "A", "x": 120.0, "y": 80.0 }
  ],
  "edges": [
    { "id": 1, "source": 1, "target": 2, "weight": 5.0 }
  ]
}
```

Dùng `System.Text.Json` (.NET 6+) hoặc `Newtonsoft.Json`.

---

## 10. Dependency flow

```
GraphApp.UI
    └── depends on ──► GraphApp.Core
                            ├── Models
                            ├── Algorithms (trả về List<AlgorithmStep>)
                            └── Converters

GraphApp.UI KHÔNG import namespace của Algorithms trực tiếp
→ chỉ gọi qua interface IGraphAlgorithm hoặc static method
```