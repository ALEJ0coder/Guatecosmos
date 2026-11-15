using UnityEngine;

namespace LunarOutpost.Core
{
    /// <summary>
    /// Coordinador básico de la partida. Escucha los eventos de los minijuegos.
    /// Más adelante puede controlar menús, cambio de escenas y fin de partida.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public int CompletedMiniGames { get; private set; }
        public string LastMiniGame { get; private set; } = string.Empty;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameEvents.OnMiniGameFinished += HandleMiniGameFinished;
        }

        private void OnDisable()
        {
            GameEvents.OnMiniGameFinished -= HandleMiniGameFinished;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void HandleMiniGameFinished(string miniGameId, bool success)
        {
            LastMiniGame = miniGameId;

            if (success)
            {
                CompletedMiniGames++;
                Debug.Log("Minijuego completado: " + miniGameId + ". Total: " + CompletedMiniGames);
            }
            else
            {
                Debug.Log("El minijuego terminó sin completarse: " + miniGameId);
            }
        }
    }
}
