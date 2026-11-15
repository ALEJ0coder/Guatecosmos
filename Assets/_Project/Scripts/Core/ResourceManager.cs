using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarOutpost.Core
{
    /// <summary>
    /// Los cuatro recursos que el jugador debe administrar en la estación lunar.
    /// </summary>
    public enum ResourceType
    {
        FoodOxygen,
        Power,
        Shielding,
        Rest
    }

    /// <summary>
    /// Guarda y modifica los recursos de la partida. Todos los valores se mantienen entre 0 y 100.
    /// </summary>
    public sealed class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("Valores iniciales (0 a 100)")]
        [SerializeField, Range(0f, 100f)] private float initialFoodOxygen = 60f;
        [SerializeField, Range(0f, 100f)] private float initialPower = 60f;
        [SerializeField, Range(0f, 100f)] private float initialShielding = 60f;
        [SerializeField, Range(0f, 100f)] private float initialRest = 60f;

        private readonly Dictionary<ResourceType, float> values = new Dictionary<ResourceType, float>();

        /// <summary>
        /// Se dispara cuando cambia un recurso: tipo, valor anterior y valor nuevo.
        /// </summary>
        public event Action<ResourceType, float, float> OnResourceChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResetResources();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Restaura los recursos a sus valores iniciales configurados en el Inspector.</summary>
        public void ResetResources()
        {
            values[ResourceType.FoodOxygen] = Mathf.Clamp(initialFoodOxygen, 0f, 100f);
            values[ResourceType.Power] = Mathf.Clamp(initialPower, 0f, 100f);
            values[ResourceType.Shielding] = Mathf.Clamp(initialShielding, 0f, 100f);
            values[ResourceType.Rest] = Mathf.Clamp(initialRest, 0f, 100f);
        }

        /// <summary>Devuelve el valor actual de un recurso.</summary>
        public float Get(ResourceType resource)
        {
            if (!values.ContainsKey(resource))
            {
                values[resource] = 0f;
            }

            return values[resource];
        }

        /// <summary>
        /// Suma o resta un recurso. El resultado nunca será menor que 0 ni mayor que 100.
        /// Ejemplo: Change(ResourceType.FoodOxygen, 20f).
        /// </summary>
        public float Change(ResourceType resource, float amount)
        {
            float previousValue = Get(resource);
            float newValue = Mathf.Clamp(previousValue + amount, 0f, 100f);

            values[resource] = newValue;
            OnResourceChanged?.Invoke(resource, previousValue, newValue);

            return newValue;
        }

        /// <summary>Establece directamente el valor de un recurso, respetando el límite 0–100.</summary>
        public void Set(ResourceType resource, float value)
        {
            float previousValue = Get(resource);
            float newValue = Mathf.Clamp(value, 0f, 100f);

            values[resource] = newValue;
            OnResourceChanged?.Invoke(resource, previousValue, newValue);
        }
    }
}
