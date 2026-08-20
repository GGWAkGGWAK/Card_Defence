# Card Defense

Android-first poker defense prototype built with Unity `2022.3.19f1` and URP 2D.

## Open and play

1. Open this folder with Unity `2022.3.19f1`.
2. Open `Assets/Scenes/CardDefensePrototype.unity`.
3. Enter Play Mode and press **소환**.

## Implemented prototype systems

- Closed-loop monster movement; survivors persist between timed rounds.
- Normal, fast, tank, gold, and boss monster archetypes with distinct health, speed, reward, color, and size.
- Every tenth round begins with a boss; all monsters show world health bars and the HUD shows active boss HP.
- Queued overlapping waves retain the health, reward, and archetype rules of their original round.
- Monster-count loss condition.
- Kill-gold economy and paid random card summons.
- Four suits and ranks 2 through Ace.
- Poker-hand evaluator including low-Ace and high-Ace straights.
- Centralized monster/tower simulation to reduce per-object `Update` overhead.
- Prewarmed object pools for monsters and card towers.
- Android portrait, IL2CPP, ARM64, URP 2D configuration.
- Mouse/touch card selection and free five-card poker merging.
- Poker-hand tower grades with escalating damage multipliers.
- Unlimited per-hand gold upgrades for the current run.

## Prototype controls

- **소환**: enters placement mode; tap/click a preferred empty slot to spend gold and place the card.
- Card tap/click: selects or deselects up to five field cards.
- Card drag to an empty slot: moves the tower without cost.
- Card drag onto an occupied slot: swaps the two tower positions.
- **5장 합성**: consumes five selected cards and creates one poker-hand tower.
- **족보 강화**: upgrades the selected card's hand grade for every matching tower.
- **판매**: removes the focused tower, immediately frees its slot, and refunds part of its value.
- With five cards selected, the HUD previews the resulting poker hand before merging.
- A five-card fusion result is final: it shows a star, cannot be selected for another merge,
  and can only be used as the target hand for upgrades.
- A merge is blocked when the five selected cards contain the exact same suit and rank more than once.
  Equal ranks with different suits remain valid.
- After defeat, **새 게임** reloads the gameplay scene and resets the run.
- **x1/x2/x4** cycles gameplay speed without a pause mode.

## Balance report

Run `Card Defense > Generate Round Balance CSV` to export rounds 1-100 to
`Docs/Balance/round_balance_1_100.csv`. The report includes per-monster HP, total wave HP,
kill rewards, cumulative gold, summon equivalents, and DPS required to clear within one round interval.

Run `Card Defense > Generate Poker Probability CSV` to export a deterministic 2,000,000-trial
simulation matching the game's independent random summons to `Docs/Balance/poker_merge_probability.csv`.

## Tests

- EditMode: poker-hand evaluation.
- PlayMode: prototype scene and core-system startup smoke test.

## Generated content

`Assets/CardDefense/Generated` is created by `Card Defense > Build Prototype Project` and contains the balance asset, prefabs, and the path material.

## Current prototype limits

Manual placement, save data, advanced effects, final art/UI, and detailed balance tuning remain for subsequent milestones.
