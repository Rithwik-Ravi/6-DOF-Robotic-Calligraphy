# 6-DOF Robotic Calligraphy: Asynchronous TCP Coordinate Streaming

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![Mitsubishi Electric](https://img.shields.io/badge/Mitsubishi_Electric-E60012?style=for-the-badge&logo=mitsubishielectric&logoColor=white)
![TCP/IP](https://img.shields.io/badge/TCP/IP-000000?style=for-the-badge&logo=tcp%2Fip&logoColor=white)

This repository contains the software architecture for real-time robotic calligraphy. It uses a Mitsubishi Electric CR800 Controller and a 6-DOF robotic arm. 

Industrial robot controllers usually rely on static, pre-programmed waypoints. This project bypasses those limitations. It implements a Custom Asynchronous TCP Socket Server in C#. This allows for real-time, dynamic toolpath generation and execution.

## 🚀 Architecture Overview

The system has two primary components communicating over a standard TCP/IP socket:

1. **C# Coordinate Engine (Client)**: Translates user text input into continuous spatial toolpaths. It uses a custom single-stroke vector font engine. It then streams the physical coordinates `(X, Y, Z)` asynchronously to the robot.
2. **MELFA BASIC VI Listener (Server)**: A lightweight script running continuously on the CR800 controller. It listens on port `10003` and parses incoming ASCII coordinate strings. It commands the servo drives using joint (`MOV`) or linear (`MVS`) interpolation immediately.

### The Handshake & Communication Protocol

- **Connection**: The C# Client connects to the CR800 Controller (or simulator) on `IP: 192.168.0.20` (or `127.0.0.1`), Port: `10003`.
- **Packet Structure**: Coordinates are formatted as fixed-length strings. This minimizes parsing overhead on the controller. 
  Example: `MVS; -500.00;  850.00;  126.55`
- **Execution Loop**:
  1. Client sends a coordinate packet.
  2. Controller parses the string and updates the target position vector (`P2`).
  3. Controller executes the physical move.
  4. Controller replies with an `ACK` string.
  5. Client awaits the `ACK` before streaming the next waypoint. This ensures the robot's motion planner is never overwhelmed.

## 🧪 How to Run and Test

You can test the system components using the included PowerShell scripts and the RT ToolBox3 simulator.

### 1. Test the C# App with Mock Server (`test_server.ps1`)
Use this to test the C# application without needing the actual robot simulator.
- Open PowerShell and run `.\test_server.ps1`.
- It will start a mock server on port `5555`.
- Open the C# App, set IP to `127.0.0.1` and Port to `5555`. 
- Click **Connect**, then **Generate**, and finally **Execute**.
- The PowerShell window will print the coordinates it receives from the app.

### 2. Test the Simulator Listener (`test_raw_sender.ps1`)
Use this to send manual coordinates to the RT ToolBox3 simulator to verify movement.
- Load and run `RobotListener.prg` in your RT ToolBox3 simulator.
- Open PowerShell and run `.\test_raw_sender.ps1`.
- Type `"MOV; -500.00;  700.00;  200.00"\r` and hit Enter.
- The virtual robot should move, and you will see an `ACK` printed back.

### 3. Test App Connection (`test_client.ps1`)
Use this as a lightweight client to check if the robot simulator is broadcasting.
- Load and run `RobotListener.prg` in RT ToolBox3.
- Open PowerShell and run `.\test_client.ps1`.
- It will connect to the robot on port `10003` and listen for any responses.

## 🧠 Why This Matters

Writing a standard `.prg` file is standard operator work. Developing a bidirectional, real-time streaming architecture is much more advanced. It demonstrates a deep understanding of network programming and low-level controller integration. This architecture opens the door for computer vision integration and dynamic collision avoidance. These features are essential for modern Industry 4.0 applications.
