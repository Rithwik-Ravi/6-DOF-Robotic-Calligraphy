# 6-DOF Robotic Calligraphy & Additive Manufacturing: Asynchronous TCP Coordinate Streaming

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![Mitsubishi Electric](https://img.shields.io/badge/Mitsubishi_Electric-E60012?style=for-the-badge&logo=mitsubishielectric&logoColor=white)
![TCP/IP](https://img.shields.io/badge/TCP/IP-000000?style=for-the-badge&logo=tcp%2Fip&logoColor=white)

Real-time robotic calligraphy and additive manufacturing using a Mitsubishi RV-8CRL-D arm + CR800 controller. A modular C# WinForms application streams live coordinates over TCP to a MELFA BASIC VI listener on the robot — no pre-programmed waypoints needed.

> **Note:** This project is still a Work in Progress (WIP). We are actively improving both the 2D path planning and the experimental 3D printing features.

---

## 📸 Project Showcase

### Application UI
![Application UI](UI.png)

### Video Demonstration
*(Placeholder for video demonstrating loading an STL and the robot executing the print)*

---

## 🚀 Architecture (Strategy Pattern)

The codebase has been refactored from a monolithic UI-driven design into a scalable, decoupled architecture using the **Strategy Pattern**. This supports both 2D Vectorization and 3D Additive Manufacturing via a unified networking pipeline.

### The `ToolpathEngine`
- **`IToolpathGenerator`**: A strict interface enforcing the `List<RoboticWaypoint>` contract.
- **`ToolpathCoordinator`**: Manages the active pipeline and generates the final toolpath to be streamed.

### The Pipelines
- **`ImageVectorizationPipeline2D`**: Encapsulates all Emgu.CV logic (`ZhangSuenThinning`, Ramer-Douglas-Peucker compression, Nearest Neighbor Path Optimization).
- **`TextCalligraphyPipeline2D`**: Handles font dictionary generation, word-wrapping, and typographic vector math.
- **`PrusaSlicerStrategy` (3D)**: Headlessly executes PrusaSlicer via CLI to slice an input `.stl` file.
- **`GCodeParser` (3D)**: Stateful G-Code parser that transforms multi-layer extrusion coordinates into `RoboticWaypoint6DOF` structs tracking X, Y, Z, and Posture.

### The `CoreNetworking` Layer
- **`RobotTcpClient`**: Fully decoupled from the UI. Handles bidirectional handshakes and wraps robotic coordinate strings (e.g., `MVS; X; Y; Z\r`) asynchronously. Backwards-compatible to handle both 3-axis 2D drawing and 6-axis 3D printing routines.

---

## ✨ Key Features

### 🧊 3D Printing Pipeline (NEW)
- **Headless Slicing:** Integrates directly with `prusa-slicer-console.exe` to slice `.stl` models.
- **Auto-Centering:** The backend mathematically offsets the physical coordinates to perfectly center your 3D print within the 200x150mm physical bed.
- **Interactive 3D Orbit Viewer:** A dedicated 3D canvas allows you to drag to orbit around the generated 3D toolpath. Extrusions are rendered cleanly, while travel moves and Z-hops are filtered correctly.
- **Layer Debugger:** Scrub through individual print layers using a timeline slider to inspect precise toolpaths.

### 🖋️ Dynamic Typography Engine
- **3 Font Styles:** Block, Rounded, and Italic (programmatic on-the-fly shear transform).
- **Advanced Text Layout:** Multi-line text wrapping with automatic spacing. The final line is centered, while previous lines are evenly justified.

### 🖼️ Image-to-Toolpath Vectorization
- **Emgu.CV Integration:** Converts any raster image (`.jpg`, `.png`) directly into a robotic toolpath.
- **Ramer-Douglas-Peucker Compression:** Drastically reduces dense pixel-level jaggedness by approximating smooth polygons.
- **Path Optimization:** Nearest Neighbor sorting algorithm minimizes "air time".
- **Auto-Scaling:** Proportional scaling strictly obeys the robot's physical Cartesian workspace envelope.

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

### Slicer Configuration
- Ensure PrusaSlicer is installed and `prusa-slicer-console.exe` is added to your system PATH or resides in the working directory.

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
- Transfer `RobotListener_SingleLoop.prg` from this repo and Write to Controller.

---

## 🧪 Testing (Step-by-Step)

1. Run `RobotListener_SingleLoop.prg` on controller.
2. Build and run the C# WinForms app (`dotnet run --project RobotCalligraphyApp.csproj`).
3. Enter IP: `192.168.0.20`, Port: `10003`.
4. Click **Connect**.
5. Select a Pipeline:
   - **2D**: Vectorize an Image or Generate Text.
   - **3D**: Load a 3D STL and click "Slice & Parse".
6. Click **Execute** → coordinates stream to robot.

---

## 🔮 Next Steps
- Implement precise material extrusion velocity control mapping robot speed to E-axis steps.
- Add G-Code Arc (G2/G3) support for smoother robotic curves.
- Automatic mesh repair integration.
