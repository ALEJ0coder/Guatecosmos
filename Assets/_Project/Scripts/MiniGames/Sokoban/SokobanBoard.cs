using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarOutpost.MiniGames.Sokoban
{
    /// <summary>
    /// Instantánea del tablero, usada por el sistema de deshacer.
    /// </summary>
    public struct SokobanSnapshot
    {
        public Vector2Int Player;
        public List<Vector2Int> BatteryPositions;
        public int MoveCount;
    }

    /// <summary>
    /// Contiene únicamente las reglas de Sokoban (movimiento, empuje, condición de victoria).
    /// No depende de Canvas, Input ni de ResourceManager: así se puede probar con
    /// Unity Test Framework sin levantar una escena.
    /// </summary>
    public sealed class SokobanBoard
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Vector2Int PlayerPosition { get; private set; }
        public int MoveCount { get; private set; }

        private readonly HashSet<Vector2Int> walls = new HashSet<Vector2Int>();
        private readonly List<SokobanBattery> batteries = new List<SokobanBattery>();
        private readonly List<SokobanGoal> goals = new List<SokobanGoal>();

        public IReadOnlyList<SokobanBattery> Batteries => batteries;
        public IReadOnlyList<SokobanGoal> Goals => goals;

        /// <summary>Carga un nivel y deja el tablero listo para jugar desde cero.</summary>
        public void LoadLevel(SokobanLevelData level)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));

            Width = level.Width;
            Height = level.Height;

            walls.Clear();
            foreach (Vector2Int wall in level.Walls)
                walls.Add(wall);

            batteries.Clear();
            foreach (Vector2Int position in level.BatteryStarts)
                batteries.Add(new SokobanBattery(position));

            goals.Clear();
            foreach (Vector2Int position in level.StationPositions)
                goals.Add(new SokobanGoal(position));

            PlayerPosition = level.PlayerStart;
            MoveCount = 0;
            RefreshConnections();
        }

        public bool IsInsideBoard(Vector2Int position)
        {
            return position.x >= 0 && position.x < Width &&
                   position.y >= 0 && position.y < Height;
        }

        public bool IsWall(Vector2Int position) => walls.Contains(position);

        public bool IsGoal(Vector2Int position) => goals.Any(goal => goal.Position == position);

        public int BatteryIndexAt(Vector2Int position)
        {
            for (int i = 0; i < batteries.Count; i++)
            {
                if (batteries[i].Position == position)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Intenta mover al astronauta un paso en una dirección, empujando una batería si corresponde.
        /// Devuelve true si el tablero cambió (para que el controlador cuente el movimiento
        /// y guarde una instantánea de deshacer).
        /// </summary>
        public bool TryMove(Vector2Int direction)
        {
            Vector2Int nextPosition = PlayerPosition + direction;
            if (!IsInsideBoard(nextPosition) || IsWall(nextPosition))
                return false;

            int batteryIndex = BatteryIndexAt(nextPosition);
            if (batteryIndex >= 0)
            {
                // Regla: no se puede empujar una batería contra una pared, otra batería
                // o fuera del tablero. Tampoco se pueden empujar dos a la vez porque solo
                // se mueve la que está inmediatamente frente al astronauta.
                Vector2Int batteryDestination = nextPosition + direction;
                bool blocked = !IsInsideBoard(batteryDestination)
                    || IsWall(batteryDestination)
                    || BatteryIndexAt(batteryDestination) >= 0;

                if (blocked)
                    return false;

                batteries[batteryIndex].Position = batteryDestination;
            }

            PlayerPosition = nextPosition;
            MoveCount++;
            RefreshConnections();
            return true;
        }

        public bool IsSolved()
        {
            return batteries.Count == goals.Count && batteries.All(battery => battery.IsConnected);
        }

        public SokobanSnapshot CreateSnapshot()
        {
            return new SokobanSnapshot
            {
                Player = PlayerPosition,
                BatteryPositions = batteries.Select(b => b.Position).ToList(),
                MoveCount = MoveCount,
            };
        }

        public void RestoreSnapshot(SokobanSnapshot snapshot)
        {
            PlayerPosition = snapshot.Player;
            for (int i = 0; i < batteries.Count && i < snapshot.BatteryPositions.Count; i++)
                batteries[i].Position = snapshot.BatteryPositions[i];
            MoveCount = snapshot.MoveCount;
            RefreshConnections();
        }

        private void RefreshConnections()
        {
            foreach (SokobanGoal goal in goals)
                goal.IsOccupied = BatteryIndexAt(goal.Position) >= 0;

            foreach (SokobanBattery battery in batteries)
                battery.IsConnected = IsGoal(battery.Position);
        }
    }
}
