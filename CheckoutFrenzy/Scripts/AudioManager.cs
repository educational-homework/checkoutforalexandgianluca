using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField, Tooltip("Audio source for playing background music (BGM)")]
        private AudioSource BGMPlayer;

        [SerializeField, Tooltip("Audio source for playing sound effects (SFX)")]
        private AudioSource SFXPlayer;

        [SerializeField, Tooltip("Duration for fading in/out background music")]
        private float fadeDuration = 0.75f;

        [SerializeField, Tooltip("List of sound effects data")]
        private List<AudioData> SFXList;

        [SerializeField, Tooltip("List of background music tracks to play in sequence")]
        private List<AudioClip> BGMPlaylist = new List<AudioClip>();

        // A dictionary that maps AudioID values to their corresponding AudioData.
        private Dictionary<AudioID, AudioData> SFXLookup;

        private int currentBGMIndex = 0;
        private bool isPlayingQueue = false;
        private float originalBGMVolume;

        void Awake()
        {
            // Singleton pattern to ensure only one instance of AudioManager exists across scenes.
            if (Instance == null)
            {
                Instance = this; // If no instance exists, assign this instance as the singleton
                DontDestroyOnLoad(gameObject); // Prevent the instance from being destroyed when loading new scenes
            }
            else
            {
                Destroy(gameObject); // Destroy the duplicate instance
                return; // Exit the method to prevent further execution
            }

            originalBGMVolume = BGMPlayer.volume;

            SFXLookup = SFXList.ToDictionary(x => x.id);
        }

        /// <summary>
        /// Plays a sound effect (SFX) by playing the given AudioClip.
        /// Optionally pauses the BGM while the SFX is playing and restores the BGM after the clip finishes.
        /// </summary>
        /// <param name="clip">The AudioClip representing the sound effect to play.</param>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;

            SFXPlayer.PlayOneShot(clip);
        }

        /// <summary>
        /// Plays a sound effect (SFX) based on an audio ID.
        /// Optionally pauses the BGM while the SFX is playing and restores the BGM after the clip finishes.
        /// </summary>
        /// <param name="audioID">The AudioID representing the sound effect to play.</param>
        /// <param name="pauseBGM">Determines whether to pause the background music while the SFX is playing (default is false).</param>
        public void PlaySFX(AudioID audioID)
        {
            if (!SFXLookup.ContainsKey(audioID)) return;

            var audioData = SFXLookup[audioID];
            PlaySFX(audioData.clip);
        }

        /// <summary>
        /// Starts playing the background music (BGM) playlist in sequence.
        /// If fading is enabled, the transition between tracks will be smooth.
        /// </summary>
        /// <param name="fade">Determines whether to apply a fade-in effect when starting the BGM queue (default is true).</param>
        public void PlayBGMQueue(bool fade = true)
        {
            if (BGMPlaylist.Count == 0 || isPlayingQueue) return;

            isPlayingQueue = true;
            currentBGMIndex = 0;
            StartCoroutine(PlayBGMQueueCoroutine(fade));
        }

        /// <summary>
        /// Stops playing the background music (BGM) queue.
        /// If fading is enabled, the music will fade out before stopping.
        /// </summary>
        /// <param name="fade">Determines whether to apply a fade-out effect when stopping the BGM queue (default is true).</param>
        public void StopBGMQueue(bool fade = true)
        {
            if (!isPlayingQueue) return;

            isPlayingQueue = false;
            StartCoroutine(StopBGMAsync(fade));
        }

        private IEnumerator PlayBGMQueueCoroutine(bool fade)
        {
            while (isPlayingQueue && BGMPlaylist.Count > 0)
            {
                AudioClip nextBGM = BGMPlaylist[currentBGMIndex];
                yield return PlayBGMAsync(nextBGM, fade);

                // Wait for the track to finish before playing the next one
                yield return new WaitUntil(() => !BGMPlayer.isPlaying || !isPlayingQueue);

                // Stop if the playlist was interrupted
                if (!isPlayingQueue) yield break;

                // Move to the next track, loop back if at the end
                currentBGMIndex = (currentBGMIndex + 1) % BGMPlaylist.Count;
            }
        }

        private IEnumerator PlayBGMAsync(AudioClip clip, bool fade)
        {
            if (fade) yield return BGMPlayer.DOFade(0, fadeDuration).WaitForCompletion();

            BGMPlayer.clip = clip;
            BGMPlayer.Play();

            if (fade) yield return BGMPlayer.DOFade(originalBGMVolume, fadeDuration).WaitForCompletion();
        }

        private IEnumerator StopBGMAsync(bool fade)
        {
            if (fade) yield return BGMPlayer.DOFade(0, fadeDuration).WaitForCompletion();

            BGMPlayer.Stop();
            BGMPlayer.clip = null;
        }
    }

    public enum AudioID
    {
        Pick,
        Throw,
        Impact,
        Draw,
        Flip,
        Scanner,
        Beep,
        Coin,
        Kaching,
        DoorOpen,
        DoorClose,
        SlidingDoor,
        EntranceOpen,
        EntranceClose,
        Click,
        TrashCan,
        LevelUp,
        Construction,
        Bell
    }

    [System.Serializable]
    public class AudioData
    {
        [Tooltip("Unique ID for each audio clip")]
        public AudioID id;

        [Tooltip("The audio clip associated with the audio ID")]
        public AudioClip clip;
    }
}
