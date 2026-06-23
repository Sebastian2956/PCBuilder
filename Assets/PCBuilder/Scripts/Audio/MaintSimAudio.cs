using UnityEngine;

namespace PCBuilder.Interaction
{
    public class MaintSimAudio : MonoBehaviour
    {
        private static MaintSimAudio instance;

        [Header("Audio Customization (Optional)")]
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip pickupClip;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip failClip;
        [SerializeField] private AudioClip completeClip;

        private AudioSource audioSource;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D flat sound
        }

        public static void PlaySound(string soundType)
        {
            if (instance == null) return;
            instance.Play(soundType);
        }

        private void Play(string soundType)
        {
            AudioClip clip = GetClip(soundType);
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
            else
            {
                // Generate procedural sound effect so it works beautifully without asset dependencies
                PlayProceduralSound(soundType);
            }
        }

        private AudioClip GetClip(string soundType)
        {
            switch (soundType.ToLower())
            {
                case "hover": return hoverClip;
                case "pickup":
                case "clipopen":
                case "clipclose":
                    return pickupClip;
                case "success":
                case "snap":
                    return successClip;
                case "fail":
                case "reject":
                    return failClip;
                case "complete": return completeClip;
                default: return null;
            }
        }

        private void PlayProceduralSound(string soundType)
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            float frequency = 440f;
            bool isSuccess = false;
            bool isFail = false;
            bool isHover = false;

            switch (soundType.ToLower())
            {
                case "hover":
                    frequency = 800f;
                    duration = 0.03f;
                    isHover = true;
                    break;
                case "pickup":
                case "clipopen":
                case "clipclose":
                    frequency = 550f;
                    duration = 0.08f;
                    break;
                case "snap":
                case "success":
                    frequency = 660f;
                    duration = 0.25f;
                    isSuccess = true;
                    break;
                case "reject":
                case "fail":
                    frequency = 150f;
                    duration = 0.3f;
                    isFail = true;
                    break;
                case "complete":
                    frequency = 880f;
                    duration = 0.5f;
                    isSuccess = true;
                    break;
            }

            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float value = 0f;

                if (isSuccess)
                {
                    // A pleasant major arpeggio chord sweep (C major)
                    float f1 = frequency;
                    float f2 = frequency * 1.25f; // Major 3rd
                    float f3 = frequency * 1.5f;  // Perfect 5th
                    value = Mathf.Sin(2 * Mathf.PI * f1 * t) * 0.3f +
                            Mathf.Sin(2 * Mathf.PI * f2 * t) * 0.2f +
                            Mathf.Sin(2 * Mathf.PI * f3 * t) * 0.2f;
                }
                else if (isFail)
                {
                    // A harsh buzzy descending sawtooth-like sound
                    float currentFreq = Mathf.Lerp(frequency, frequency * 0.5f, t / duration);
                    value = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * currentFreq * t)) * 0.2f;
                }
                else if (isHover)
                {
                    // High pitch subtle blip
                    value = Mathf.Sin(2 * Mathf.PI * frequency * t) * 0.05f;
                }
                else
                {
                    // Regular pitch clean sine blip
                    value = Mathf.Sin(2 * Mathf.PI * frequency * t) * 0.15f;
                }

                // Apply linear decay envelope to prevent clicks
                float envelope = 1f - (t / duration);
                samples[i] = value * envelope;
            }

            AudioClip proceduralClip = AudioClip.Create("ProceduralSound_" + soundType, sampleCount, 1, sampleRate, false);
            proceduralClip.SetData(samples, 0);

            // Play the clip
            audioSource.PlayOneShot(proceduralClip);
        }
    }
}