using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarOutpost.MiniGames.Sokoban
{
    /// <summary>
    /// Valida la estructura de un SokobanLevelData y comprueba, mediante búsqueda,
    /// que exista al menos una secuencia de movimientos que lo resuelva.
    /// No es parte de la lista de archivos sugerida en el encargo original, pero la sección 4
    /// exige esta validación explícitamente, así que se añade como archivo independiente
    /// en vez de sobrecargar SokobanLevelData o SokobanBoard.
    /// </summary>
    public static class SokobanLevelValidator
    {
        private const int MaxExploredStates = 20000;

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        };

        /// <summary>Revisa la estructura del nivel. Devuelve la lista de errores encontrados (vacía si es válido).</summary>
        public static List<string> ValidateStructure(SokobanLevelData level)
        {
            List<string> errors = new List<string>();

            if (level == null)
            {
                errors.Add("El nivel es nulo.");
                return errors;
            }

            if (level.BatteryStarts == null || level.BatteryStarts.Count != SokobanLevelData.RequiredBatteries)
                errors.Add($"Se requieren exactamente {SokobanLevelData.RequiredBatteries} baterías.");

            if (level.StationPositions == null || level.StationPositions.Count != SokobanLevelData.RequiredBatteries)
                errors.Add($"Se requieren exactamente {SokobanLevelData.RequiredBatteries} estaciones.");

            bool PositionIsValid(Vector2Int position)
            {
                bool insideBounds = position.x >= 0 && position.x < level.Width &&
                                     position.y >= 0 && position.y < level.Height;
                return insideBounds && !level.Walls.Contains(position);
            }

            if (!PositionIsValid(level.PlayerStart))
                errors.Add("La posición inicial del astronauta no es válida (fuera del tablero o sobre un muro).");

            if (level.BatteryStarts != null)
            {
                HashSet<Vector2Int> seen = new HashSet<Vector2Int>();
                foreach (Vector2Int battery in level.BatteryStarts)
                {
                    if (!PositionIsValid(battery))
                        errors.Add($"Hay una batería en una posición inválida: {battery}.");
                    if (!seen.Add(battery))
                        errors.Add($"Hay dos baterías en la misma posición: {battery}.");
                    if (battery == level.PlayerStart)
                        errors.Add($"Una batería está en la misma casilla que el astronauta: {battery}.");
                }
            }

            if (level.StationPositions != null)
            {
                HashSet<Vector2Int> seen = new HashSet<Vector2Int>();
                foreach (Vector2Int station in level.StationPositions)
                {
                    if (!PositionIsValid(station))
                        errors.Add($"Hay una estación en una posición inválida: {station}.");
                    if (!seen.Add(station))
                        errors.Add($"Hay dos estaciones en la misma posición: {station}.");
                }
            }

            return errors;
        }

        /// <summary>
        /// Búsqueda en anchura sobre el espacio de estados (posición del astronauta + posiciones
        /// de las baterías) para comprobar que el nivel tiene solución, sin depender de una
        /// secuencia de movimientos escrita a mano. Pensado para tableros pequeños (como 8x8 con
        /// 2 baterías); se detiene tras MaxExploredStates para no colgar el Editor con niveles
        /// mucho más grandes.
        /// </summary>
        public static bool IsSolvable(SokobanLevelData level)
        {
            SokobanBoard board = new SokobanBoard();
            board.LoadLevel(level);

            string startKey = StateKey(board.PlayerPosition, board.Batteries.Select(b => b.Position));
            Queue<SokobanBoard> frontier = new Queue<SokobanBoard>();
            HashSet<string> visited = new HashSet<string> { startKey };
            frontier.Enqueue(board);

            int exploredStates = 0;
            while (frontier.Count > 0 && exploredStates < MaxExploredStates)
            {
                SokobanBoard current = frontier.Dequeue();
                exploredStates++;

                if (current.IsSolved())
                    return true;

                foreach (Vector2Int direction in Directions)
                {
                    SokobanBoard next = Clone(current);
                    if (!next.TryMove(direction))
                        continue;

                    string key = StateKey(next.PlayerPosition, next.Batteries.Select(b => b.Position));
                    if (visited.Add(key))
                        frontier.Enqueue(next);
                }
            }

            return false;
        }

        private static SokobanBoard Clone(SokobanBoard source)
        {
            SokobanBoard clone = new SokobanBoard();
            SokobanLevelData snapshotAsLevel = new SokobanLevelData(
                source.Width,
                source.Height,
                source.PlayerPosition,
                source.Batteries.Select(b => b.Position).ToList(),
                source.Goals.Select(g => g.Position).ToList(),
                EnumerateWalls(source));
            clone.LoadLevel(snapshotAsLevel);
            return clone;
        }

        private static List<Vector2Int> EnumerateWalls(SokobanBoard source)
        {
            List<Vector2Int> walls = new List<Vector2Int>();
            for (int x = 0; x < source.Width; x++)
            {
                for (int y = 0; y < source.Height; y++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (source.IsWall(position))
                        walls.Add(position);
                }
            }
            return walls;
        }

        private static string StateKey(Vector2Int player, IEnumerable<Vector2Int> batteryPositions)
        {
            IEnumerable<string> ordered = batteryPositions
                .Select(p => p.x + "," + p.y)
                .OrderBy(s => s);
            return player.x + ";" + player.y + "|" + string.Join(";", ordered);
        }
    }
}
