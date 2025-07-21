# GitHub Copilot Instructions for RimWorld Modding in C#

## Mod Overview and Purpose
This mod is designed to enhance the visual representation of pawns in RimWorld by adding more detailed hand and weapon interactions. The primary goal is to improve immersion by allowing weapons to be rendered in a way that reflects their use by the pawn, such as adjusting positions, angles, and rendering states. 

## Key Features and Systems
- **Hand Rendering**: Adds functionality to draw hands on pawns when they are using weapons or other items.
- **Weapon Aiming**: Provides a refined method for rendering weapons during pawn interactions, enhancing the visual detail of combat situations.
- **Customization Options**: Users can adjust settings related to the rendering of hands and weapons through a graphical interface in the game.
- **Compatibility**: Integrates with existing mods, such as Combat Extended, to maintain stability and functionality across modded installations.

## Coding Patterns and Conventions
- **File Naming and Organization**: Class files are named after the primary class they contain, following CamelCase naming conventions.
- **Static Helper Classes**: Static classes are used for utility functions and extensions, such as `ListingExtension` and `MemoryUtility_UnloadUnusedUnityAssets`.
- **Modular Design**: Different responsibilities are divided into separate classes, such as `HandDrawer` for hand rendering logic and `ShowMeYourHandsModSettings` for mod settings management.
- **Access Modifiers**: Use of `public`, `private`, and `internal` to control access and encapsulate logic appropriately.

## XML Integration
While the XML integration details are not explicitly mentioned in the class summaries, it's typical in RimWorld mods to use XML files to define or extend game data such as `ThingDef` or `CompProperties`. This allows mod authors to describe new items, crafting recipes, and other in-game entities without hardcoding them in C#. In this mod:
- Use XML to extend or modify existing RimWorld `ThingDef` entries or to introduce new ones associated with hand and weapon rendering.
- XML files should live in a `Defs` folder, following RimWorld's mod structure.

## Harmony Patching
- **Purpose**: Harmony is used to apply runtime patches to the RimWorld's base game code, allowing developers to alter game mechanics without modifying the original source.
- **Examples in Mod**: The mod likely applies patches using the `CombatExtended_PawnRenderer_DrawEquipmentAiming` static class, which hooks into the rendering process and modifies how equipment is drawn when aiming.
- **Implementation**: Ensure the correct setup of Harmony by creating a Harmony instance, identifying the target methods to patch via reflection, and implementing prefix, postfix, or transpiler methods as needed.

## Suggestions for Copilot
- **Refactoring Assistance**: Suggest refactorings that create reusable methods for repetitive rendering tasks or utility functions.
- **Code Completeness**: Offer code completions that adhere to the established coding patterns and styles found in the project.
- **Harmony Management**: Assist in generating template code for Harmony patching, with clear comments indicating where to apply patches, and use of reflection for method identification.
- **Error Handling**: Propose robust error handling patterns, especially when dealing with rendering operations and external mod integrations.
- **Optimization Tips**: Suggest performance improvements, such as lazy computations or better resource management when rendering dynamic content like hands and weapons.

Always review Copilot suggestions to ensure they match the mod's design philosophy and maintain performance objectives.
