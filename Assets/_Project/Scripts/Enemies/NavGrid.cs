using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace N89.Enemies
{
    // Поиск пути для врагов: сетка проходимости поверх стен (A*, 8 направлений).
    // Сетка строится лениво при первом запросе — по одной на (слои-препятствия, радиус тела).
    // Если путь не нашёлся (например, открылась дверь), сетка пересобирается, но не чаще раза в секунду.
    public static class NavGrid
    {
        private const float CellSize = 0.25f;
        private const float RebuildInterval = 1f;
        private const float Padding = 2f;
        private const int MaxCells = 250000;

        private class Grid
        {
            public Vector2 Origin;
            public int Width;
            public int Height;
            public bool[] Walkable;
            public float BuiltAt;
        }

        private static readonly Dictionary<(int, float), Grid> s_grids = new Dictionary<(int, float), Grid>();

        // Буферы A*, общие для всех запросов
        private static float[] s_g = new float[0];
        private static int[] s_parent = new int[0];
        private static int[] s_stamp = new int[0];
        private static bool[] s_closed = new bool[0];
        private static int s_search;
        private static readonly List<(float f, int index)> s_open = new List<(float, int)>();
        private static readonly List<int> s_cells = new List<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_grids.Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => s_grids.Clear();

        // Путь от from до to: углы ломаной (без стартовой точки), последняя точка — сама цель.
        // false — пути нет (path пуст).
        public static bool FindPath(Vector2 from, Vector2 to, float radius, LayerMask blockers, List<Vector2> path)
        {
            path.Clear();
            Grid grid = GetGrid(radius, blockers, false);
            if (grid == null)
            {
                return false;
            }

            int start = NearestWalkable(grid, from);
            int goal = NearestWalkable(grid, to);
            if (start >= 0 && start == goal)
            {
                path.Add(to);
                return true;
            }

            if (start < 0 || goal < 0 || !Search(grid, start, goal))
            {
                // Возможно, сетка устарела (открылась дверь) — пересобираем и пробуем ещё раз
                Grid fresh = GetGrid(radius, blockers, true);
                if (fresh == grid || fresh == null)
                {
                    return false;
                }

                grid = fresh;
                start = NearestWalkable(grid, from);
                goal = NearestWalkable(grid, to);
                if (start < 0 || goal < 0 || start == goal || !Search(grid, start, goal))
                {
                    return false;
                }
            }

            // Восстанавливаем цепочку клеток и оставляем только повороты
            s_cells.Clear();
            for (int c = goal; c != start; c = s_parent[c]) s_cells.Add(c);
            s_cells.Reverse();

            for (int i = 0; i < s_cells.Count - 1; i++)
            {
                int prev = i == 0 ? start : s_cells[i - 1];
                int cur = s_cells[i];
                int next = s_cells[i + 1];
                if (cur - prev != next - cur)
                {
                    path.Add(CellCenter(grid, cur));
                }
            }

            path.Add(to);
            return true;
        }

        private static bool Search(Grid grid, int start, int goal)
        {
            int count = grid.Width * grid.Height;
            if (s_g.Length < count)
            {
                s_g = new float[count];
                s_parent = new int[count];
                s_stamp = new int[count];
                s_closed = new bool[count];
            }

            s_search++;
            s_open.Clear();
            Visit(start, 0f, start);
            Push(Heuristic(grid, start, goal), start);

            int gx = goal % grid.Width;
            int gy = goal / grid.Width;
            while (s_open.Count > 0)
            {
                int current = Pop();
                if (s_closed[current])
                {
                    continue;
                }

                s_closed[current] = true;
                if (current == goal)
                {
                    return true;
                }

                int cx = current % grid.Width;
                int cy = current / grid.Width;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (!IsWalkable(grid, nx, ny)) continue;
                        // По диагонали — только если не срезает угол стены
                        if (dx != 0 && dy != 0 && (!IsWalkable(grid, cx + dx, cy) || !IsWalkable(grid, cx, cy + dy))) continue;

                        int n = ny * grid.Width + nx;
                        float g = s_g[current] + (dx != 0 && dy != 0 ? 1.4142f : 1f);
                        if (s_stamp[n] == s_search && (s_closed[n] || g >= s_g[n])) continue;

                        Visit(n, g, current);
                        float h = Mathf.Sqrt((nx - gx) * (nx - gx) + (ny - gy) * (ny - gy));
                        Push(g + h, n);
                    }
                }
            }

            return false;
        }

        private static void Visit(int cell, float g, int parent)
        {
            s_stamp[cell] = s_search;
            s_g[cell] = g;
            s_parent[cell] = parent;
            s_closed[cell] = false;
        }

        private static float Heuristic(Grid grid, int a, int b)
        {
            int dx = a % grid.Width - b % grid.Width;
            int dy = a / grid.Width - b / grid.Width;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // ---------------------------------------------------------------- Двоичная куча

        private static void Push(float f, int index)
        {
            s_open.Add((f, index));
            int i = s_open.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (s_open[parent].f <= s_open[i].f) break;
                (s_open[parent], s_open[i]) = (s_open[i], s_open[parent]);
                i = parent;
            }
        }

        private static int Pop()
        {
            int result = s_open[0].index;
            int last = s_open.Count - 1;
            s_open[0] = s_open[last];
            s_open.RemoveAt(last);

            int i = 0;
            while (true)
            {
                int left = i * 2 + 1;
                int right = left + 1;
                int smallest = i;
                if (left < s_open.Count && s_open[left].f < s_open[smallest].f) smallest = left;
                if (right < s_open.Count && s_open[right].f < s_open[smallest].f) smallest = right;
                if (smallest == i) break;
                (s_open[smallest], s_open[i]) = (s_open[i], s_open[smallest]);
                i = smallest;
            }

            return result;
        }

        // ---------------------------------------------------------------- Сетка

        // rebuild — пересобрать, если сетка старше RebuildInterval
        private static Grid GetGrid(float radius, LayerMask blockers, bool rebuild)
        {
            var key = (blockers.value, radius);
            if (s_grids.TryGetValue(key, out Grid grid) && (!rebuild || Time.time - grid.BuiltAt < RebuildInterval))
            {
                return grid;
            }

            grid = Build(radius, blockers);
            s_grids[key] = grid;
            return grid;
        }

        private static Grid Build(float radius, LayerMask blockers)
        {
            // Границы — по всем коллайдерам-препятствиям на сцене
            bool any = false;
            Bounds bounds = default;
            foreach (Collider2D col in Object.FindObjectsByType<Collider2D>())
            {
                if ((blockers.value & (1 << col.gameObject.layer)) == 0) continue;
                if (!any)
                {
                    bounds = col.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(col.bounds);
                }
            }

            if (!any)
            {
                return null;
            }

            var grid = new Grid
            {
                Origin = (Vector2)bounds.min - Vector2.one * Padding,
                Width = Mathf.CeilToInt((bounds.size.x + Padding * 2f) / CellSize),
                Height = Mathf.CeilToInt((bounds.size.y + Padding * 2f) / CellSize),
                BuiltAt = Time.time,
            };

            if (grid.Width * grid.Height > MaxCells)
            {
                Debug.LogWarning($"[NavGrid] Уровень слишком большой для сетки: {grid.Width}x{grid.Height}");
                return null;
            }

            grid.Walkable = new bool[grid.Width * grid.Height];
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    int i = y * grid.Width + x;
                    grid.Walkable[i] = !Physics2D.OverlapCircle(CellCenter(grid, i), radius, blockers);
                }
            }

            return grid;
        }

        private static bool IsWalkable(Grid grid, int x, int y)
        {
            return x >= 0 && y >= 0 && x < grid.Width && y < grid.Height && grid.Walkable[y * grid.Width + x];
        }

        private static Vector2 CellCenter(Grid grid, int index)
        {
            return grid.Origin + new Vector2(index % grid.Width + 0.5f, index / grid.Width + 0.5f) * CellSize;
        }

        // Клетка под точкой, а если она в стене (враг/игрок прижат к стене) — ближайшая свободная рядом
        private static int NearestWalkable(Grid grid, Vector2 point)
        {
            int px = Mathf.FloorToInt((point.x - grid.Origin.x) / CellSize);
            int py = Mathf.FloorToInt((point.y - grid.Origin.y) / CellSize);
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int r = 0; r <= 4 && best < 0; r++)
            {
                for (int y = py - r; y <= py + r; y++)
                {
                    for (int x = px - r; x <= px + r; x++)
                    {
                        if (!IsWalkable(grid, x, y)) continue;
                        int i = y * grid.Width + x;
                        float d = (CellCenter(grid, i) - point).sqrMagnitude;
                        if (d < bestDistance)
                        {
                            bestDistance = d;
                            best = i;
                        }
                    }
                }
            }

            return best;
        }
    }
}
