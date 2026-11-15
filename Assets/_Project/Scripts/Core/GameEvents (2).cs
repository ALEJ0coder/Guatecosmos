using System;

namespace LunarOutpost.Core
{
    /// <summary>
    /// Eventos compartidos. Los minijuegos anuncian aquí que terminaron,
    /// sin necesitar una referencia directa al Dashboard u otros módulos.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>Parámetros: identificador del minijuego y si fue completado con éxito.</summary>
        public static event Action<string, bool> OnMiniGameFinished;

        public static void RaiseMiniGameFinished(string miniGameId, bool success)
        {
            OnMiniGameFinished?.Invoke(miniGameId, success);
        }
    }
}
