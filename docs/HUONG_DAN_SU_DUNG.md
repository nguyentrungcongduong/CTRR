# 📖 Hướng Dẫn Sử Dụng — GraphApp

> Ứng dụng trực quan hóa đồ thị và các thuật toán cơ bản / nâng cao.
> Ngôn ngữ: C# · .NET 8 · Windows Forms

---

## 📋 Mục lục

1. [Giao diện tổng quan](#1-giao-diện-tổng-quan)
2. [Vẽ đồ thị](#2-vẽ-đồ-thị)
3. [Lưu & Mở đồ thị](#3-lưu--mở-đồ-thị)
4. [Các phương pháp nhập đồ thị](#4-các-phương-pháp-nhập-đồ-thị)
5. [Chuyển đổi biểu diễn đồ thị](#5-chuyển-đổi-biểu-diễn-đồ-thị)
6. [Tìm đường đi ngắn nhất — Dijkstra](#6-tìm-đường-đi-ngắn-nhất--dijkstra)
7. [Duyệt đồ thị — BFS & DFS](#7-duyệt-đồ-thị--bfs--dfs)
8. [Kiểm tra đồ thị 2 phía — Bipartite](#8-kiểm-tra-đồ-thị-2-phía--bipartite)
9. [Prim — Cây khung nhỏ nhất](#9-prim--cây-khung-nhỏ-nhất)
10. [Kruskal — Cây khung nhỏ nhất](#10-kruskal--cây-khung-nhỏ-nhất)
11. [Ford-Fulkerson — Luồng cực đại](#11-ford-fulkerson--luồng-cực-đại)
12. [Fleury — Đường / Chu trình Euler](#12-fleury--đường--chu-trình-euler)
13. [Hierholzer — Chu trình Euler O(E)](#13-hierholzer--chu-trình-euler-oe)
14. [Phím tắt](#14-phím-tắt)

---

## 1. Giao diện tổng quan

```
┌─────────────────────────────────── Toolbar ───────────────────────────────────┐
│ Chọn | Thêm đỉnh | Thêm cạnh | Xóa | Có hướng | Undo | Redo | Mẫu | Lưu | Mở │
│ Biểu Diễn | Nhập Ma Trận | Nhập nhanh | Xuất PNG                              │
├─────────────────────────────────────────────────────────────────────────────── ┤
│                                                                                 │
│                          Canvas vẽ đồ thị                   │ Panel Biểu Diễn │
│                         (kéo thả, zoom, pan)                │ (Ma trận kề /   │
│                                                              │  Danh sách kề / │
│                                                              │  Danh sách cạnh)│
├─────────────────────────────────────────────────────────────────────────────── ┤
│  Thuật toán: [▼]   Đỉnh bắt đầu: [▼]   [Đỉnh đích: ▼]   [▶ Chạy]   Bước: — │
│  |◀  ◀  ▶  ▶|  ⏵ Phát   🐢───────🐇   ⏱ Bình thường                        │
│  Mô tả bước hiện tại (RichTextBox log, cuộn được)                              │
├─────────────────────────────────────────────────────────────────────────────── ┤
│ 📍 6 đỉnh  🔗 7 cạnh  — Vô hướng                              ↖ Chọn/Kéo     │
└─────────────────────────────────────────────────────────────────────────────── ┘
```

---

## 2. Vẽ đồ thị

### 2.1 Thêm đỉnh
1. Nhấn nút **⊕ Thêm đỉnh** trên toolbar (hoặc nhấn phím **N**).
2. Click vào vị trí bất kỳ trên canvas → đỉnh mới xuất hiện với nhãn tự động (A, B, C, …).

### 2.2 Thêm cạnh
1. Nhấn nút **→ Thêm cạnh** trên toolbar.
2. Click **đỉnh nguồn** → viền nét đứt xuất hiện xung quanh đỉnh đó.
3. Click **đỉnh đích** → hộp thoại nhập **Trọng số** hiện ra.
4. Nhập trọng số (hoặc để mặc định = 1) → nhấn OK.

> **Lưu ý:** Nếu click lại cùng một đỉnh, lệnh thêm cạnh sẽ bị hủy.

### 2.3 Kéo thả đỉnh
1. Chọn chế độ **↖ Chọn** (mặc định).
2. Giữ chuột trái lên đỉnh và kéo đến vị trí mới.

### 2.4 Đổi tên đỉnh *(tính năng mới)*
1. Ở chế độ **↖ Chọn**, **double-click** vào đỉnh cần đổi tên.
2. Hộp nhập tên hiện ngay trên đỉnh — gõ tên mới.
3. Nhấn **Enter** để lưu, **Escape** để hủy.

### 2.5 Xóa đỉnh / cạnh
1. Chọn chế độ **✕ Xóa**.
2. Click vào đỉnh → xác nhận xóa đỉnh và toàn bộ cạnh liên quan.
3. Click vào cạnh → xác nhận xóa cạnh đó.

### 2.6 Bật / tắt đồ thị có hướng
- Nhấn nút **⇄ Có hướng** trên toolbar để chuyển đổi.
- Khi bật: cạnh vẽ với mũi tên, thuật toán hoạt động theo chiều cạnh.
- Khi tắt: cạnh vô hướng (đường thẳng), hai chiều đều đi được.

### 2.7 Zoom & Pan
| Thao tác | Kết quả |
|----------|---------|
| Lăn bánh xe chuột | Zoom vào / ra quanh con trỏ |
| Giữ chuột giữa + kéo | Di chuyển (pan) |
| Nhấn **F** | Vừa màn hình (fit all) |
| Nhấn **Ctrl+0** | Reset zoom về 100% |
| Nút **🔲 Vừa màn hình** | Như phím F |

---

## 3. Lưu & Mở đồ thị

### Lưu
1. Nhấn nút **💾 Lưu** trên toolbar (hoặc **Ctrl+S** nếu có).
2. Chọn vị trí lưu → file được lưu định dạng **`.graph.json`**.
3. File lưu đầy đủ: vị trí đỉnh, nhãn, trọng số cạnh, loại đồ thị.

### Mở
1. Nhấn nút **📂 Mở** trên toolbar.
2. Chọn file `.graph.json` → đồ thị được tải lên canvas.

### Undo / Redo
- **Ctrl+Z** hoặc nút **↩ Hoàn tác** — hoàn tác thao tác vừa rồi (tối đa 50 bước).
- **Ctrl+Y** hoặc nút **↪ Làm lại** — làm lại sau khi hoàn tác.

### Xuất PNG
1. Nhấn nút **📷 Xuất PNG** hoặc **Ctrl+E**.
2. Chọn tên file → ảnh xuất ở độ phân giải 2× (chất lượng cao).

---

## 4. Các phương pháp nhập đồ thị

### 4.1 Vẽ tay trên canvas
*(Xem mục 2)*

### 4.2 Nhập từ ma trận kề
1. Nhấn nút **📝 Nhập Ma Trận** trên toolbar.
2. Dialog **Nhập Ma Trận Kề** hiện ra:
   - Chọn số đỉnh N.
   - Nhập nhãn cho từng đỉnh (A, B, C, … hoặc tên tùy ý).
   - Điền vào bảng: `matrix[i][j]` = trọng số cạnh i→j, `0` = không có cạnh.
3. Nhấn **OK** → đồ thị được tạo và hiển thị trên canvas.

### 4.3 Nhập nhanh từ văn bản *(tính năng mới)*
1. Nhấn nút **📋 Nhập nhanh** trên toolbar.
2. Dialog hiện ra — nhập mỗi dòng theo cú pháp:
   ```
   <Nguồn> <Đích> [TrọngSố]
   ```
   **Ví dụ:**
   ```
   A B 5
   B C 3
   A C 7
   C D
   # Dòng bắt đầu bằng # là chú thích, bị bỏ qua
   ```
   - Trọng số là số thực, dùng dấu chấm thập phân (`.`).
   - Bỏ trọng số → mặc định = 1.
   - Phân tách bằng khoảng trắng, tab hoặc dấu phẩy.
3. Tích chọn **Có hướng** nếu muốn đồ thị có hướng.
4. Nhấn **✔ Áp dụng** → đồ thị tự fit màn hình.

### 4.4 Tải đồ thị mẫu
Nhấn dropdown **📚 Đồ thị mẫu** và chọn một trong 5 mẫu:

| Mẫu | Phù hợp kiểm tra |
|-----|-----------------|
| 🔵 Vô hướng 6 đỉnh | BFS, DFS, Prim, Kruskal |
| 🟡 Có hướng có trọng số | Dijkstra, Ford-Fulkerson |
| 🟢 2 Phía (Bipartite) | Kiểm tra Bipartite |
| 🟣 Đường Euler | Fleury, Hierholzer |
| ⚪ Chu trình Euler | Fleury, Hierholzer |

---

## 5. Chuyển đổi biểu diễn đồ thị

### Mở Panel Biểu Diễn
- Nhấn nút **📊 Biểu Diễn** trên toolbar → Panel xuất hiện bên phải canvas.

### 3 dạng biểu diễn (3 tab)

#### Tab 1 — Ma Trận Kề
- Bảng N×N hiển thị trọng số cạnh.
- Hàng i, cột j = trọng số cạnh i→j (0 = không có cạnh).
- **Chỉnh sửa:** Click vào ô, gõ giá trị mới.
- **Áp dụng:** Nhấn **🔄 Áp Dụng vào Canvas** → đồ thị cập nhật ngay.

#### Tab 2 — Danh Sách Kề
- Mỗi dòng: `<Đỉnh>: <Láng giềng 1>(trọng số) → <Láng giềng 2>(trọng số) → ...`

  **Ví dụ vô hướng:**
  ```
  A: B(4) → D(2)
  B: A(4) → C(5) → E(3)
  ```
  **Ví dụ có hướng (bỏ trọng số = 1):**
  ```
  A: B → C
  B: C
  ```
- **Chỉnh sửa** trực tiếp trong khung văn bản.
- **Áp dụng:** Nhấn **🔄 Áp Dụng vào Canvas**.

#### Tab 3 — Danh Sách Cạnh
- Bảng 3 cột: **Nguồn | Đích | Trọng số**.
- **Thêm cạnh:** Nhấn nút **+ Thêm cạnh** → hàng mới, gõ trực tiếp.
- **Xóa cạnh:** Chọn hàng → nhấn **− Xóa cạnh**.
- **Áp dụng:** Nhấn **🔄 Áp Dụng vào Canvas**.

> **Lưu ý chuyển đổi:**
> - Đồ thị **vô hướng**: ma trận đối xứng, danh sách kề liệt kê cả 2 chiều.
> - Đồ thị **có hướng**: ma trận không đối xứng, danh sách kề chỉ chiều đi ra.

---

## 6. Tìm đường đi ngắn nhất — Dijkstra

**Yêu cầu:** Đồ thị có trọng số ≥ 0 (có hướng hoặc vô hướng).

### Các bước thực hiện:
1. Tạo đồ thị có **trọng số trên mỗi cạnh**.
2. Trong panel Animation, chọn **"Dijkstra – Đường đi ngắn nhất"**.
3. Chọn **Đỉnh bắt đầu** (nguồn).
4. *(Tùy chọn)* Chọn **Đỉnh đích** → thuật toán dừng sớm khi đến đích và **tô màu tím** đường đi cụ thể.
   - Bỏ trống Đỉnh đích = tìm khoảng cách ngắn nhất đến **tất cả đỉnh**.
5. Nhấn **▶ Chạy**.

### Đọc kết quả:
| Màu | Ý nghĩa |
|-----|---------|
| 🟢 Xanh lá | Đỉnh đã xử lý xong |
| 🟠 Cam | Đỉnh đang trong hàng đợi ưu tiên |
| 🔴 Đỏ | Đỉnh đang xét |
| Tím (cạnh) | Đường đi ngắn nhất |
| Số dưới đỉnh | Khoảng cách ngắn nhất hiện tại (∞ = chưa đến được) |

### Ví dụ minh họa:
```
Nguồn: A
  dist[A] = 0
  dist[B] = ∞ → 4
  dist[C] = ∞ → 7
  ...
✅ Đường đi ngắn nhất A→C: A → B → C, tổng = 7
```

---

## 7. Duyệt đồ thị — BFS & DFS

**Yêu cầu:** Đồ thị bất kỳ (có/vô hướng, có/không trọng số).

### BFS (Duyệt chiều rộng)
1. Chọn **"BFS – Duyệt chiều rộng"**.
2. Chọn **Đỉnh bắt đầu**.
3. Nhấn **▶ Chạy**.

**Nguyên lý:** Dùng hàng đợi (Queue). Duyệt tất cả đỉnh ở cùng mức trước khi xuống mức sâu hơn.

| Màu | Ý nghĩa |
|-----|---------|
| 🟢 Xanh lá | Đã duyệt xong |
| 🟠 Cam | Đang trong hàng đợi |
| 🔴 Đỏ | Đỉnh đang xét hiện tại |

### DFS (Duyệt chiều sâu)
1. Chọn **"DFS – Duyệt chiều sâu"**.
2. Chọn **Đỉnh bắt đầu**.
3. Nhấn **▶ Chạy**.

**Nguyên lý:** Dùng stack (ngăn xếp). Đi sâu vào một nhánh trước khi quay lui.

> **Xử lý đồ thị không liên thông:** Cả BFS và DFS đều tự động xử lý — sau khi duyệt xong một thành phần liên thông, thuật toán tiếp tục với thành phần chưa duyệt.

---

## 8. Kiểm tra đồ thị 2 phía — Bipartite

**Yêu cầu:** Đồ thị bất kỳ.

**Định nghĩa:** Đồ thị 2 phía (bipartite) là đồ thị có thể chia đỉnh thành 2 nhóm A và B sao cho mọi cạnh đều nối một đỉnh thuộc A với một đỉnh thuộc B (không có cạnh trong cùng nhóm).

### Các bước:
1. Chọn **"Kiểm tra 2 phía (Bipartite)"**.
2. Không cần chọn đỉnh bắt đầu.
3. Nhấn **▶ Chạy**.

### Đọc kết quả:
| Màu | Ý nghĩa |
|-----|---------|
| 🟢 Xanh lá (đỉnh) | Thuộc **Nhóm A** |
| 🟠 Cam (đỉnh) | Thuộc **Nhóm B** |
| 🔴 Đỏ (đỉnh) | 2 đỉnh xung đột (cùng nhóm, kề nhau) |

- Kết quả **✅ LÀ đồ thị 2 phía** → hiện danh sách Nhóm A và Nhóm B.
- Kết quả **❌ KHÔNG phải** → tô đỏ 2 đỉnh tạo ra xung đột.

> **Ví dụ ứng dụng:** Đồ thị bipartite thường xuất hiện trong bài toán ghép cặp (matching), lập lịch, ...

---

## 9. Prim — Cây khung nhỏ nhất

**Yêu cầu:** Đồ thị **vô hướng** có trọng số.

**Định nghĩa:** Cây khung nhỏ nhất (MST) là cây con nối tất cả đỉnh với tổng trọng số nhỏ nhất.

### Các bước:
1. Tạo đồ thị vô hướng với trọng số.
2. Tắt chế độ **"Có hướng"** nếu đang bật.
3. Chọn **"Prim – Cây khung nhỏ nhất (MST)"**.
4. Chọn **Đỉnh bắt đầu** (thuật toán có thể bắt đầu từ bất kỳ đỉnh nào).
5. Nhấn **▶ Chạy**.

### Đọc kết quả:
| Màu | Ý nghĩa |
|-----|---------|
| 🟢 Xanh lá (đỉnh) | Đã vào MST |
| 🟠 Cam (đỉnh) | Có thể đưa vào MST (trong hàng đợi ưu tiên) |
| 🔴 Đỏ (đỉnh) | Đỉnh đang xét |
| Tím (cạnh) | Cạnh thuộc MST |
| Số dưới đỉnh | `key[v]` — trọng số nhỏ nhất để kết nối v vào MST |

**Kết quả cuối:** Tổng trọng số MST và danh sách cạnh MST.

---

## 10. Kruskal — Cây khung nhỏ nhất

**Yêu cầu:** Đồ thị **vô hướng** có trọng số.

### Các bước:
1. Tạo đồ thị vô hướng với trọng số.
2. Chọn **"Kruskal – Cây khung nhỏ nhất"**.
3. Không cần chọn đỉnh bắt đầu (thuật toán xét toàn bộ cạnh).
4. Nhấn **▶ Chạy**.

### Nguyên lý:
- Sắp xếp tất cả cạnh theo trọng số tăng dần.
- Lần lượt chọn cạnh có trọng số nhỏ nhất mà **không tạo chu trình** (dùng Union-Find).

### Đọc kết quả:
| Màu | Ý nghĩa |
|-----|---------|
| Tím (cạnh) | Cạnh được chọn vào MST |
| Đỏ nhạt (cạnh) | Cạnh bị từ chối (tạo chu trình) |
| Vàng (cạnh) | Cạnh đang xét |

---

## 11. Ford-Fulkerson — Luồng cực đại

**Yêu cầu:** Đồ thị **có hướng**, trọng số = khả năng thông qua (capacity).

**Định nghĩa:** Tìm luồng lớn nhất có thể đi từ đỉnh **Nguồn (Source)** đến đỉnh **Đích (Sink)**.

### Các bước:
1. Tạo đồ thị **có hướng** với trọng số (= capacity).
2. Bật chế độ **"Có hướng"** trên toolbar.
3. Chọn **"Ford-Fulkerson – Luồng cực đại"**.
4. Chọn **Đỉnh bắt đầu** = **đỉnh Nguồn**.
5. Chọn **Đỉnh đích** = **đỉnh Đích** *(tính năng mới — hiện ComboBox riêng)*.
6. Nhấn **▶ Chạy**.

### Đọc kết quả:
- Label trên mỗi cạnh: **`flow/capacity`** (ví dụ: `3/5` nghĩa là đang chạy 3 trên tổng capacity 5).
- Cạnh **no đầy** (flow = capacity) được tô sáng ở bước cuối.

| Màu | Ý nghĩa |
|-----|---------|
| 🟢 Xanh lá (đỉnh) | Đỉnh Nguồn |
| 🟠 Cam (đỉnh) | Đỉnh Đích |
| Cam (cạnh) | Đường tăng luồng hiện tại |

**Kết quả cuối:** `MAX FLOW = X` — tổng luồng cực đại.

> **Biến thể:** Sử dụng Edmonds-Karp (BFS tìm đường tăng luồng) — độ phức tạp O(VE²).

---

## 12. Fleury — Đường / Chu trình Euler

**Yêu cầu:** Đồ thị liên thông có đường Euler hoặc chu trình Euler.

**Định lý Euler:**
- **Chu trình Euler** (qua tất cả cạnh, về điểm xuất phát): tất cả đỉnh đều có **bậc chẵn**.
- **Đường Euler** (qua tất cả cạnh, không về điểm xuất phát): đúng **2 đỉnh có bậc lẻ** (bắt đầu từ 1 trong 2 đỉnh đó).

### Các bước:
1. Tạo đồ thị thỏa điều kiện Euler ở trên.
2. Chọn **"Fleury – Đường/Chu trình Euler"**.
3. Chọn **Đỉnh bắt đầu** (với đường Euler: nên chọn đỉnh bậc lẻ).
4. Nhấn **▶ Chạy**.

### Nguyên lý:
- Mỗi bước chọn cạnh kề tiếp theo, **ưu tiên cạnh không phải cầu** (bridge).
- Kiểm tra cầu bằng thuật toán DFS (Tarjan-style).

### Đọc kết quả:
| Màu | Ý nghĩa |
|-----|---------|
| Tím (cạnh) | Cạnh đã đi qua |
| Vàng (cạnh) | Cạnh đang kiểm tra (bridge check) |
| Đỏ nhạt (cạnh) | Cạnh là cầu, tránh đi trừ khi không còn lựa chọn |

---

## 13. Hierholzer — Chu trình Euler O(E)

**Yêu cầu:** Giống Fleury — đồ thị liên thông thỏa điều kiện Euler.

**Ưu điểm so với Fleury:** Hiệu quả hơn — độ phức tạp **O(E)** thay vì O(E²).

### Các bước:
1. Tạo đồ thị thỏa điều kiện Euler.
2. Chọn **"Hierholzer – Chu trình Euler (O(E))"**.
3. Chọn **Đỉnh bắt đầu**.
4. Nhấn **▶ Chạy**.

### Nguyên lý:
- Dùng stack để đi qua các cạnh.
- Khi bị kẹt (không còn cạnh tiếp theo), quay lui và nối vào đường đi chính.

---

## 14. Phím tắt

| Phím | Chức năng |
|------|-----------|
| **Space** | Phát / Dừng animation |
| **←** | Bước trước |
| **→** | Bước sau |
| **Home** | Về bước đầu |
| **End** | Về bước cuối |
| **Enter** | Chạy thuật toán |
| **F** | Vừa màn hình (fit) |
| **Ctrl+Z** | Hoàn tác |
| **Ctrl+Y** | Làm lại |
| **Ctrl+E** | Xuất PNG |
| **Ctrl+0** | Reset zoom 100% |

### Điều khiển animation

```
|◀      ◀      ▶      ▶|     ⏵ Phát
Bước đầu  Trước  Sau  Bước cuối  Phát/Dừng

🐢 ─────────────────────── 🐇
   Chậm   (kéo thanh)  Nhanh
```

---

## 💡 Mẹo sử dụng

1. **Đồ thị mẫu nhanh:** Dùng menu **📚 Đồ thị mẫu** để thử ngay không cần vẽ tay.
2. **So sánh Prim vs Kruskal:** Cùng một đồ thị, chạy cả 2 → kết quả MST giống nhau nhưng quá trình khác.
3. **Xem biểu diễn song song:** Mở Panel Biểu Diễn khi vẽ để xem ma trận kề cập nhật theo thời gian thực.
4. **Nhập nhanh đồ thị lớn:** Dùng **📋 Nhập nhanh** thay vì click từng cạnh.
5. **Double-click** đỉnh để đổi tên giúp đặt tên có nghĩa (S, T, A, B, ...) trước khi chạy Ford-Fulkerson.
6. **Tốc độ animation:** Kéo thanh về phía 🐇 để xem nhanh, phía 🐢 để quan sát từng bước kỹ hơn.
7. **Bước thủ công:** Nhấn **◀ ▶** để tự điều khiển từng bước mà không cần phát tự động.

---

## 🐛 Xử lý lỗi thường gặp

| Thông báo lỗi | Nguyên nhân | Giải pháp |
|---------------|-------------|-----------|
| "Prim chỉ chạy trên đồ thị VÔ HƯỚNG" | Đang bật chế độ Có hướng | Tắt nút **⇄ Có hướng** |
| "Ford-Fulkerson chỉ chạy trên đồ thị CÓ HƯỚNG" | Đồ thị vô hướng | Bật nút **⇄ Có hướng** |
| "Dijkstra không hoạt động với trọng số âm" | Có cạnh trọng số < 0 | Sửa trọng số cạnh ≥ 0 |
| "Không có đường Euler" | Vi phạm điều kiện bậc đỉnh | Kiểm tra lại bậc các đỉnh |
| "Nguồn và đích không được trùng nhau" | Chọn cùng đỉnh cho nguồn và đích | Chọn 2 đỉnh khác nhau |
| "Không tìm thấy cạnh hợp lệ" | Format nhập nhanh sai | Kiểm tra cú pháp `A B 5` |

---

*Phiên bản tài liệu: 1.1 — Cập nhật: 2026-06-10*
