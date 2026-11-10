SPACE MISSION DESIGNER — COMPLETE EDITION
Unity Editor: 6000.6.8f1

WHAT IS INCLUDED
- Main menu, mission selection, equipment store, assembly, validation, launch, mission control and results.
- Three mission profiles: EARTH observation, MOON surface expedition, MARS surface expedition.
- Earth: orbital observation mission with cloud-cover, orbit-drift and data-downlink decisions.
- Moon: transfer decisions, powered descent, EVA astronaut controls, rover, three sample sites, terrain scan, beacon deployment and safe return.
- Mars: deep-space transfer decisions, a dedicated Martian atmospheric-sky treatment and rust-red terrain/rock palette, Jezero exploration profile, EVA controls, rover, three rock-core sites, regolith scan, weather/navigation beacon and safe return.
- Launch sequence with 3D rocket assembly, animated ascent, exhaust/smoke, booster separation, Earth limb and starfield transition.
- Audio clips for UI, countdown, ignition, engine loop, liftoff, radio and surface ambience.
- Resource textures for lunar ground, lunar panorama and Earth diffuse map.

HOW TO OPEN
1. Extract this ZIP to a new folder.
2. In Unity Hub choose Add > Add project from disk.
3. Select the extracted folder containing Assets, Packages and ProjectSettings.
4. Open with Unity 6000.6.8f1.
5. Open Assets/Project_/Scenes/MainMenu.unity and press Play.

CONTROLS — SURFACE EXPEDITION
WASD: move / steer rover
Right mouse: look
Shift: sprint / rover boost
Space: low-gravity jump
E: collect a geology sample
F: scan terrain / regolith
B: deploy beacon
Q: enter or exit rover
R: return to lander after completing objectives

ENGINEERING / ECONOMY MODEL
Credits (CR), equipment prices, rocket capacities, equipment masses, power draw/generation, event deltas, science data and scores are game-balance values. They are not quoted procurement prices, official NASA specifications, flight predictions or validated engineering calculations.

Core rules used by the game:
- Payload utilization (%) = selected payload mass / selected vehicle's simulated capacity * 100.
- Power margin (kW) = selected power generation - selected system draw.
- Mission validation checks required categories, payload capacity, minimum power margin and mission-specific vehicle requirements.
- Flight choices change power, communication link, vehicle integrity, science data, elapsed days and score.
- Surface expedition completion requires collecting 3 geology samples, scanning the terrain, deploying a beacon and returning to the lander with adequate oxygen, suit power and communication.

IMPORTANT LIMITATIONS
This is a playable Unity prototype / educational mission-design simulator, not an aerospace-grade simulator. The astronaut, rocket, lander and rover are procedural Unity meshes rather than scanned or commercial photorealistic FBX assets. Mars and lunar terrain are procedural meshes/textures. Mars descent is represented as a simplified powered-descent sequence; it does not calculate real atmospheric entry, parachute sizing, orbital mechanics, radiation or thermal protection. Earth observation is an orbital decision mission, not a free-roam Earth surface mission.

TESTING NOTE
The project was assembled and statically inspected in this environment, but the Unity Editor was not available here for a full compile/playtest on the target machine. Open it in Unity and inspect Console first; resolve any environment/package-specific warnings or errors before building a submission.
