# First Light

A top-down light-reflection puzzle game, built from the *First Light* design document.

The sun has gone dark. You are the last spark of light, cast down from the sky. You cannot
carry the light — a beam enters each room from a fixed source and is always on. You walk to
the mirrors, turn them, and bend that beam onto plates, receivers, switches, the red eyes
hidden in the darkness, and finally the way out.

## Running it

Open the project in Unity **6000.4.2f1**, open `Assets/Scenes/FirstLight.unity` and press play.
Nothing needs wiring in the editor — the scene holds one `GameManager` object and everything
else (camera setup, sprites, levels, HUD, sound) is generated at runtime.

| Input | Action |
| --- | --- |
| `WASD` / arrows | move |
| `E` or `Space` | turn the nearest mirror counter-clockwise |
| `Q` | turn it back |
| `R` | restart the level |
| `[` `]` | previous / next level |
| `Esc` | back to the title |

On Android and iOS an on-screen pad appears instead: a direction cross on the left,
two buttons on the right that turn the nearest mirror either way, and a restart ring
in the corner. Tap anywhere to start or to play again.

On mobile it also locks to landscape — both ways up, so the device can still be flipped —
because the levels are wide and portrait would shrink them to nothing.

It appears wherever a touchscreen is actually used, not by platform. A build for a
phone knows it is one; a build served *to* a phone through a browser does not, because
there the platform only ever reports WebGL — so the pad also comes out the moment the
screen is touched, and steps back out of the way if the player then reaches for the
keyboard, which covers a laptop that is both.

With nothing ever touching the screen (`GameManager.touchControls`, default `Auto`) a
desktop build behaves exactly as it always did — the pad reserves no screen, reads no
pointers and draws nothing. `Always` forces it on if you want to try it in the editor
with the mouse standing in for a finger.

Two details worth knowing if you touch that code. It hit-tests the Input System's
touches itself rather than using IMGUI buttons, which keeps it independent of whether
IMGUI receives pointer events under the new input backend, and means one thumb can
hold a direction while the other turns a mirror. And the camera pulls back and drops
by half the pad's band, so no level is ever played under a thumb.

## Mechanics

**Mirrors are one-sided.** A mirror is a 45° plate with a bright face and a dull back. It has
four orientations and each one reflects exactly two of the four incoming directions; a beam
that hits the back is swallowed. That is what makes a four-step rotation a real decision
rather than a two-state toggle.

**Everything answers to light, not to touch**, and every device belongs to one of two
families. *Held* devices read the light continuously and answer the instant it moves on.
*Latched* devices need lighting once and then stay that way, freeing the beam to go
somewhere else. Most puzzles are built on the tension between the two.

| Device | Family | Behaviour |
| --- | --- | --- |
| Plate → bridge | held | the bridge exists only while the plate is lit; step off-cycle and you fall |
| Plate → held door | held | open while the plate is lit, and it slams shut the moment the light moves on |
| Plate → nightbloom | held, inverted | a path that exists only while the plate is **dark** — light closes it |
| Receiver → door | latched | light it once and the door stays open, for the beam and for you |
| Switch → gate | latched | one flash is enough, and the gate never closes |
| Lantern | held | catches the beam and burns, casting light in all four directions; you can stand on it while it burns, and it goes out the instant the beam leaves |
| Focus lens → door | held | charged only while **two** beams cross it at once; lose one and the door shuts |
| Prism | — | splits a beam into both perpendiculars |
| Chasm | — | light crosses it; you do not |

A lantern is the one device that *makes* light rather than consuming it, so it turns a
single beam into a cross and reaches places no mirror chain can. It is still held: stop
feeding it and the room goes dark again — including under your own feet, because a burning
lamp is a stone you can stand on. That is what lets a corridor of light turn a corner
inside a gloom, where the beam is the only floor.

### The darkness

| Enemy | Behaviour |
| --- | --- |
| Cloud + static eye | the cloud is deadly everywhere until its eye is struck; one beam is enough |
| Roaming eye | wanders its whole cloud, so you flood the cloud and let it walk into the light |
| Warded eye | a shell one beam cannot break — it needs **two beams arriving from different directions at the same moment**, which means the prism |
| Gloom | not a blob but a whole room, and it is never cleared by walking around it: only the cells the beam is *currently* touching will hold you, so the light is a walkway you have to keep alive |

A gloom with an eye in it lifts for good once that eye is out. A gloom with **no** eye is
simply permanent dark — legitimate, because the beam is always a way through. A *cloud*
with no eye would wall a level off forever, so the tests reject it.

Crossing a gloom has one geometric catch worth knowing when you build levels. Where a
corridor of light turns, the corner is the mirror itself, and the lit cells either side of
it are diagonal to each other — so the turn is not walkable. Two things fix it: a plain
floor landing tucked into the corner, or a lantern, which *is* the corner and is walkable
while it burns.

Wards and lenses share one idea deliberately: two beams at once. It shows up first as a
lock (level 13) and then as an enemy (level 14), so combat and puzzle stay the same verb.

### Standing where the floor might leave

Bridges fall, nightblooms close and gloom goes dark underfoot. Any of those while you are
standing there costs you the walk back from the start — never lost progress. The solver
models this too, so a "solution" that relies on standing somewhere it is about to drop
is not counted as one.

**The beam settles instantly.** Light opens a door, the opened door passes more light, a lit
lantern throws light of its own, a struck eye stops absorbing, a second beam breaks a ward —
so the trace repeats until nothing changes. Every pass only adds light, so it always converges. Puzzles are about
geometry and sequencing, never timing.

