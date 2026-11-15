using UnityEngine;

namespace LunarOutpost.MiniGames.Sokoban
{
    /// <summary>
    /// Representa una estación eléctrica (la "ranura" donde debe llegar una batería).
    /// Al igual que SokobanBattery, es una clase de datos: el tablero la dibuja por código.
    /// </summary>
    public sealed class SokobanGoal
    {
        public Vector2Int Position { get; }

        /// <summary>Verdadero mientras haya una batería colocada encima de esta estación.</summary>
        public bool IsOccupied { get; set; }

        public SokobanGoal(Vector2Int position)
        {
            Position = position;
            IsOccupied = false;
        }
    }
}
