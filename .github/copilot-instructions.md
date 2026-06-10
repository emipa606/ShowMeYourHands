# GitHub Copilot Instructions for "Show Me Your Hands" Mod

## Mod Overview and Purpose

**Mod Name**: Show Me Your Hands  
**Author**: Mlie  
**Package ID**: Mlie.ShowMeYourHands  

The "Show Me Your Hands" mod for RimWorld enhances the visual realism of the game by displaying pawns' hands on their weapons when they are drafted. This mod dynamically adjusts hand positions based on the graphics of the weapon, extending compatibility to both vanilla and most modded weapons. It builds upon the concept of Clutter Weapon Hands with dynamic flexibility, offering settings that can be customized per weapon.

## Key Features and Systems

- **Dynamic Hand Display**: Automatically positions hands on weapons for a wide range of mods and vanilla content.
- **Customizable Settings**: Mod settings allow for fine-tuning of hand display attributes and can be exported for integration with other mods.
- **Compatibility**: Supports a variety of mods including, but not limited to, Enable Oversized Weapons, Yayo's Combat 3, RunAndGun, Dual Wield, Combat Extended, and Melee Animation.
- **Visual Enhancements**: Options for hand coloring based on apparel, artificial limbs, and resizing based on pawn body size.
- **Additional Functionality**: Can display hands when pawns carry items, with adjustments for missing limbs or oversized weapons.

## Coding Patterns and Conventions

- **C# Patterns**: Follow typical C# conventions for readability and maintainability. Use meaningful identifiers and camelCase or PascalCase as appropriate.
- **Type Definitions**: Utilize organized type definitions as seen in classes like `BigAndSmallFramework`, `ClutterHandsTDef`, and `HandDrawer`.
- **Member Functions**: Member functions such as `DrawHandsOnWeapon` and `GetModifiedSize` should be user-friendly and reflect their purpose clearly.

## XML Integration

XML files are pivotal for storing definitions and hand positions specific to mod conditions. The files found under `.../Defs/HandPositions/` include configurations like:
- `Anomaly.xml`
- `Biotech.xml`
- and others.

These XML files use `WHands.ClutterHandsTDef` to define hand configurations per weapon, enabling integration and easy expansion with other mods.

## Harmony Patching

The mod employs the Harmony library (`brrainz.harmony`) for patching, allowing for safe injection of code modifications into existing game methods. Key patched methods should be articulated using `Postfix`, `Prepare`, and `TargetMethod`, ensuring seamless integration without conflicts.

Key files using Harmony include:
- `CombatExtended_PawnRenderer_DrawEquipmentAiming.cs`
- Ensure patches do not override critical game functions unless absolutely necessary.

## Suggestions for Copilot

When using GitHub Copilot within this mod, consider the following suggestions:

1. **Leverage XML Definitions**: Suggest XML code that adheres to RimWorld’s schema for defining new hand positions.
2. **Suggest Harmony Patches**: Generate common Harmony patch structures, including Postfix methods.
3. **UI Enhancements**: Suggest improvements in `HandDrawer` methods for enhancing visual rendering of hands.
4. **Debugging Assistance**: Provide suggestions for potential debug outputs or logging strategies to trace graphical issues.
5. **Compatibility Checks**: Include code snippets for verifying compatibility with known popular mods.

By providing structured input and guidelines to Copilot, you can maximize productivity and maintain high code quality throughout the "Show Me Your Hands" mod development process.

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

