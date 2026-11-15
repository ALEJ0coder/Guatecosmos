using System.Collections.Generic;
using UnityEngine;

namespace LunarOutpost.MiniGames.Sokoban
{
    /// <summary>
    /// Describe un nivel de Sokoban mediante datos puros (sin dependencias de escena).
    /// Reemplaza las posiciones sueltas que antes estaban escritas a mano dentro de SokobanMiniGame.
    /// </summary>
    public sealed class SokobanLevelData
    {
        /// <summary>Número de baterías (y de estaciones) que exige el diseño actual.</summary>
        public const int RequiredBatteries = 2;

        public int Width { get; }
        public int Height { get; }
        public Vector2Int PlayerStart { get; }
        public IReadOnlyList<Vector2Int> BatteryStarts { get; }
        public IReadOnlyList<Vector2Int> StationPositions { get; }
        public IReadOnlyCollection<Vector2Int> Walls { get; }

        public SokobanLevelData(
            int width,
            int height,
            Vector2Int playerStart,
            IReadOnlyList<Vector2Int> batteryStarts,
            IReadOnlyList<Vector2Int> stationPositions,
            IReadOnlyCollection<Vector2Int> walls)
        {
            Width = width;
            Height = height;
            PlayerStart = playerStart;
            BatteryStarts = batteryStarts;
            StationPositions = stationPositions;
            Walls = walls;
        }

        /// <summary>
        /// Nivel inicial 8x8 de "Battery Grid". Mantiene exactamente las mismas posiciones
        /// que ya existían en SokobanMiniGame.cs para no invalidar Sokoban_Science.md.
        /// La solución de referencia (12 movimientos) está verificada en
        /// SokobanLevelValidatorTests y corresponde al comentario original:
        /// ↑↑↑↑→↓↓↑↑→↓↓
        /// </summary>
        public static SokobanLevelData CreateDefaultLevel()
        {
            const int width = 8;
            const int height = 8;
            HashSet<Vector2Int> walls = new HashSet<Vector2Int>();

            for (int x = 0; x < width; x++)
            {
                walls.Add(new Vector2Int(x, 0));
                walls.Add(new Vector2Int(x, height - 1));
            }
            for (int y = 0; y < height; y++)
            {
                walls.Add(new Vector2Int(0, y));
                walls.Add(new Vector2Int(width - 1, y));
            }
            // Obstáculo interior: crea un reto de navegación sin bloquear la solución conocida.
            walls.Add(new Vector2Int(2, 6));

            Vector2Int playerStart = new Vector2Int(1, 1);
            List<Vector2Int> batteries = new List<Vector2Int>
            {
                new Vector2Int(2, 4),
                new Vector2Int(3, 4),
            };
            List<Vector2Int> stations = new List<Vector2Int>
            {
                new Vector2Int(2, 2),
                new Vector2Int(3, 2),
            };

            return new SokobanLevelData(width, height, playerStart, batteries, stations, walls);
        }

        /// <summary>
        /// Secuencia de movimientos que resuelve CreateDefaultLevel(). Se usa en las pruebas
        /// automatizadas y como respaldo documental de que el nivel es resoluble.
        /// </summary>
        public static IReadOnlyList<Vector2Int> GetReferenceSolution()
        {
            return new List<Vector2Int>
            {
                Vector2Int.up, Vector2Int.up, Vector2Int.up, Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down, Vector2Int.down,
                Vector2Int.up, Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down, Vector2Int.down,
            };
        }
    }
}
