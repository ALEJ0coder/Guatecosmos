using LunarOutpost.Core;
using UnityEngine;

namespace LunarOutpost.MiniGames.Sokoban
{
    /// <summary>
    /// Único punto de contacto entre Sokoban y ResourceManager/GameManager/GameEvents.
    /// No crea un sistema de recursos propio: solo llama a la API que ya existe en Core.
    /// Se encarga de que el costo de Descanso se cobre una sola vez por intento y de que
    /// la recompensa de Energía solo se otorgue una vez por victoria.
    /// </summary>
    public sealed class SokobanResourceController
    {
        private readonly float restCost;
        private readonly float powerReward;
        private bool rewardGrantedForCurrentAttempt;

        public SokobanResourceController(float restCost, float powerReward)
        {
            this.restCost = restCost;
            this.powerReward = powerReward;
        }

        /// <summary>Crea GameManager/ResourceManager si la escena se abrió directamente, sin pasar por un menú previo.</summary>
        public void EnsureCoreServices()
        {
            if (ResourceManager.Instance == null)
                new GameObject("ResourceManager").AddComponent<ResourceManager>();

            if (GameManager.Instance == null)
                new GameObject("GameManager").AddComponent<GameManager>();
        }

        public float GetRest() => ResourceManager.Instance.Get(ResourceType.Rest);
        public float GetPower() => ResourceManager.Instance.Get(ResourceType.Power);
        public float RestCost => restCost;
        public float PowerReward => powerReward;

        /// <summary>
        /// Intenta empezar una misión. Cobra el Descanso una sola vez si hay suficiente;
        /// si no alcanza, no cobra nada y devuelve false para que la UI lo explique.
        /// </summary>
        public bool TryBeginAttempt(out string reason)
        {
            EnsureCoreServices();
            float currentRest = GetRest();
            if (currentRest < restCost)
            {
                reason = $"Necesitas {restCost:0} de descanso. Tienes {currentRest:0}.";
                return false;
            }

            ResourceManager.Instance.Change(ResourceType.Rest, -restCost);
            rewardGrantedForCurrentAttempt = false;
            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// Otorga la recompensa de Energía. Protegido contra llamadas duplicadas: si ya se
        /// otorgó para este intento (por ejemplo, por una llamada repetida al evento de victoria),
        /// no vuelve a sumar puntos.
        /// </summary>
        public float GrantVictoryReward()
        {
            if (rewardGrantedForCurrentAttempt)
                return GetPower();

            float before = GetPower();
            float after = ResourceManager.Instance.Change(ResourceType.Power, powerReward);
            rewardGrantedForCurrentAttempt = true;
            GameEvents.RaiseMiniGameFinished("Sokoban", true);
            return after - before;
        }

        /// <summary>Salir sin completar el nivel no debe generar recompensas ni penalizaciones extra.</summary>
        public void NotifyExitWithoutVictory()
        {
            GameEvents.RaiseMiniGameFinished("Sokoban", false);
        }
    }
}
