using System.Linq;
using LunarOutpost.Core;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace LunarOutpost.MiniGames.Sokoban
{
    /// <summary>
    /// Punto de entrada de "Battery Grid". Crea (o reutiliza) los servicios de Core,
    /// construye la interfaz y conecta entre sí al resto de los componentes del minijuego:
    /// SokobanBoard (reglas), SokobanPlayerController (movimiento/deshacer),
    /// SokobanInputHandler (teclado/táctil), SokobanResourceController (ResourceManager)
    /// y SokobanUIController (todo lo visual).
    ///
    /// API pública para integrarse con el Dashboard u otro menú:
    ///   - Cargar la escena "SokobanScene" (por ejemplo con SceneManager.LoadScene).
    ///   - Suscribirse a LunarOutpost.Core.GameEvents.OnMiniGameFinished para saber cuándo
    ///     el jugador terminó (con o sin éxito) y volver al Dashboard.
    ///   - RequestExit() permite forzar una salida segura desde fuera (por ejemplo, un botón
    ///     global de "Volver" en la barra superior del juego completo).
    /// </summary>
    public sealed class SokobanMiniGame : MonoBehaviour
    {
        [Header("Costos y recompensas (ajustables en el Inspector)")]
        [SerializeField, Range(0f, 100f)] private float restCost = 10f;
        [SerializeField, Range(0f, 100f)] private float powerReward = 25f;

        private SokobanLevelData level;
        private SokobanBoard board;
        private SokobanPlayerController playerController;
        private SokobanResourceController resourceController;
        private SokobanInputHandler inputHandler;
        private SokobanUIController uiController;
        private SokobanAudioController audioController;

        private bool isPaused;
        private bool hasWonCurrentAttempt;

        private void Start()
        {
            EnsureEventSystem();

            level = SokobanLevelData.CreateDefaultLevel();
            ValidateLevelOrLogError();

            board = new SokobanBoard();
            playerController = new SokobanPlayerController(board);
            resourceController = new SokobanResourceController(restCost, powerReward);
            resourceController.EnsureCoreServices();

            inputHandler = gameObject.AddComponent<SokobanInputHandler>();
            audioController = gameObject.AddComponent<SokobanAudioController>();
            uiController = gameObject.AddComponent<SokobanUIController>();

            SokobanUICallbacks callbacks = new SokobanUICallbacks
            {
                RequestDirection = HandleDirectionRequested,
                Undo = HandleUndo,
                Reset = HandleReset,
                StartOrRetryMission = HandleStartOrRetryMission,
                Pause = HandlePause,
                Resume = HandleResume,
                ExitToMenu = HandleExit,
            };
            uiController.BuildInterface(level.Width, level.Height, callbacks);

            inputHandler.OnDirectionRequested += HandleDirectionRequested;
            playerController.OnLevelSolved += HandleLevelSolved;

            board.LoadLevel(level);
            uiController.RefreshBoard(board);
            RefreshHud();
            uiController.SetStatus("Preparando la misión...", false);
            RefreshControlsInteractable();
        }

        /// <summary>
        /// Recalcula qué botones deben estar activos a partir del estado actual
        /// (isPlaying, si hay historial para deshacer, y si ya se ganó este intento).
        /// CORRECCIÓN: antes esto solo se llamaba en 4 puntos del ciclo de vida (inicio,
        /// comenzar misión, ganar, salir), así que el botón "UNDO" quedaba deshabilitado
        /// para siempre en cuanto se iniciaba una misión, sin importar cuántos movimientos
        /// se hicieran. Ahora se llama también después de cada movimiento, de cada "Undo" y
        /// de cada "Restart", para que el botón refleje el historial real en todo momento.
        /// </summary>
        private void RefreshControlsInteractable()
        {
            uiController.SetControlsInteractable(
                isPlaying: playerController.IsActive,
                canUndo: playerController.CanUndo,
                missionButtonEnabled: !playerController.IsActive,
                hasWon: hasWonCurrentAttempt);
        }

        /// <summary>Permite forzar una salida segura desde fuera de este componente (por ejemplo, un botón global).</summary>
        public void RequestExit() => HandleExit();

        private void ValidateLevelOrLogError()
        {
            var errors = SokobanLevelValidator.ValidateStructure(level);
            if (errors.Count > 0)
            {
                Debug.LogError("Sokoban: el nivel por defecto tiene errores de diseño: " + string.Join(" | ", errors));
                return;
            }

            if (!SokobanLevelValidator.IsSolvable(level))
                Debug.LogError("Sokoban: el nivel por defecto no tiene solución encontrada por el solver.");
        }

        private void HandleDirectionRequested(Vector2Int direction)
        {
            if (isPaused || !playerController.IsActive)
                return;

            Vector2Int before = board.PlayerPosition;
            bool batteryWasThere = board.BatteryIndexAt(board.PlayerPosition + direction) >= 0;
            bool moved = playerController.Move(direction);
            if (!moved)
                return;

            if (batteryWasThere) audioController.PlayPush();
            else audioController.PlayMove();

            uiController.RefreshBoard(board);
            RefreshHud();
            RefreshControlsInteractable(); // ver corrección: mantiene "UNDO" sincronizado con el historial.
            _ = before; // conservado para depuración futura si hace falta animar el tramo recorrido.
        }

        private void HandleUndo()
        {
            if (isPaused) return;
            if (playerController.Undo())
            {
                audioController.PlayUndo();
                uiController.RefreshBoard(board);
                RefreshHud();
                RefreshControlsInteractable();
                uiController.SetStatus("Movimiento deshecho. ¡Puedes volver a intentarlo!", false);
            }
        }

        private void HandleReset()
        {
            if (isPaused || !playerController.IsActive) return;

            // Reiniciar el tablero NO vuelve a cobrar descanso: sigue siendo la misma misión.
            playerController.ResetLevel(level);
            uiController.RefreshBoard(board);
            RefreshHud();
            RefreshControlsInteractable();
            uiController.SetStatus("Tablero reiniciado. No se cobra descanso extra en esta misma misión.", false);
        }

        private void HandleStartOrRetryMission()
        {
            uiController.HideVictoryPanel();

            if (!resourceController.TryBeginAttempt(out string reason))
            {
                uiController.SetStatus(reason + " Descansa y vuelve a intentarlo.", false);
                RefreshHud();
                return;
            }

            hasWonCurrentAttempt = false;
            playerController.StartLevel(level);
            uiController.ShowNextScienceTip();
            uiController.RefreshBoard(board);
            RefreshHud();
            uiController.SetStatus("¡Misión iniciada! Lleva las dos baterías a sus ranuras.", false);
            RefreshControlsInteractable();
        }

        private void HandleLevelSolved()
        {
            float rewardGained = resourceController.GrantVictoryReward();
            string educationalMessage = SokobanUIController.EducationalMessages
                .Last(); // "la energía es limitada": el mensaje más adecuado para el cierre de la misión.

            hasWonCurrentAttempt = true;
            audioController.PlayVictory();
            uiController.SetStatus("¡EXCELENTE! Las dos baterías están conectadas.", true);
            uiController.ShowVictoryPanel(rewardGained, educationalMessage);
            RefreshHud();
            RefreshControlsInteractable();
        }

        private void HandlePause()
        {
            if (!playerController.IsActive) return;
            isPaused = true;
            uiController.ShowPausePanel();
        }

        private void HandleResume()
        {
            isPaused = false;
            uiController.HidePausePanel();
        }

        /// <summary>Salida segura: no otorga ni descuenta recursos; solo avisa que el minijuego terminó sin victoria.</summary>
        private void HandleExit()
        {
            bool wasPlayingWithoutWinning = playerController.IsActive;
            isPaused = false;
            uiController.HidePausePanel();
            uiController.HideVictoryPanel();
            playerController.Deactivate();

            if (wasPlayingWithoutWinning)
                resourceController.NotifyExitWithoutVictory();

            hasWonCurrentAttempt = false;
            uiController.SetStatus("Minijuego en pausa de salida. Vuelve a 'START MISSION' cuando quieras.", false);
            RefreshControlsInteractable();
        }

        private void RefreshHud()
        {
            int connected = board.Batteries.Count(b => b.IsConnected);
            uiController.RefreshHud(
                resourceController.GetPower(),
                resourceController.GetRest(),
                resourceController.RestCost,
                connected,
                SokobanLevelData.RequiredBatteries,
                board.MoveCount);
        }

        /// <summary>El proyecto usa Input System; la UI necesita su módulo de eventos para recibir clics.</summary>
        private void EnsureEventSystem()
        {
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            BaseInputModule currentModule = eventSystem.GetComponent<BaseInputModule>();
            if (!(currentModule is InputSystemUIInputModule))
            {
                if (currentModule != null)
                    Destroy(currentModule);
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
