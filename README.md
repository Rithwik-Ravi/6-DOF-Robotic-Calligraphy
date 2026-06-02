# 6-DOF Robotic Calligraphy: Asynchronous TCP Coordinate Streaming

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![Mitsubishi Electric](https://img.shields.io/badge/Mitsubishi_Electric-E60012?style=for-the-badge&logo=mitsubishielectric&logoColor=white)
![TCP/IP](https://img.shields.io/badge/TCP/IP-000000?style=for-the-badge&logo=tcp%2Fip&logoColor=white)

Real-time robotic calligraphy using a Mitsubishi RV-8CRL-D arm + CR800 controller. A modular C# WinForms application streams live coordinates over TCP to a MELFA BASIC VI listener on the robot — no pre-programmed waypoints needed.

---

## 📸 Project Showcase

### Application UI
<!-- Replace the link below with your actual UI screenshot -->
![Application UI Placeholder](docs/images/ui_placeholder.png)

### Robot in Action
<!-- Replace the link below with your actual robot picture/gif -->
![Robot Working Placeholder](docs/images/robot_placeholder.png)

---

## 🚀 Clean Architecture (Strategy Pattern)

The codebase has been refactored from a monolithic UI-driven design into a scalable, decoupled architecture using the **Strategy Pattern**. This sets the foundation for expanding into 3D Printing and Additive Manufacturing.

### The `ToolpathEngine`
- **`IToolpathGenerator`**: A strict interface enforcing the `List<RoboticWaypoint> Generate()` contract.
- **`ToolpathCoordinator`**: Manages the active pipeline and generates the final toolpath to be streamed.

### The Pipelines (`Pipelines_2D` & `Pipelines_3D`)
- **`ImageVectorizationPipeline2D`**: Encapsulates all Emgu.CV logic (`ZhangSuenThinning`, Ramer-Douglas-Peucker compression, and Nearest Neighbor Path Optimization) to convert raster images to optimized robotic toolpaths.
- **`TextCalligraphyPipeline2D`**: Handles font dictionary generation, word-wrapping, and typographic vector math.
- **`Slicer3D_Placeholder`**: Architectural placeholder ready to consume `.stl` files and output 3-dimensional, multi-layered toolpaths.

### The `CoreNetworking` Layer
- **`RobotTcpClient`**: Fully decoupled from the UI. Handles the bidirectional handshakes and wraps robotic coordinate strings (e.g., `MVS; X; Y; Z\r`) asynchronously.

---

## ✨ Key Features

### 🖋️ Dynamic Typography Engine
- **3 Font Styles:** Block, Rounded, and Italic (programmatic on-the-fly shear transform).
- **Advanced Text Layout:** Multi-line text wrapping with automatic spacing. The final line is centered, while previous lines are evenly justified.

### 🖼️ Image-to-Toolpath Vectorization
- **Emgu.CV Integration:** Converts any raster image (`.jpg`, `.png`) directly into a robotic toolpath.
- **Pencil Sketch (Adaptive Thresholding):** Tuned for tracing soft facial features without blowing out shadows.
- **Alpha Compositing:** Perfectly flattens transparent PNGs onto a white background.
- **Ramer-Douglas-Peucker Compression:** Drastically reduces dense pixel-level jaggedness by approximating smooth polygons.
- **Path Optimization:** Nearest Neighbor sorting algorithm minimizes "air time".
- **Auto-Scaling:** Proportional scaling strictly obeys the robot's physical Cartesian workspace envelope.

### 🖥️ Enhanced UI & Visualization
- **Side-by-Side View:** Displays the original raster image next to the generated toolpath preview.
- **Physical Bounding Box:** Renders a dynamically scaled bounding box of the 4:3 (200x150mm) physical workspace.
- **Live Robotic Tracking:** An orange tracking indicator renders in real-time over the vectorized image, mapped to the physical location of the end-effector.
- **Dynamic ETA & Progress:** Calculates the estimated time of completion and tracks progress percentage live.

### ⚡ Execution & Control
- **Zero-Latency Streaming Protocol:** The robot hides TCP/IP network latency while combining Continuous Interpolation (`CNT 1`) for perfectly blended, non-stop drawing at high speeds!
- **Live Control:** Asynchronous cancellation tokens allow instant **Pause** and **Stop** commands. Hitting Stop safely returns the robot to its home position.

---

## 🔌 Hardware Setup

### Physical Connections
- Ethernet cable from laptop → CR800 controller LAN port

### Network Configuration
- **Robot IP:** `192.168.0.20` (factory default)
- **Laptop IP:** `192.168.0.100` (static, same subnet)
- **Subnet Mask:** `255.255.255.0`

---

## 🛠️ RT ToolBox3 Setup (From Scratch)

### 1. Configure OPT12 (Ethernet Data Link Port)
- Parameter → Communication and Network → **OPT12**
- **Mode:** `1: Server`, **Port #:** `10003`, **Protocol:** `0: No-procedure`, **Packet Type:** `0: CR`

### 2. Set CPRCE12 to Data Link Mode ⚠️ CRITICAL
- Parameter List → `CPRCE12` (or `CPRCE(2)`)
- **Change value from `0` to `2`** (Data Link mode — raw data goes to MELFA BASIC `INPUT`/`PRINT`).

### 3. Map COM2 to OPT12
- Parameter List → `COMDEV(2)` = `OPT12`

### 4. Power Cycle
- Turn controller OFF → wait 5 sec → turn ON.

### 5. Load the Program
- Transfer `RobotListener.prg` from this repo and Write to Controller.

---

## 🧪 Testing (Step-by-Step)

### Step 1: Verify the Connection (Diagnostic)
1. Turn **Servos ON** on teach pendant
2. Run `RobotListener.prg` on controller
3. Open PowerShell:
   ```
   cd C:\Users\Rithwik\Desktop\Mitsubishi\RobotCalligraphy\RobotCalligraphyApp
   .\test_diagnostic.ps1
   ```
4. Expected: `Response #1: [ACK]` and robot arm moves.

### Step 2: Full C# Application
1. Run `RobotListener.prg` on controller.
2. Build and run the C# WinForms app (`dotnet run`).
3. Enter IP: `192.168.0.20`, Port: `10003`.
4. Click **Connect**.
5. Vectorize an Image or Generate Text → Check visual preview.
6. Click **Execute** → coordinates stream to robot.

---

## 📁 File Structure

| Folder/File | Purpose |
|------|---------|
| `CoreNetworking/` | C# TCP client logic. Fully decoupled network handler. |
| `ToolpathEngine/` | Architecture definitions: `IToolpathGenerator`, `ToolpathCoordinator`. |
| `Pipelines_2D/` | Emgu.CV Image Vectorization and Font Dictionaries. |
| `Pipelines_3D/` | Prepared foundation for 3D Slicing and G-Code parsing. |
| `RobotListener.prg` | MELFA BASIC VI TCP server — runs on physical controller. |
| `Form1.cs` | WinForms UI — UI controllers, preview rendering. |
| `test_*.ps1` | PowerShell scripts for mocking server/client connections. |

---

## 🔮 Next Steps

### Phase 2: 3D Printing / Additive Manufacturing
- [ ] Implement `Slicer3D_Placeholder` to ingest `.stl` or `.gcode`.
- [ ] Extend `RoboticWaypoint` to support end-effector material extrusion.
- [ ] Multi-layer path planning with Z-increment per layer.
