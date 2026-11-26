# GitHub Copilot Instructions for Show Me Your Hands Mod

## Mod Overview and Purpose
The "Show Me Your Hands" mod enhances the visual appeal of pawns in RimWorld by dynamically displaying their hands on their weapons when drafted. This mod is compatible with both vanilla weapons and those from other mods. The mod settings allow for customization of hand properties such as color, resizing, and positioning. Additionally, it supports various other mods, including Enable Oversized Weapons, Yayo's Combat 3, Yayo's Animation, RunAndGun, Dual Wield, Combat Extended, and Melee Animation.

## Key Features and Systems
- **Dynamic Hand Display**: Pawns show their hands on their weapon based on the weapon's graphics. All hand rendering is suppressed while swimming or when standard bed/duty rules hide the body; custom mod render-worker visibility changes require separate compatibility support. Hands render after the pawn; weapon hands require current-frame draw confirmation for each wielded weapon (vanilla and Combat Extended). Custom render replacements that skip these draw paths must call `PawnRenderer_DrawEquipmentAiming.RecordWeaponDraw` as well as provide a weapon location.
- **Mod Settings**: Customizable settings for each weapon and its graphic styles, including hand color, resizing, and positioning. The weapon preview keeps rotated textures aligned at different UI scales; the weapon selector includes a localized search hint.
- **Compatibility**: Works with various other mods like Enable Oversized Weapons, Yayo's Combat 3, Yayo's Animation, RunAndGun, Dual Wield, Combat Extended, and Melee Animation.
- **Exportable Settings**: Mod settings can be exported and included in other mods to add native support. Style-specific `WeaponCompLoader` entries specify both `ThingTargets` and `StyleTargets`; manual values take precedence over definitions and generated positions.

## Coding Patterns and Conventions
- **C# Files**: The mod is primarily written in C# and follows a structured pattern with classes and methods for specific functionalities. For example, the `ClutterHandsTDef.cs` file defines the hand definitions, while the `PawnRenderUtility_DrawCarriedThing.cs` file handles the rendering of carried things with hands.

- **XML Files**: The mod uses XML files for defining hand positions and other settings. Each XML file corresponds to a specific mod or weapon type and contains definitions for hand positions and other properties.

- **Harmony Patching**: The mod uses Harmony patching to modify the behavior of existing methods in the RimWorld codebase. This allows the mod to integrate seamlessly with the game without modifying the original code.

## XML Integration
The mod uses XML files to define hand positions and other settings. Each XML file corresponds to a specific mod or weapon type and contains definitions for hand positions and other properties. For example, the `Anomaly.xml` file defines hand positions for weapons from the Anomaly mod, while the `Core.xml` file defines hand positions for vanilla weapons.

## Harmony Patching
The mod uses Harmony patching to modify the behavior of existing methods in the RimWorld codebase. This allows the mod to integrate seamlessly with the game without modifying the original code. The `CombatExtended_PawnRenderer_DrawEquipmentAiming.cs` file, for example, contains a Harmony patch that modifies the `DrawEquipmentAiming` method in the `CombatExtended_PawnRenderer` class to display hands on weapons during aiming animations.

## Suggestions for Copilot
- **C# Snippets**: Copilot can generate C# code snippets for common tasks, such as defining new classes, methods, and properties. This can save developers time and effort when writing new code.
- **XML Snippets**: Copilot can generate XML code snippets for defining new hand positions and other settings. This can be particularly useful for developers who are new to XML and want to quickly create new hand definitions.
- **Harmony Patch Snippets**: Copilot can generate Harmony patch snippets for modifying existing methods in the RimWorld codebase. This can save developers time and effort when creating new patches for the mod.

## Conclusion
The "Show Me Your Hands" mod is a powerful tool for enhancing the visual appeal of pawns in RimWorld. By following the coding patterns and conventions outlined in this document, developers can create new features and functionality for the mod, as well as contribute to the wider RimWorld modding community.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Temporary files

- Do not create temporary files, build output, test artifacts, or Copilot session
  folders inside the mod root.
- Store temporary artifacts under `%USERPROFILE%\CopilotTemp\[modname]`.
- Direct build and test output there as well, without changing project settings.
- Clean up task-specific temporary artifacts when finished.

## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

