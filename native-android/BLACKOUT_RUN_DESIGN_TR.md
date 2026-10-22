# Derby Arena — BLACKOUT RUN (Prototype 1.0.5)

## Direction
A third-person, over-the-shoulder vehicle combat game set in an abandoned industrial floodway. The goal is not to clear a wave and stop: the player must secure three reactor cells under fire and reach the north extraction pad before the four-minute blackout window closes.

## Prototype changes
- **New location:** `BLACKOUT SPILLWAY`, a purpose-built industrial GLB with flood walls, gantries, drainage pipes, wreckage, hazard markings, beacon towers, oil-slick patches, a north extraction pad and distant industrial skyline.
- **New player vehicle:** `BREAKLINE INTERCEPTOR`, a teal armored ram car with side armor and a twin-barrel turret, exported under the runtime asset name `player_muscle.glb` to preserve loader compatibility.
- **New win condition:** secure the Pump Station, Turbine Bay and Coolant Vault cells, then reach the north extraction pad. Eliminating every enemy is not sufficient to win.
- **Pressure:** a four-minute mission clock. Player death or timeout fails the run.
- **HUD:** mission progress, next sector, extraction unlock state, target distance and remaining time.
- **Camera:** existing smooth third-person chase camera remains the default; Filament remains the real-time PBR renderer.
- **Combat:** existing vehicle steering, weapon firing, enemy AI, collisions, damage, loot and effects remain active around the new objective loop.

## Objective coordinates
Coordinates use game units, converted to world meters with `WORLD_SCALE = 0.04`:
- Pump Station: `(-300, 60)`
- Turbine Bay: `(285, 95)`
- Coolant Vault: `(-35, -265)`
- North extraction pad: `(0, -575)`

Each cell has a 62-unit collection radius; extraction has an 82-unit radius. The corresponding cyan cell props and pad are baked into the arena GLB.

## Validation performed
- New arena and player vehicle GLBs pass GLB 2.0 header/JSON checks.
- Asset pipeline `validate()` reports 0 problems, including required node names and world bounds.
- `ExtractionRunSystem` was compiled with `kotlinc` and a JVM harness passed: extraction is locked before all cells, three cells unlock extraction, and timeout fails the run.
- Android Gradle/APK build and physical-device rendering are **not verified here** because the environment could not download the Gradle distribution. GitHub Actions is configured to run the Android build and upload the debug APK; treat its green result as the next build gate, then test the APK on a device.
