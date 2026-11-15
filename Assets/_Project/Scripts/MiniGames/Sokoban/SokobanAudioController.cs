using UnityEngine;

namespace LunarOutpost.MiniGames.Sokoban
{
    /// <summary>
    /// Reproduce efectos de sonido opcionales. El minijuego no trae clips de audio propios
    /// (sección 6/10: nada de dependencias externas obligatorias), así que cada método es un
    /// no-op seguro si no se asignó un AudioClip en el Inspector. El equipo puede arrastrar
    /// sus propios clips más adelante sin tocar código.
    /// </summary>
    public sealed class SokobanAudioController : MonoBehaviour
    {
        [Header("Clips opcionales (se puede dejar todo vacío)")]
        [SerializeField] private AudioClip moveClip;
        [SerializeField] private AudioClip pushClip;
        [SerializeField] private AudioClip undoClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            if (source == null)
                source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        public void PlayMove() => PlayClip(moveClip);
        public void PlayPush() => PlayClip(pushClip);
        public void PlayUndo() => PlayClip(undoClip);
        public void PlayVictory() => PlayClip(victoryClip);

        private void PlayClip(AudioClip clip)
        {
            if (clip == null || source == null)
                return;

            source.PlayOneShot(clip, volume);
        }
    }
}
