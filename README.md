# Adonis Life

A procedurally generated open-world life-simulation game built with Unity 6 (6000.4.x) and the
Universal Render Pipeline. Every world system is deterministic, data-driven, and validated by
EditMode tests.

## Project layout

```
Adonis Life/
  Assets/
    Editor/                    Editor generators, pipeline, validation suite, dev tools
    Project/
      Gameplay/                Player, camera, interaction, inventory, quests, save system
      World/
        Buildings/             Zoned building planner (12 building types)
        Common/                Shared deterministic hashing
        Configs/               Authored ScriptableObject settings (world, city, terrain)
        Data/                  Authored + runtime data models
        Environment/           Day/night cycle and weather hooks
        Materials/             Generated URP materials (GPU instancing enabled)
        Npc/                   Sidewalk navigation, crowd zones, pedestrian/vehicle agents
        ProceduralCity/        City, road-detail, infrastructure, environment layout math
        Scenes/                Generated prototype scenes
        Streaming/             Chunk streaming (loader/unloader, hysteresis, LOD rings)
        Terrain/               Deterministic height field + Burst-parallel heightmaps
        Tests/Editor/          EditMode test suites (122 tests)
        Tools/                 City statistics, generation profiler, performance overlay
        Traffic/               Road/lane graphs, pathfinding, traffic lights, spawn planning
        UrbanCell/             Approved 250x250 m urban base cell geometry model
```

## Architecture

- **Pure layout models** (`ProceduralCityLayout`, `RoadDetailLayout`, `BuildingBlockPlanner`,
  `TerrainHeightField`, ...) compute all geometry from settings structs with no scene or asset
  dependencies, so they are unit-testable and deterministic per seed.
- **Editor generators** (`Assets/Editor/Adonis*Setup.cs`) turn those models into scenes and
  assets. Every generator is idempotent: it skips itself when its output exists.
- **Runtime systems** (streaming, traffic lights, NPC agents, day/night, weather, gameplay)
  are thin MonoBehaviours over pure, tested cores.

## Editor menus

- `Adonis Life/World/Generate Full World Pipeline` — run every generation step in order.
- `Adonis Life/World/...` — individual generation steps (city, terrain, details, buildings,
  infrastructure, environment, traffic, NPC, gameplay, optimization pass).
- `Adonis Life/Tools/...` — world debugger, streaming debugger, chunk viewer, city statistics,
  generation profiler, validation suite, performance overlay.

## Testing and validation

Run all EditMode tests headlessly:

```
Unity -batchmode -projectPath "Adonis Life" -runTests -testPlatform EditMode ^
      -testResults results.xml -logFile test.log
```

Run the project validation suite (asset validity, scene inventory, hierarchy, instancing):

```
Unity -batchmode -quit -projectPath "Adonis Life" ^
      -executeMethod AdonisValidationSuite.RunAll -logFile validate.log
```

CI (`.github/workflows/ci.yml`) runs the EditMode tests on every push and pull request to
`main`; it requires `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD` repository secrets.

## Conventions

- Never commit `ProjectSettings/ProjectSettings.asset` scripting-define reordering noise.
- Generated scenes are reproducible: regenerate them via the pipeline rather than hand-editing.
- Every new system ships with a pure model and EditMode tests.
