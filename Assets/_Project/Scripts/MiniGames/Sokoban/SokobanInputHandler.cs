using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LunarOutpost.MiniGames.Sokoban
{
    /// <summary>
    /// Traduce el teclado (y los botones táctiles, mediante RequestDirection) en direcciones
    /// de movimiento. Compila contra el Input System nuevo cuando está habilitado en el
    /// proyecto (ENABLE_INPUT_SYSTEM) y usa el Input Manager clásico como alternativa cuando
    /// no lo está (ENABLE_LEGACY_INPUT_MANAGER). Unity define estos símbolos según la
    /// configuración de "Active Input Handling" en Project Settings, así que nunca se compilan
    /// los dos caminos de forma contradictoria ni hay doble lectura del mismo evento.
    /// </summary>
    public sealed class SokobanInputHandler : MonoBehaviour
    {
        /// <summary>Se dispara una sola vez por pulsación, nunca mientras la tecla se mantiene presionada.</summary>
        public event Action<Vector2Int> OnDirectionRequested;

        public bool IsEnabled { get; set; } = true;

        private void Update()
        {
            if (!IsEnabled)
                return;

#if ENABLE_INPUT_SYSTEM
            ReadFromInputSystem();
#elif ENABLE_LEGACY_INPUT_MANAGER
            ReadFromLegacyInput();
#endif
        }

        /// <summary>Permite que los botones en pantalla (táctiles) disparen el mismo camino que el teclado.</summary>
        public void RequestDirection(Vector2Int direction)
        {
            if (IsEnabled)
                OnDirectionRequested?.Invoke(direction);
        }

#if ENABLE_INPUT_SYSTEM
        private void ReadFromInputSystem()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
                RequestDirection(Vector2Int.up);
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
                RequestDirection(Vector2Int.down);
            else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
                RequestDirection(Vector2Int.left);
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
                RequestDirection(Vector2Int.right);
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        private void ReadFromLegacyInput()
        {
            // GetKeyDown ya dispara una sola vez por pulsación, igual que wasPressedThisFrame.
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                RequestDirection(Vector2Int.up);
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                RequestDirection(Vector2Int.down);
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                RequestDirection(Vector2Int.left);
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                RequestDirection(Vector2Int.right);
        }
#endif
    }
}
