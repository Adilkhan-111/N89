# Туториал (`Assets/_Project/Scenes/Tutorial.unity`)

- Сборка: **N89 > Build Tutorial Scene** (`Scripts/Editor/TutorialSceneBuilder.cs`). Пересоздаёт сцену, префабы в `Prefabs/` и временные спрайты в `Art/Sprites/Generated/` (рисуются кодом в `SpriteFactory.cs`; настоящий арт — заменить PNG с теми же именами). Данные взломов (`ScriptableObjects/Quickhacks/`) создаются один раз и дальше тюнятся в инспекторе.
- Реплики голоса и шаги — в `Scripts/Tutorial/TutorialDirector.cs`. Две части: калибровка в шестиугольной лаборатории → побег (89 взламывает шлюз, бежит через охрану корпорации Е, у выхода его убивают в спину, камера улетает вверх, итоговый экран с рейтингом «не оценено» и кнопкой «Вперёд» → `Interlude01`).
- Смерть во время побега перезагружает сцену с чекпоинта (`Scripts/Core/Checkpoint.cs`, статический, сбрасывается из главного меню). R после смерти перезапускает побег с чекпоинта.
- У пистолета 89 магазин на 8; перезарядку объясняет `ShootingStep`, досрочную по R — `ReloadStep`.
- Общие для туториала и миссий части сборки — в `TutorialSceneBuilder`: `CreateCommonPrefabs` (взломы, эффекты, пули), `PlayerPrefab`, `EnemyPrefab`, `SpawnEnemy`/`SpawnPerimeter`, `BuildHud` (полный боевой HUD с итоговым экраном), `BurstPrefab`, `AddLegs`, `BuildCrosshair`.