One consequence worth knowing: a held door that could only ever be opened by light arriving
*through itself* stays shut. The settle starts with everything closed and only opens things,
so a self-supporting loop never bootstraps. That is deterministic and it is the honest answer,
but it means such a layout is a dead end rather than a clever trick.

**Winning a room** means standing on the exit *while it is lit*, with every eye put out.

## Levels

Thirty-two levels in six phases — tutorial (1–4), combine (5–12), walk the light (13–16),
escalate (17–24), converge (25–29), finale (30–32).

The finale is not three rooms but **one tower climbed in three goes**. Each of those levels
begins in the state the one before it ended in: the same mirrors at the angles you left
them, the eye you put out still out, and you standing on the exit you just reached — with
another floor of the shaft opened above you. Nothing resets, so the last stretch reads as a
single ascent rather than three puzzles.

That promise lives in the seam between maps and would break silently if one were edited, so
`FinaleChainTests` holds it: each room must start on the previous room's exit, carried-over
mirrors must keep their positions, the tower may only ever open up and never wall a cell
back in, and the cleared eye must not come back.

Pacing note for a jam: the whole set is 10–15 minutes, and a judge will usually give a
game 5–10. Most levels are one or two mirror turns and under half a minute; the long ones
are deliberately late (25, 27, 29). `[` and `]` jump between levels, which is the fastest
way to show someone a specific mechanic.

*Walk the light* is a four-room act on one idea: the darkness is not an obstacle to clear
but a floor to lay. The beam stops being a switch and becomes a road — one you lead, one
that turns at lamps you stand on, one that holds a door open from inside itself.
Each one introduces a single new idea and recombines it with what came before.

Levels are ASCII maps in [`LevelLibrary.cs`](Assets/Scripts/FirstLight/LevelLibrary.cs); the
legend is in the comment above them. Editing a map is the whole workflow — the geometry, the
visuals and the collision all come from that one string.

## Layout

```
Assets/Scripts/FirstLight/
  LightSim.cs       tiles, one-sided mirror maths, level parsing, the beam solver
  LevelLibrary.cs   the thirty-two maps
  LevelView.cs      builds a level's visuals and keeps them in sync with the light
  GameManager.cs    camera, player, level flow, HUD
  Art.cs            every sprite, generated at runtime
  Palette.cs        the two-tone colour language
  Sfx.cs            procedural sound cues
Assets/Tests/
  EditMode/         exhaustive solvability, structure and held/latched rule checks
  PlayMode/         boots the real game and runs every level; screenshot capture
```

## Tests

```bash
Unity.exe -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml
```

`EveryLevelIsSolvable` searches the complete reachable state space of each level — player
position × mirror orientations × cleared eyes × switch state × charged receivers — and fails if
any map cannot be finished. It also models the ground going out from under you, so a solution
that depends on standing on a bridge you are about to drop is not counted. Edit a map into an
unwinnable shape and the tests say so.

`HeldElementTests` pins the held-versus-latched rules directly: a plate drops when the light
moves on while a receiver stays charged, a held door shuts while a latched one does not, a
nightbloom is walkable only in the dark, and a lantern lights cells no beam could reach and
goes out when the beam is turned away.

`DarknessTests` pins the harder darkness on a fixture that can deliver either one beam or
two on demand — plus the rule that a lamp holds you only while it burns: a gloom cell is walkable only where the beam falls, an eyeless gloom stays
deadly for good while a struck eye makes the whole region safe, a ward survives one beam and
breaks under two where a plain eye dies to one, and a focus lens holds its door only while
both beams are crossing.

`RuntimeSmokeTests` boots the actual game object and runs every level for a few frames, so the
whole build-sprites-and-render path is exercised too.

`TouchControlsTests` and `MobileCameraTests` cover the on-screen pad. Layout and
hit-testing are pure functions of the screen size, so they are checked at several phone
and tablet resolutions with no device attached: buttons stay on screen, never overlap,
stay big enough to hit, a held direction keeps reading while a turn fires once per press,
and two thumbs register at once. The pair that matters most assert the negative — that on
desktop the pad is inert even if something hands it a pointer, and that the desktop camera
framing still matches the original rule exactly.

To regenerate screenshots, set `FL_SHOT_DIR` and run the PlayMode test filter `CaptureShots`.

## Decisions taken from the document's open questions

The design document left five questions open. These are the answers the build assumes; all of
them are cheap to change.

1. **Rotate in place only.** Mirrors turn where they stand; nothing is carried. Rotation alone
   carries all thirty-two levels, and carryable mirrors were listed as the first thing to cut.
2. **The clouds are hazardous.** Walking into living darkness sends you back to where the
   level started — a cost in time, with no health bar or fail state to build around.
3. **Failure is local.** A death moves you, not the room: mirrors, charged receivers and
   cleared eyes keep their state, so you never lose progress you earned. The same applies when
   the ground goes out from under you — a bridge dropping or a nightbloom closing costs you the
   walk back, nothing more.
4. **One continuous thread.** Levels chain directly, the world's ambient light rises a little
   with each one, and the finale hands the beam back to the sky.
5. **The entity is unhindered.** Normal speed everywhere, no slow or snare — the darkness
   stops you at its edge rather than fighting you inside it.

One further deviation worth naming: movement is on the grid, one cell per step, smoothed
visually. The document describes free top-down movement, but grid steps make "which cell is
lit" and "can I stand here" the same question the puzzle asks, and they let the solver above
verify exactly what the player experiences.
