# GitHub Copilot Instructions for the RimWorld Mod: Show Me Your Hands

## Mod Overview and Purpose

Show Me Your Hands is a RimWorld mod that enhances the visual representation of pawns interacting with weapons and tools. When a pawn is drafted, their hands will dynamically show on their weapons, adding a layer of immersion and realism. This mod aims to work with most weapons, both vanilla and modded, and provides settings to enable customization for individual weapon appearances.

## Key Features and Systems

- **Dynamic Hand Display:** Hands are shown on weapons based on the weapon's graphics and available hand-definitions.
- **Cross-Mod Compatibility:** Native support is available for popular mods like Enable Oversized Weapons, Yayo's Combat 3, Yayo's Animation, RunAndGun, and Dual Wield.
- **Comprehensive Mod Settings:** Users can customize settings for each weapon, including options to export these settings for integration into other mods.
- **Various Visual Enhancements:** Includes options to color hands based on apparel or limb conditions, resize hands for various pawn sizes, and adapt hand positioning for oversized weapons.
- **Compatibility with Animations:** The mod ensures hand placements are consistent even during various animation states.
  
## Coding Patterns and Conventions

- **C# Classes and Methods:** Follow object-oriented principles by using classes and methods to encapsulate functionality. For instance, `HandDrawer` class and its private methods are used for rendering hand visuals on weapons.
  
- **Method Naming Conventions:** Methods follow camelCase naming conventions, such as `DrawHandsOnWeapon` and `readXML`.

- **Public vs. Private Access Modifiers:** Methods and properties are given appropriate access modifiers to ensure encapsulation, such as public methods for external interaction and private methods for internal operations.

## XML Integration

- **XML Definitions:** XML is employed to define mod settings that can be dynamically read and altered, particularly with `ReadXML` method within `HandDrawer.cs`.

- **Exporting and Importing Settings:** The mod allows for export and import of XML settings to facilitate sharing and integration with other mods or expansions.

## Harmony Patching

- **Harmony Library:** Use Harmony to apply runtime patches to existing methods for enhanced compatibility with other mods. For instance, `CombatExtended_PawnRenderer_DrawEquipmentAiming` contains patches for better equipment rendering.

- **Patch Organization:** Keep patches logically organized and grouped by purpose, such as all animation-related patches together, to improve maintainability and clarity.

## Suggestions for Copilot

1. **Automate XML Configuration:** Create automated methods to generate or update XML configurations to simplify integration processes for other developers.

2. **Extend Compatibility Patches:** Use Copilot to suggest Harmony patches for any new mods or RimWorld updates to ensure continued compatibility.

3. **Optimize Rendering Logic:** Improve hand rendering optimizations for performance improvements using Suggestions from Copilot.

4. **UI Enhancement Ideas:** Get ideas from Copilot for enhancing the mod settings UI within the game, making it more intuitive and visually appealing.

5. **Automated Test Suggestions:** Write automated tests to validate the functionality of hand rendering and mod settings using suggestions from Copilot.

By following these instructions, Copilot can assist in creating a more robust and feature-rich mod, ensuring an enhanced visual experience for RimWorld players.
