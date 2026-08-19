# Card Defense

Android-first poker defense prototype built with Unity `2022.3.19f1` and URP 2D.

## Open and play

1. Open this folder with Unity `2022.3.19f1`.
2. Open `Assets/Scenes/CardDefensePrototype.unity`.
3. Enter Play Mode and press **카드 소환**.

## Implemented prototype systems

- Closed-loop monster movement; survivors persist between timed rounds.
- Monster-count loss condition.
- Kill-gold economy and paid random card summons.
- Four suits and ranks 2 through Ace.
- Poker-hand evaluator including low-Ace and high-Ace straights.
- Centralized monster/tower simulation to reduce per-object `Update` overhead.
- Prewarmed object pools for monsters and card towers.
- Android portrait, IL2CPP, ARM64, URP 2D configuration.

## Tests

- EditMode: poker-hand evaluation.
- PlayMode: prototype scene and core-system startup smoke test.

## Generated content

`Assets/CardDefense/Generated` is created by `Card Defense > Build Prototype Project` and contains the balance asset, prefabs, and the path material.

## Current prototype limits

Card merging, hand-tier upgrades, placement selection, save data, and final art/UI remain for subsequent milestones.
