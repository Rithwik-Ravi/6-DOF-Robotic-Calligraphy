# 6-DOF Robotic Calligraphy: Asynchronous TCP Coordinate Streaming

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![Mitsubishi Electric](https://img.shields.io/badge/Mitsubishi_Electric-E60012?style=for-the-badge&logo=mitsubishielectric&logoColor=white)
![TCP/IP](https://img.shields.io/badge/TCP/IP-000000?style=for-the-badge&logo=tcp%2Fip&logoColor=white)

Real-time robotic calligraphy using a Mitsubishi RV-8CRL-D arm + CR800 controller. A C# WinForms app streams live coordinates over TCP to a MELFA BASIC VI listener on the robot — no pre-programmed waypoints needed.

---

## ✨ Key Features Added

### 🖋️ Dynamic Typography Engine
- **3 Font Styles:**
  - **Block:** Angular, geometric letterforms.
  - **Rounded:** Smooth curves for C, D, O, S, U, etc.
  - **Italic:** Programmatic on-the-fly shear transform applied to the base font geometry.
- **Advanced Text Layout:** Multi-line text wrapping with automatic spacing. The final line is centered, while previous lines are evenly justified to fill the physical workspace width.

### 🖼️ Image-to-Toolpath Vectorization
- **Emgu.CV Integration:** Converts any raster image (`.jpg`, `.png`) directly into a robotic toolpath.
- **Pencil Sketch (Adaptive Thresholding):** Specifically tuned for portraits and photographs, replacing global edge detection with localized contrast blocks (`BlockSize=21`) to perfectly trace soft facial features (eyes, nose, mouth) without blowing out shadows.
- **Alpha Compositing:** Perfectly flattens transparent PNGs onto a white background to prevent edge-detection loss.
- **Ramer-Douglas-Peucker Compression:** Drastically reduces dense pixel-level jaggedness by approximating smooth polygons and discarding intermediate points, controlled by a dynamic `epsilonFactor`.
- **Path Optimization:** Uses a Nearest Neighbor sorting algorithm on the extracted contours to minimize the robot's "air time" (jumping between lines).
- **Auto-Scaling & Bounding Box:** Proportional scaling maps the image pixels perfectly to the robot's physical Cartesian workspace envelope (200x150mm), strictly obeying safety limits.

### 🖥️ Enhanced UI & Visualization
- **Side-by-Side View:** The UI now displays the uploaded original raster image directly next to the generated robotic toolpath preview for easy comparison.
- **Physical Bounding Box:** The toolpath preview dynamically renders a bounding box scaled precisely to the 4:3 (200x150mm) physical workspace, ensuring you know exactly where the robot will draw and that it will never exceed frame limits.
- **Live Robotic Tracking:** As the robot draws, a highly-visible orange tracking indicator renders in real-time directly over the vectorized image, perfectly mapped to the physical location of the end-effector.
- **Dynamic ETA & Progress:** Automatically calculates the estimated time of completion and tracks progress percentage live using high-precision network execution telemetry.

### ⚡ Execution & Control
- **Optimized Toolpaths:** Reduced Z-axis lift-off transit height (5mm vs 20mm) and increased controller speed overrides (`Ovrd 100`, `Spd 800`) for significantly faster writing.
- **Zero-Latency Streaming Protocol:** By moving the `ACK` handshake *before* physical motion, the robot hides all TCP/IP network latency while combining with Continuous Interpolation (`CNT 1`) for perfectly blended, non-stop drawing at high speeds!
- **Live Control:** Asynchronous `CancellationTokenSource` allows instant **Pause** and **Stop** commands during live TCP streaming. Hitting Stop safely returns the robot to its home position.

---

## 🚀 Architecture

```
C# App (Client)  ──TCP/IP──▶  CR800 Controller (Server)  ──▶  RV-8CRL-D Arm
   Port 10003                   RobotListener.prg                 6-DOF Motion
```

- **C# Coordinate Engine** — Converts text → single-stroke vector paths → `(X, Y, Z)` coordinates
- **MELFA BASIC VI Listener** — Parses incoming ASCII strings, commands servos via `MOV`/`MVS`
- **Bidirectional handshake** — Client sends coordinate → Robot moves → Robot replies `ACK` → Client sends next

### Packet Format
```
MOV; -500.00;  850.00;  126.55\r
```
- 3-char command (`MOV` or `MVS`)
- Fixed-width X, Y, Z fields separated by semicolons
- Terminated with CR (`\r`)

---

## 🔌 Hardware Setup

### Physical Connections
- Ethernet cable from laptop → CR800 controller LAN port
- No USB required

### Network Configuration
- **Robot IP:** `192.168.0.20` (factory default)
- **Laptop IP:** `192.168.0.100` (static, same subnet)
- **Subnet Mask:** `255.255.255.0`
- Verify with `ping 192.168.0.20` in PowerShell

---

## 🛠️ RT ToolBox3 Setup (From Scratch)

### 1. Create New Workspace
- Open RT ToolBox3 → Workspace → New
- Name: `RobotCalligraphyEthernet`
- Robot Model: Select `RV-8CRL-D`

### 2. Communication Settings (Step 3 in Wizard)
- **IP Address:** `192.168.0.20`
- **Subnet Mask:** `255.255.255.0`
- **Method:** `TCP/IP`
- **Port #:** `10001` (for RT ToolBox3's own connection)
- Click Finish

### 3. Configure OPT12 (Ethernet Data Link Port)
- Go to Parameter → Communication and Network
- Double-click **OPT12** in the Device List
- Set these values:
  - **Mode:** `1: Server`
  - **Port #:** `10003`
  - **Protocol:** `0: No-procedure`
  - **Packet Type:** `0: CR`
- Click OK

### 4. Set CPRCE12 to Data Link Mode ⚠️ CRITICAL
- Go to Parameter → **Parameter List**
- Search for `CPRCE`
- Find element for OPT12 (either `CPRCE(2)` or `CPRCE12`)
- **Change value from `0` to `2`**
  - `0` = Command server intercepts all traffic (broken)
  - `2` = Data Link mode — raw data goes to MELFA BASIC `INPUT`/`PRINT` (working)
- Click **Write** (NOT Initialize)

### 5. Map COM2 to OPT12
- In Parameter List, search for `COMDEV`
- Set `COMDEV(2)` = `OPT12`
- Click **Write**

### 6. Power Cycle
- Turn controller OFF → wait 5 sec → turn ON
- Parameters only take effect after reboot

### 7. Load the Program
- In the workspace tree, right-click Program folder → Add Existing
- Select `RobotListener.prg` from this repo
- Right-click → Write to Controller

---

## 🧪 Testing (Step-by-Step)

### Step 1: Verify the Connection (Diagnostic)
1. Turn **Servos ON** on teach pendant
2. Run `RobotListener.prg` on controller → waits at `*WAITCONN`
3. Open PowerShell:
   ```
   cd C:\Users\Rithwik\Desktop\Mitsubishi\RobotCalligraphy\RobotCalligraphyApp
   .\test_diagnostic.ps1
   ```
4. Script auto-connects, sends one coordinate, listens for 10 seconds
5. ✅ Expected: `Response #1: [ACK]` and robot arm moves

### Step 2: Manual Coordinate Sender
1. Restart `RobotListener.prg` on controller
2. Run:
   ```
   .\test_raw_sender.ps1
   ```
3. Type: `MOV; -586.18;  783.00;  182.01` → press Enter
4. ✅ Expected: `Robot says: ACK` and robot moves to position
5. Send more coordinates or type `exit` to quit

### Step 3: Full C# Application
1. Restart `RobotListener.prg` on controller
2. Build and run the C# WinForms app
3. Enter IP: `192.168.0.20`, Port: `10003`
4. Click **Connect**
5. Type text → Click **Generate** → Check visual preview
6. Click **Execute** → coordinates stream to robot

---

## 📁 File Structure

| File | Purpose |
|------|---------|
| `RobotListener.prg` | MELFA BASIC VI TCP server — runs on controller |
| `RobotTcpClient.cs` | C# TCP client — handles socket communication |
| `Form1.cs` | WinForms UI — text input, preview, execution |
| `test_diagnostic.ps1` | Auto-sends one command, captures all responses for 10s |
| `test_raw_sender.ps1` | Interactive manual coordinate sender |
| `test_server.ps1` | Mock TCP server for testing C# app without robot |
| `test_client.ps1` | Passive listener to check robot broadcasts |

---

## 🐛 Issues Encountered & Fixes

### 1. "COM file is not opened" (Error 3140)
- **Cause:** `COM2:` had no device mapped to it
- **Fix:** Set `COMDEV(2) = OPT12` in Parameter List → Write → Reboot

### 2. Robot responds with `QeR601000000` instead of `ACK`
- **Cause:** `CPRCE12 = 0` (No-procedure) still routes traffic through the controller's built-in MC Protocol command server, which intercepts and rejects raw strings
- **Fix:** Set `CPRCE12 = 2` (Data Link mode) → Write → Reboot
- This was the **root cause** of all communication failures

### 3. `Line Input #1, C1$` → Syntax Error
- **Cause:** `Line Input` is not supported in MELFA BASIC VI
- **Fix:** Use standard `INPUT #1, C1$`

### 4. `INPUT #1, "", C1$` → Syntax Error
- **Cause:** Prompt string syntax not supported on CR800
- **Fix:** Use standard `INPUT #1, C1$`

### 5. PowerShell `ReadLine()` hangs forever
- **Cause:** MELFA BASIC terminates with `\r` only (no `\n`), Windows `ReadLine()` waits for `\n`
- **Fix:** Byte-by-byte reading, break on `\r` (ASCII 13)

### 6. RT ToolBox3 simulator — `Print #1` never reaches client
- **Cause:** RT ToolBox3's virtual networking does not support bidirectional TCP on loopback
- **Fix:** Test on physical hardware only

---

## 🔮 Next Steps

### Phase 1: Continuous Calligraphy Streaming
- [x] Stream full vector font toolpath from C# app to physical robot
- [x] Verify `ACK`-gated flow control at 20% override speed
- [x] Tune `Ovrd` and `Spd` parameters for smooth pen strokes
- [x] Add pen-up / pen-down Z-axis logic for letter spacing
- [x] Implement multi-line text layout and justification
- [x] Integrate Emgu.CV for custom image/logo vectorization

### Phase 2: 3D Printing / Additive Manufacturing
- [ ] Extend coordinate system to include Z-layer slicing
- [ ] Implement G-code → coordinate converter
- [ ] Add material extrusion control (end-effector I/O)
- [ ] Multi-layer path planning with Z-increment per layer

---

## 🧠 Why This Matters

Building a standard `.prg` file is operator-level work. This project implements a **bidirectional real-time streaming architecture** with deep parameter-level controller integration (`COMDEV`, `CPRCE`, `OPT` routing). This opens the door for computer vision integration, dynamic collision avoidance, and adaptive toolpath generation — core capabilities for Industry 4.0 applications.
