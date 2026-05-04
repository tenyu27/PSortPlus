# AI Agent Context: PSortPlus (PSort+)

This file provides essential context, architecture details, and development guidelines for AI agents working on the **PSortPlus** (PSort+) repository.

## 1. Project Context
**PSortPlus** is a Final Fantasy XIV plugin built on the Dalamud framework. It automatically sorts the party list using customizable rules based on the current zone (territory), your job, and the party's job composition. It uses the game's built-in memory structures to sort the list directly in the game client.
- **In-Game Commands:** 
  - `/psp` triggers a manual sort.
  - `/psp ui` or `/psp config` opens the configuration window.
  - `/psp tutorial` toggles the tutorial window.

## 2. Architecture & Dependencies
The plugin is written in C# and targets the Dalamud `.NET` runtime.

### Dependencies
- **Dalamud SDK:** `Dalamud.NET.Sdk` (v14.0.1+).
- **ECommons:** (Git Submodule) Provides utilities for UI, config management (`EzConfig`), command registration (`EzCmd`), logging, and event scheduling.
- **OtterGui:** (Git Submodule) Provides advanced ImGui elements for the user interface.
- **FFXIVClientStructs:** Reverse-engineered FFXIV memory structures used for interacting with the game's UI and party data.

### Key Components
- `PSortPlus.cs`: Entry point (`IDalamudPlugin`). Handles initialization, command routing, and contains the core update loop (`OnUpdate`) which evaluates rules against the current game state and triggers `SortPartyList`.
- **Party Sorting Logic:** 
  - Reads `AgentHUD.Instance()->PartyMembers` from `FFXIVClientStructs` to get the game's actual party list UI order and jobs.
  - Uses `InfoProxyPartyMember.Instance()->ChangeOrder(currentIndex, newIndex)` to tell the game client to swap members.
- `Configuration/`: Contains the configuration schema defining profiles, rules, presets, and conditions.
- `GUI/`: Contains the ImGui-based windows for configuration and rule management.

## 3. Development Guidelines & References

When generating or modifying code for this project, refer to the following resources:

### Core References
- **Dalamud API Reference:** [https://dalamud.dev/api/](https://dalamud.dev/api/) (Check here for the latest interface definitions, breaking changes, and available services in Dalamud).
- **Goatcorp (Dalamud parent):** [https://github.com/goatcorp](https://github.com/goatcorp) | [Dalamud Repo](https://github.com/goatcorp/Dalamud/)
- **FFXIVClientStructs:** [https://github.com/aers/FFXIVClientStructs/](https://github.com/aers/FFXIVClientStructs/) (Check here when game patches alter memory offsets, such as `AgentHUD` or `InfoProxyPartyMember`).
- **ECommons:** Refer to the local submodule source or ECommons documentation for usage of `EzConfig`, `EzCmd`, `TickScheduler`, `DuoLog`, and `Svc` services.

### Patch Update Protocol (Dalamud API Updates)
When a new FFXIV patch releases, Dalamud updates its framework, which may break plugins. Follow these steps when tasked with updating the plugin:
1. **Check Tags:** Look at [https://github.com/goatcorp/Dalamud/tags](https://github.com/goatcorp/Dalamud/tags) to identify the latest stable API version matching the game patch. Update the `.csproj` SDK version accordingly.
2. **Review Structs:** Verify that memory offsets and structures (like `AgentHUD` and `InfoProxyPartyMember` in `PSortPlus.cs`) align with the latest `FFXIVClientStructs` definitions.
3. **Adapt API Changes:** Use [https://dalamud.dev/api/](https://dalamud.dev/api/) to resolve any breaking changes in `IDalamudPlugin` services or interfaces.
