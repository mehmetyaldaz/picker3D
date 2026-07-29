# Picker3D

![Picker3D main screen](Assets/Materials/mainScreen.png)

Picker3D is a mobile arcade game in which the player controls an
automatically moving collector by dragging left and right. The goal is
to gather different shapes and deliver enough of them to the Dropbox
at the end of each part.

Once the required number of objects has been deposited, the gates open
and the player can continue to the next part. After completing every
part, the player reaches the final ramp. Rapid taps increase the
player's speed and launch distance, while the reached reward zone
determines the number of gems earned.

## Gameplay

1. Tap the `Tap to Play` screen to start a level.
2. Drag left and right to collect objects.
3. Deposit enough objects into the Dropbox at the end of each part.
4. Complete every part to reach the final ramp.
5. Tap rapidly while climbing the ramp to increase launch power.
6. Land in a reward zone, earn gems, and continue to the next level.

## Features

- Infinite, continuously connected level progression
- Mobile drag controls for horizontal movement
- Randomly selected Easy, Normal, and Hard difficulties
- Object requirements based on difficulty and part order
- Three manually designed collectible layouts: A, B, and C
- Up to 60 collectible spawn points per layout
- Sphere, Cube, Cylinder, and Capsule collectible shapes
- One randomly selected shape and color for each part
- Large objects that split into smaller collectibles on player contact
- A spinner power-up that can appear randomly in each part
- Dropbox requirement and progress display
- Tap-powered final ramp and physics-based launch
- Seven reward zones with different gem rewards
- Gem balance, reward counting, and flying gem animations
- Unlockable and selectable skin/color materials
- Store, Mission, Failed, and Level Finished screens
- Collect Memes, Finish Levels, and Collect Shapes missions
- Mission reset timer that remains accurate while the app is closed
- Persistent level, gem, mission, and cosmetic progression

## Progressive Level Length

The level structure is designed to become longer as the player
progresses:

- Early levels contain 3 parts followed by the final ramp.
- Mid-game levels can contain 3 or 4 parts followed by the final ramp.
- Later levels can contain 3, 4, or 5 parts followed by the final ramp.

The 3-part, 4-part, and 5-part level template prefabs have been
prepared. Selecting the appropriate template according to the level
number is planned as part of the `LevelManager` progression system.
The selection will use a deterministic seed derived from the level
number so restarting or reopening the same level will not change its
layout length.

## Controls

- **Horizontal movement:** Hold and drag left or right
- **Ramp acceleration:** Tap the screen repeatedly
- **Menus:** Tap the relevant UI button

## Technology

- Unity `6000.0.79f1`
- Universal Render Pipeline `17.0.4`
- Unity Input System `1.19.0`
- Cinemachine `3.1.7`
- C#
- Target platforms: Android and iOS

## Getting Started

1. Clone or download the repository.
2. Open the project through Unity Hub with Unity `6000.0.79f1`.
3. Open `Assets/Scenes/Proto.unity`.
4. Press the Play button in the Unity Editor.
5. Use Device Simulator to test the mobile layout.

## Project Structure

```text
Assets/
├── Data/          Game settings, difficulties, missions, and level definitions
├── Materials/     Gameplay and mission materials
├── Prefab/        Level, part, collectible, UI, and environment prefabs
├── Scenes/        Unity scenes
└── Scripts/
    ├── Collectibles/
    ├── Core/
    ├── Level/
    ├── Missions/
    ├── Player/
    └── UI/
```

## Development Status

Picker3D is currently in active development. The next major steps are
connecting the prepared 3-part, 4-part, and 5-part templates to level
progression and completing input testing on physical mobile devices.

