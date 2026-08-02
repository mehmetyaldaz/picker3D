# Picker3D

![Picker3D main screen](Assets/Materials/mainScreen.png)

Picker3D is a mobile arcade game built with Unity. The player controls
an automatically moving U-shaped collector, gathers physics-based
objects, and delivers enough of them to the Dropbox at the end of each
part.

Completing every part opens the final ramp. Repeated taps increase ramp
speed and launch distance, and the highest reward zone touched determines
the gem reward. The next level is prepared ahead of the player so the
game can continue indefinitely without returning to a level-selection
screen.

## Gameplay Loop

1. Start from the `Tap to Play` screen.
2. Drag left and right to collect objects while the player moves forward.
3. Reach the Dropbox and deposit the required number of objects.
4. Cross the raised bridge and continue through every part.
5. Tap repeatedly on the final ramp to increase launch power.
6. Land on the reward ground and receive the highest reward zone touched.
7. Continue through the `Level Finished` screen to the next prepared level.

Failing a Dropbox requirement opens the `Failed` screen. Continuing resets
the current level, its part progress, generated content, and player state.

## Main Features

- Infinite, continuously connected level progression
- Deterministic level generation based on the saved level number and seed
- Mobile drag input with controlled horizontal movement restrictions near
  each Dropbox
- Tap-powered final ramp, physics launch, landing, and recovery animation
- Random Easy, Normal, or Hard difficulty for every level
- Difficulty-based requirements for as many as five parts
- Small, large, and flying collectible generation modes
- Manually authored small-object layouts with up to 60 spawn points
- One randomly selected collectible shape and color per part
- Sphere, Cube, Cylinder, and Capsule collectible prefabs
- Large objects that split into their smaller versions on player contact
- A flying spawner that follows a manual route and drops collectibles
- Random spinner power-up support for each part
- Dropbox progress text, gate animation, bridge extension, and clear particles
- Seven final-ramp reward zones and animated gem collection
- Persistent gem balance, level progress, missions, and cosmetics
- Randomly unlockable and selectable skin/color materials
- Tap to Play, Store, Mission, Failed, and Level Finished screens
- Collect Memes, Finish Levels, and Collect Shapes missions
- Offline-safe mission reset timer based on saved UTC timestamps
- Random outside-view templates and shared randomized environment colors

## Progressive Level Length

Three reusable level templates are active:

| Level range | Available templates |
| --- | --- |
| 1-10 | 3 parts |
| 11-20 | 3 or 4 parts |
| 21+ | 3, 4, or 5 parts |

The unlock levels are Inspector settings on `LevelManager` and currently
default to Level 11 and Level 21. Template selection is deterministic, so
restarting or reopening the same level keeps the same part count.

The current difficulty configurations require the following counts:

| Difficulty | Part 1 | Part 2 | Part 3 | Part 4 | Part 5 |
| --- | ---: | ---: | ---: | ---: | ---: |
| Easy | 10 | 15 | 20 | 25 | 30 |
| Normal | 20 | 25 | 30 | 35 | 40 |
| Hard | 30 | 35 | 40 | 45 | 50 |

Each configuration currently generates 10 more objects than the required
amount.

## Part Visual Variations

Every `LevelPart` contains a local URP Volume. Its Channel Mixer appearance
is selected deterministically for that part:

- Normal/default rendering
- Red filter
- Green filter
- Black-and-white filter

`Volume Effect Chance` controls how often a filter is used. The default is
`0.5`, meaning 50% filtered and 50% normal. `Red Style Weight`,
`Green Style Weight`, and `Black And White Style Weight` control the relative
distribution inside the filtered 50%.

## Missions

The current mission types are:

- Collect a mission collectible (`Collect Memes`)
- Finish a number of levels
- Deposit a number of shapes into Dropboxes

Mission targets and rewards are selected from Inspector-configurable option
lists. Progress, selected targets, rewards, completion, and claim state are
stored with `PlayerPrefs`. The reset deadline is saved as UTC time, so elapsed
time remains correct while the app is closed. The reset interval is currently
Inspector-configurable in minutes.

## Store and Cosmetics

The Store contains Skin and Color sections. Both categories apply materials
to the player:

- Random skin unlock: 3000 gems by default
- Random color unlock: 2000 gems by default
- Locked and selected overlays
- Toggle the selected cosmetic off to restore the original player material
- `Out of Stock` state after every item in a category is unlocked

Unlocks and the selected cosmetic persist between sessions.

## Save Data

Runtime progression is stored locally with Unity `PlayerPrefs`, including:

- Current level number
- Total gem balance
- Mission progress, rewards, claims, and reset deadline
- Unlocked cosmetics and the selected material

The Tap to Play screen also contains development controls for resetting
progress and adding levels or gems during testing.

## Controls

- **Horizontal movement:** Hold and drag left or right
- **Ramp acceleration:** Tap repeatedly while climbing the final ramp
- **Menus:** Tap the relevant UI button

The project is designed around portrait mobile input and can be tested with
Unity Device Simulator.

## Technology

- Unity `6000.0.79f1`
- Universal Render Pipeline `17.0.4`
- Unity Input System `1.19.0`
- Cinemachine `3.1.7`
- C#
- Target platforms: Android and iOS

## Getting Started

1. Clone or download the repository.
2. Open it through Unity Hub with Unity `6000.0.79f1`.
3. Open `Assets/Scenes/Proto.unity`.
4. Enter Play Mode.
5. Use Device Simulator with a portrait mobile device profile.

## Project Structure

```text
Assets/
|-- Data/          Difficulty, mission, level, and collectible settings
|-- Materials/     Gameplay, UI, cosmetic, and mission materials
|-- Prefab/        Levels, parts, collectibles, UI, and environment prefabs
|-- Scenes/        Unity scenes
`-- Scripts/
    |-- Collectibles/  Collection, layouts, splitting, and flying drops
    |-- Core/          Game state, wallet, and restart services
    |-- Cosmetics/     Unlocks, selection, and player material persistence
    |-- Data/          ScriptableObject configuration types
    |-- Level/         Infinite levels, parts, ramp, rewards, and generation
    |-- Missions/      Mission definitions, progress, spawning, and timer
    |-- Player/        Drag/tap input, movement, ramp physics, and power-up
    |-- UI/            Screen routing, HUDs, store, and mission presentation
    `-- World/         Gates, bridge, Dropbox, and surface visuals
```

## Development Status

Picker3D is in active development. The core infinite gameplay loop, 3/4/5
part progression, missions, store, persistence, and procedural variations are
implemented. Remaining work primarily consists of mobile-device testing,
performance profiling, content balancing, UI polish, and release preparation.
