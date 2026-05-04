# Thaumiel Map Editor (TME)

> [!WARNING]
> This project is currently in early development. Expect bugs, missing features, and breaking changes.

Thaumiel Map Editor (TME) is a Unity based map editor designed to help developers and designers create, edit, and manage game maps. TME provides an in editor interface for placing and configuring a wide range of game objects, from interactive elements and lighting to props and navigation aids.

Our docs are available at: https://thaumiel.gitbook.io/tme-docs/

---

<table align="center" style="width: 100%; max-width: 600px; border-collapse: separate; border-spacing: 15px;">
    <tr>
        <td align="center" style="background-color: #1d1d1d; border-radius: 10px; padding: 10px; ; width: 100px;">
            <a href="README.md" 
               style="display: block; width: 100%; height: 100%; text-align: center; text-decoration: none; color: #333; cursor: pointer;">
                <img src="https://flagsapi.com/US/flat/64.png" height=30><br>
                <span style="color: #f0f0f0">English</span>
            </a>
        </td>
        <td align="center" style="background-color: #1d1d1d; border-radius: 10px; padding: 10px; width: 100px;">
            <a href="Localization/Russian.md" 
               style="display: block; width: 100%; height: 100%; text-align: center; text-decoration: none; color: #333; cursor: pointer;">
                <img src="https://flagsapi.com/RU/flat/64.png" height=30><br>
                <span style="color: #f0f0f0">Русский</span>
            </a>
        </td>
        <td align="center" style="background-color: #1d1d1d; border-radius: 10px; padding: 10px; width: 100px;">
            <a href="Localization/Spanish.md" 
               style="display: block; width: 100%; height: 100%; text-align: center; text-decoration: none; color: #333; cursor: pointer;">
                <img src="https://flagsapi.com/ES/flat/64.png" height=30><br>
                <span style="color: #f0f0f0">Español</span>
            </a>
        </td>
        <td align="center" style="background-color: #1d1d1d; border-radius: 10px; padding: 10px; width: 100px;">
            <a href="Localization/French.md" 
               style="display: block; width: 100%; height: 100%; text-align: center; text-decoration: none; color: #333; cursor: pointer;">
                <img src="https://flagsapi.com/FR/flat/64.png" height=30><br>
                <span style="color: #f0f0f0">Français</span>
            </a>
        </td>
        <td align="center" style="background-color: #1d1d1d; border-radius: 10px; padding: 10px; width: 100px;">
            <a href="Localization/Portuguese-BR.md" 
               style="display: block; width: 100%; height: 100%; text-align: center; text-decoration: none; color: #333; cursor: pointer;">
                <img src="https://flagsapi.com/BR/flat/64.png" height=30><br>
                <span style="color: #f0f0f0">Português-BR</span>
            </a>
        </td>
    </tr>
</table>

---

## Features

> Features are actively being developed. The list below reflects what is currently implemented.

- **Unity powered editor interface** — Built directly in Unity
- **Object spawning system** — Place a wide variety of typed objects onto your map from a structured spawn panel
- **Client side object support** — Certain object types are spawned on the client only and do not affect server state
- **Save & load maps** — Persist your work in a custom map format

---

## Supported Object Types

> [!NOTE]
> Objects marked as Client are spawned locally on the client and are not replicated to the server.

TME supports spawning the following object types. 

| Object Type | Scope |
|-|-|
| Doors | Server |
| Clutter | Server |
| Interactables | Server |
| Pickups | Server |
| Lockers | Server |
| Waypoints | Server |
| Cameras | Server |
| Targets | Server |
| Teleporters | Server |
| Primitives | Client |
| Lights | Client |
| Capybaras | Client |

---
## Usage & Commands

To view the full list of in-game console and admin commands, permissions, and aliases, please see our **[Commands Documentation](Commands.md)**.

## Installation

### Prerequisites

> [!IMPORTANT]
> Before getting started, make sure you have the following installed: [Unity Hub](https://unity.com/download)

### Getting Started

1. Download the repository from here: https://www.github.com/Thaumiel-Team/ThaumielMapEditorUnityProject

2. Extract the downloaded file.

3. Open the project in Unity Hub by clicking Add and selecting the extracted folder.

---

Discord Community: https://discord.gg/N8qrNHf4s9

Dependency Notices: [Click here](Dependencies.md) - (Dependencies.md)

*Thaumiel Map Editor is a work in progress. Contributions, feedback, and bug reports are welcome.*
