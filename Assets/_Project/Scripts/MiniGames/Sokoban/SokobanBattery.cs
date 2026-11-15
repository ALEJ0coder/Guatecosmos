using UnityEngine;

namespace LunarOutpost.MiniGames.Sokoban
{
    /// <summary>
    /// Representa una batería empujable dentro del tablero.
    /// Es una clase de datos simple (no un MonoBehaviour) porque el tablero se dibuja
    /// por código sobre una grilla de UI; no se usan prefabs ni sprites externos.
    /// </summary>
    public sealed class SokobanBattery
    {
        public Vector2Int Position { get; set; }

        /// <summary>Verdadero cuando la batería está sobre una estación eléctrica.</summary>
        public bool IsConnected { get; set; }

        public SokobanBattery(Vector2Int startPosition)
        {
            Position = startPosition;
            IsConnected = false;
        }
    }
}
