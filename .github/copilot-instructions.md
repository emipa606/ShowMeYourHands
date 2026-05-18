# GitHub Copilot Instructions for "Show Me Your Hands" Mod

## Mod Overview and Purpose
"Show Me Your Hands" is a mod for RimWorld designed to enhance the visual realism of drafted pawns by displaying their hands on their weapons. The mod works seamlessly with most weapons, both vanilla and modded, dynamically adapting based on the graphics of the weapon. The mod aims to improve player immersion by adding a touch of realism to weapon handling visuals.

## Key Features and Systems
- **Dynamic Hand Placement:** Automatically positions pawn hands on weapons based on weapon graphics. Utilizes predefined hand-definitions if available.
- **Compatibility with Other Mods:** Supports a range of popular mods including Enable Oversized Weapons, Yayo's Combat 3, Yayo's Animation, RunAndGun, Dual Wield, and Combat Extended. Integrates with animations to maintain hand positioning.
- **Customizable Settings:** Allows players to modify settings for each weapon through mod-settings, which can be exported for use in other mods.
- **Visual Adjustments:** Features options for coloring hands based on apparel, resizing based on pawn size, and repositioning for oversized weapons. Supports showing hands at all times, including when carrying items or when a pawn has only one hand.

## Coding Patterns and Conventions
- **Consistent Naming:** Classes and methods are named to clearly reflect their function, e.g., `HandDrawer`, `DrawHandsOnWeapon`. This aligns with C# naming conventions.
- **Encapsulation:** Use of internal classes such as `ShowMeYourHandsMod` and `ShowMeYourHandsModSettings` to encapsulate mod-specific functionality.
- **Static Utility Classes:** Leverage static classes for utility functions, such as `CombatExtended_PawnRenderer_DrawEquipmentAiming`.

## XML Integration
- Utilize XML for defining mod settings and configurations. For example, hand-definitions are likely stored and accessed using XML.
- Methods like `ReadXML()` in the `HandDrawer` class suggest integration points for reading configuration files.

## Harmony Patching
- Harmony patches are applied to modify and extend existing RimWorld functionality. This is essential for visual changes and mod interoperability.
- Patching examples include changes to how equipment is drawn (`PawnRenderer_DrawEquipmentAiming`) and modifying main menu display (`RimWorld_MainMenuDrawer_MainMenuOnGUI`).

## Suggestions for Copilot
To optimize use of GitHub Copilot for developing this mod:
- **Focus Suggestions on Common Operations:** Most code alterations will revolve around drawing hands, reading configurations, and patching compatibility. Encourage Copilot to suggest ways to streamline these tasks.
- **Encourage Pattern Recognition:** Enhance patterns for dynamic drawing adjustments and compatibility handling.
- **XML Handling and Harmony Enhancements:** Provide assistance in generating XML handling functions and expanding Harmony patch methods.
- **Namespace Assistance:** Ensure that Copilot can help maintain consistent namespace organization across the mod's diverse features.
- **Optimize Method Implementations:** Suggest efficient implementations for methods that handle rendering and settings application.

## Additional Resources
- **Credits:**
  - Arcanant: Hand-definitions.
  - Telefonmast: Texture-code support.
  - qux: Updated hand-texture.
  - shiuanyue: Chinese translation.
  - velcroboy333: XCOM armor and weapons used in previews.

For further customization and realistic hand textures, explore Nice Hands Retexture by Andromeda.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.
