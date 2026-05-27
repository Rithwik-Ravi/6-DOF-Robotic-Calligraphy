# 6-DOF Robotic Calligraphy: Asynchronous TCP Coordinate Streaming

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![Mitsubishi Electric](https://img.shields.io/badge/Mitsubishi_Electric-E60012?style=for-the-badge&logo=mitsubishielectric&logoColor=white)
![TCP/IP](https://img.shields.io/badge/TCP/IP-000000?style=for-the-badge&logo=tcp%2Fip&logoColor=white)

This repository contains the software architecture for real-time robotic calligraphy. It uses a Mitsubishi Electric CR800 Controller and a 6-DOF robotic arm. 

Industrial robot controllers usually rely on static, pre-programmed waypoints. This project bypasses those limitations. It implements a Custom Asynchronous TCP Socket Server in C#. This allows for real-time, dynamic toolpath generation and execution.

## 🚀 Architecture Overview

The system has two primary components communicating over a standard TCP/IP socket:

1. **C# Coordinate Engine (Client)**: Translates user text input into continuous spatial toolpaths. It uses a custom single-stroke vector font engine. It then streams the physical coordinates `(X, Y, Z)` asynchronously to the robot.
2. **MELFA BASIC VI Listener (Server)**: A lightweight script (`RobotListener.prg`) running continuously on the CR800 controller. It listens on port `10003` using the `Line Input` command to parse incoming ASCII coordinate strings. It commands the servo drives using joint (`MOV`) or linear (`MVS`) interpolation immediately.

### The Handshake & Communication Protocol

- **Connection**: The C# Client connects to the CR800 Controller on `IP: 192.168.0.20`, Port: `10003`.
- **Packet Structure**: Coordinates are formatted as fixed-length strings. This minimizes parsing overhead on the controller. 
  Example: `MVS; -500.00;  850.00;  126.55\r\n`
- **Execution Loop**:
  1. Client sends a coordinate packet.
  2. Controller parses the string and updates the target position vector (`P2`).
  3. Controller executes the physical move.
  4. Controller replies with an `ACK` string.
  5. Client awaits the `ACK` before streaming the next waypoint. This ensures the robot's motion planner is never overwhelmed.

## 🔌 Physical Controller Setup (CR800)

To establish raw TCP/IP communication, the physical controller MUST be properly configured to bypass the proprietary MC procedural protocol.

1. Connect your PC to the CR800 controller via Ethernet. Set your PC to a static IP on the same subnet (e.g., `192.168.0.100`).
2. Open **RT ToolBox3** -> **Parameter** -> **Communication and network**.
3. **Configure OPT12:**
   - **Device:** `OPT12`
   - **Mode:** `1: Server`
   - **Port #:** `10003`
   - **Protocol:** `0: No-procedure`
   - **Packet Type:** `0: CR`
4. **Link COMDEV:** On the right side of that same parameter window, under **Device Allocation: (COMDEV)**, change the dropdown for **COM2:** to `OPT12`. This allows the script's `OPEN "COM2:"` command to bind to the Ethernet socket.
5. **Write and Reboot:** Click **Write to Controller**, power down the physical controller box for 5 seconds, and turn it back on.

## 🧪 How to Run and Test

### 1. Test Physical Robot Movement (`test_raw_sender.ps1`)
Use this PowerShell script to manually send individual coordinates to the physical robot to verify networking and kinematics.
1. Turn **Servos ON** on the controller teach pendant.
2. Run `RobotListener.prg` on the controller. It will halt at `*WAITCONN`.
3. Open PowerShell and run `.\test_raw_sender.ps1`. It will connect to `192.168.0.20:10003`.
4. Type `MOV; -586.18;  783.00;  182.01` and press Enter.
5. The physical robot will move, and PowerShell will print `Robot says: ACK`.

### 2. Full Calligraphy Execution (C# App)
Once the manual test is successful, you can stream full toolpaths.
1. Run the C# WinForms App.
2. Enter the robot's IP `192.168.0.20` and Port `10003`.
3. Click **Connect**.
4. Type your desired text, click **Generate**, and check the visual preview.
5. Click **Execute** to stream the path asynchronously to the physical robot.

## 🧠 Why This Matters

Writing a standard `.prg` file is standard operator work. Developing a bidirectional, real-time streaming architecture is much more advanced. It demonstrates a deep understanding of network programming, parameter routing, and low-level controller integration. This architecture opens the door for computer vision integration and dynamic collision avoidance. These features are essential for modern Industry 4.0 applications.
