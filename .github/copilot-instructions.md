# GitHub Copilot Instructions for Show Me Your Hands

## Mod Overview and Purpose

**Mod Name:** Show Me Your Hands  
**Author:** Mlie  
**Description:**

"Show Me Your Hands" enhances the visual representation of pawns in RimWorld by allowing them to display their hands on weapons when drafted. This mod aims to improve the game's immersion and aesthetic by dynamically setting hand positions based on weapon graphics, ensuring compatibility with most weapons, whether vanilla or modded.

## Key Features and Systems

1. **Dynamic Hand Positioning:**
   - Automatically adjusts hand positions based on weapon graphics.
   - Utilizes pre-existing hand definitions for prior setups.

2. **Mod Settings Customization:**
   - Customize hand settings for each weapon, including exporting and importing these configurations to other mods for native support.
   - Full support for "Continued" mods.

3. **Compatibility and Integration:**
   - Compatible with multiple mods, including "Enable Oversized Weapons", "Yayo's Combat 3", "RunAndGun", "Dual Wield", and "Combat Extended".
   - Ensures hands follow weapon animations during additional movements.

4. **Visual Customizations:**
   - Options for hand coloring based on apparel/armor and artificial limbs.
   - Resizing based on pawn body size, compatible with children.
   - Hand repositioning for oversized weapons.
   - Configurations for hand display when pawns are carrying objects, missing hands, or always showing hands.

5. **Translations and Texture Support:**
   - Includes hand definitions and texture updates, with contributions from various community members.
   - Chinese translation by shiuanyue.

## Coding Patterns and Conventions

- Use clean and readable C# code organized into relevant namespaces and classes.
- Follow naming conventions such as PascalCase for class names and methods, camelCase for local variables.
- Maximize modularity and reusability of code components.
- Ensure compatibility with the Harmony library for non-intrusive modding.

## XML Integration

- XML is heavily used to define hand positions for various scenarios within the mod.
- Definitions including `WHands.ClutterHandsTDef` are organized for different settings and mods like "Anomaly", "Biotech", etc.
- Ensure XML files have consistent indentations and are well-commented to facilitate understanding.

## Harmony Patching

- Harmony is used to patch existing game methods to allow dynamic hand positioning:
  - Example types include `CombatExtended_PawnRenderer_DrawEquipmentAiming` for integrating with Combat Extended.
- Harmony patches should identify and appropriately target methods without disrupting core functionality.

## Suggestions for Copilot

- **C# Development:**
  - Assist in generating boilerplate code for new feature inductions.
  - Provide suggestions for Harmony patch methods and setup.
  - Recommend optimizations for existing methods to improve performance and readability.

- **XML Development:**
  - Auto-generate XML definitions with proper styling and structure.
  - Suggest additions to existing XML files based on detected patterns in new mod integrations.

- **Debug and Testing Support:**
  - Offer unit test templates for validating feature implementations.
  - Suggest debugging strategies within complex Harmony patched classes.

By adhering to these instructions, you'll maximize the efficiency of your development process, ensuring your mod is robust, user-friendly, and compatible with a wide range of scenarios and other mods.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

