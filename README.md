# 6-DOF Robotic Calligraphy: Asynchronous TCP Coordinate Streaming

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![Mitsubishi Electric](https://img.shields.io/badge/Mitsubishi_Electric-E60012?style=for-the-badge&logo=mitsubishielectric&logoColor=white)
![TCP/IP](https://img.shields.io/badge/TCP/IP-000000?style=for-the-badge&logo=tcp%2Fip&logoColor=white)

This repository contains the software architecture for real-time robotic calligraphy using a Mitsubishi Electric CR800 Controller and a 6-DOF robotic arm. 

By default, industrial robot controllers rely heavily on pre-programmed waypoints taught via a physical teach pendant or offline static `.prg` files. This project bypasses those traditional limitations by implementing a **Custom Asynchronous TCP Socket Server** in C#, enabling real-time, dynamic toolpath generation and execution.

## 🚀 Architecture Overview

The system consists of two primary components communicating over a standard TCP/IP socket:

1. **C# Coordinate Engine (Client)**: Translates user text input into continuous spatial toolpaths (utilizing a custom single-stroke vector font engine) and streams the physical coordinates `(X, Y, Z)` asynchronously to the robot.
2. **MELFA BASIC VI Listener (Server)**: A lightweight daemon script running continuously on the CR800 controller. It listens on port `10003`, parses incoming ASCII coordinate strings, and immediately commands the servo drives using joint (`MOV`) or linear (`MVS`) interpolation.

### The Handshake & Communication Protocol

- **Connection**: The C# Client connects to the CR800 Controller (or RT ToolBox3 Virtual Simulator) on `IP: 192.168.0.20` (or `127.0.0.1`), Port: `10003`.
- **Packet Structure**: Coordinates are formatted as fixed-length strings to minimize parsing overhead on the controller. 
  Example: `MVS; -500.00;  850.00;  126.55`
- **Execution Loop**:
  1. Client sends a coordinate packet.
  2. Controller parses the string, updates the target position vector (`P2`), and executes the move.
  3. Controller replies with an `ACK` string.
  4. Client awaits the `ACK` before streaming the next waypoint, ensuring the robot's motion planner is never overwhelmed.

## 🛠️ Testing with RT ToolBox3 (Digital Twin)

If you don't have access to the physical CR800 box, you can run the full system using the built-in digital twin in RT ToolBox3:

1. **Start the Simulator**: Open your workspace in RT ToolBox3 and click the **Simulator** button to launch the virtual controller.
2. **Configure the Listener**: Load `RobotListener.prg` into the simulator and run it. It will block and wait at `M_Open(1) = 0` for a connection.
3. **Launch the C# App**: Build and run the `RobotCalligraphyApp`. The default IP is set to `127.0.0.1` to target the local simulator.
4. **Connect & Stream**: Click **Connect**. Once the status shows "Connected", type your text, hit **Generate**, and then click **Execute**. 
5. **Watch it Work**: You will see the 3D virtual arm execute the `MVS` commands on your screen in real time!

## 🧠 Why This Matters

Writing a standard `.prg` file is standard operator work. Developing a bidirectional, real-time streaming architecture demonstrates a deeper understanding of network programming, coordinate math, and low-level controller integration. This architecture opens the door for computer vision integration, real-time sensor feedback, and dynamic collision avoidance—features essential for modern Industry 4.0 applications.
