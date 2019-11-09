using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace TO5.Wires
{
    /// <summary>
    /// Struct containing voicelines for companion to spark
    /// </summary>
    [System.Serializable]
    public struct CompanionVoiceLines
    {
        [SerializeField] private AudioClip[] m_Voicelines;          // Voicelines that can be played
        [SerializeField, Min(0)] private int m_Priority;            // Priority of this dialogue
        [SerializeField, Range(0f, 1f)] private float m_Chance;     // Chance for dialogue playing when called (1f for always)

        [SerializeField, Min(0)] private int m_RepeatDelay;     // Artificial delay between two voice lines playing (prevents voices lines happening too often)
        private int m_DelayedCount;                             // Count of how many times a voice line has been delayed

        // Get the priority of this dialogue
        public int priority { get { return m_Priority; } }

        /// <summary>
        /// Get a random audio clip to play from this dialogue
        /// </summary>
        /// <returns>Audio clip or null</returns>
        public AudioClip GetRandomVoiceline()
        {
            ++m_DelayedCount;
            if (m_DelayedCount <= m_RepeatDelay)
                return null;

            // We might not want to play a line
            if (m_Chance < 1f)
                if (Random.Range(0f, 1f) > m_Chance)
                    return null;

            if (m_Voicelines != null && m_Voicelines.Length > 0)
            {
                int index = Random.Range(0, m_Voicelines.Length);
                return m_Voicelines[index];
            }

            return null;
        }

        /// <summary>
        /// Notify that a voice line has been played
        /// </summary>
        public void OnVoicelinePlayed()
        {
            m_DelayedCount = 0;
        }
    }

    /// <summary>
    /// Handles playing the voices clip the players companion
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CompanionVoice : MonoBehaviour
    {
        // Would ideally use a dictionary for these dialogues but dictionaries aren't exposed to the editor

        public AudioMixerGroup m_GameMixer;             // Audio mixer to use when playing normal dialogue
        public AudioMixerGroup m_TutorialMixer;         // Audio mixer to use when playing tutorial dialogue

        [Header("Game")]
        public AudioClip m_IntroDialogue;                               // Initial dialogue player hears
        public CompanionVoiceLines m_MultiplierIncreaseDialogue;        // Dialogue for multiplier increase
        public CompanionVoiceLines m_MultiplierDecreaseDialogue;        // Dialogue for multiplier decrease
        public CompanionVoiceLines m_EndOfWireDialogue;                 // Dialogue for reaching the end of a wire
        public CompanionVoiceLines m_PacketCollectedDialogue;           // Dialogue for when a packet has been collected
        public CompanionVoiceLines m_JumpDialogue;                      // Dialogue for when player has jumped to a spark
        public CompanionVoiceLines m_FinaleDialogue;                    // Dialogue for when the finale starts
        public CompanionVoiceLines m_FinaleCheerDialogue;               // Dialogue for when finale is in progress
        public CompanionVoiceLines m_GameFinishedDialogue;              // Dialogue for when the game has finished

        [Header("Tutorial")]
        public AudioClip[] m_TutorialDialogue;                          // Dialogue for the tutorial
        public CompanionVoiceLines m_TutorialFinishedDialogue;          // Dialogue for when player finished the tutorial
        public CompanionVoiceLines m_TutorialSkippedDialogue;           // Dialogue for when player skipped the tutorial

        private AudioSource m_AudioSource;          // Audio source of companions voice
        private int m_DialoguePriority = -1;        // Priority of last played sound clip

        // The priority of the active dialogue playing (or -1 if no clip is playing)
        private int activeDialoguePriority { get { return m_AudioSource.isPlaying ? m_DialoguePriority : -1; } }

        void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
        }

        /// <summary>
        /// Plays initial dialogue when the game starts, must be called manually
        /// </summary>
        public void PlayIntroDialogue()
        {
            m_DialoguePriority = -1;
            PlayDialogue(m_IntroDialogue, int.MaxValue, m_TutorialMixer);
        }

        /// <summary>
        /// Plays a random voice line relating to multiplier increasing
        /// </summary>
        public void PlayMultiplierIncreaseDialogue()
        {
            PlayDialogue(ref m_MultiplierIncreaseDialogue, m_GameMixer);
        }

        /// <summary>
        /// Plays a random voice line relating to multiplier decreasing
        /// </summary>
        public void PlayMultiplierDecreaseDialogue()
        {
            PlayDialogue(ref m_MultiplierDecreaseDialogue, m_GameMixer);
        }

        /// <summary>
        /// Plays a random voice line relating to reaching end of a wire
        /// </summary>
        public void PlayEndOfWireDialogue()
        {
            PlayDialogue(ref m_EndOfWireDialogue, m_GameMixer);
        }

        /// <summary>
        /// Plays a random voice line relating to collecting a data packet
        /// </summary>
        public void PlayPacketCollectedDialogue()
        {
            PlayDialogue(ref m_PacketCollectedDialogue, m_GameMixer);
        }

        /// <summary>
        /// Plays a random voice line relating to jumping
        /// </summary>
        public void PlayJumpDialogue()
        {
            PlayDialogue(ref m_JumpDialogue, m_GameMixer);
        }

        /// <summary>
        /// Plays a random voice line relating to reaching the finale
        /// </summary>
        public void PlayFinaleDialogue()
        {
            PlayDialogue(ref m_FinaleDialogue, m_GameMixer);
        }

        /// <summary>
        /// Plays a random voice line relating to cheering during the finale
        /// </summary>
        public void PlayFinaleCheerDialogue()
        {
            PlayDialogue(ref m_FinaleCheerDialogue, m_GameMixer);
        }

        /// <summary>
        /// Plays a random voice line relating to reaching the end of the game
        /// </summary>
        public void PlayGameFinishedDialogue()
        {
            PlayDialogue(ref m_GameFinishedDialogue, m_GameMixer);
        }

        /// <summary>
        /// Plays a tutorial dialogue voiceline. Each array element
        /// should correspond to a step of the tutorial
        /// </summary>
        /// <param name="index">Voiceline to index</param>
        /// <returns>Playback time of the voiceline (0 if not playing anything)</returns>
        public float PlayTutorialDialogue(int index)
        {
            if (m_TutorialDialogue != null)
            {
                if (index >= 0 && index < m_TutorialDialogue.Length)
                {
                    m_DialoguePriority = -1;

                    AudioClip clip = m_TutorialDialogue[index];
                    return PlayDialogue(clip, int.MaxValue, m_TutorialMixer);
                }
            }

            return 0f;
        }

        /// <summary>
        /// Plays a random voice line relating to finishing the tutorial
        /// </summary>
        public void PlayTutorialFinishedDialogue()
        {
            m_DialoguePriority = -1;
            PlayDialogue(ref m_TutorialFinishedDialogue, m_TutorialMixer);
        }

        /// <summary>
        /// Plays a random voice line relating to skipping the tutorial
        /// </summary>
        public void PlayTutorialSkippedDialogue()
        {
            m_DialoguePriority = -1;
            PlayDialogue(ref m_TutorialSkippedDialogue, m_TutorialMixer);
        }

        /// <summary>
        /// Plays a random voiceline from given set of dialogue if priority is greater
        /// </summary>
        /// <param name="voiceLines">Voicelines to play</param>
        /// <param name="mixer">The mixer group dialogue should play in</param>
        /// <param name="overrideActive">If dialogue should override current dialogue if priority is the same</param>
        /// <param name="delay">Delay before starting audio playback</param>
        /// <returns>Length of audio clip if playing (0 if not)</returns>
        private float PlayDialogue(ref CompanionVoiceLines voiceLines, AudioMixerGroup mixer, bool overrideActive = true, float delay = 0f)
        {
            float length = PlayDialogue(voiceLines.GetRandomVoiceline(), voiceLines.priority, mixer, overrideActive, delay);
            if (length > 0)
                voiceLines.OnVoicelinePlayed();

            return length;
        }

        /// <summary>
        /// Plays dialogue if priority than active priority
        /// </summary>
        /// <param name="clip">Dialogue to play</param>
        /// <param name="priority">Priority of dialogue</param>
        /// <param name="mixer">The mixer group to update source with</param>
        /// <param name="overrideActive">If dialogue should override current dialogue if priority is the same</param>
        /// <param name="delay">Delay before starting audio playback</param>
        /// <returns>Length of audio clip</returns>
        private float PlayDialogue(AudioClip clip, int priority, AudioMixerGroup mixer, bool overrideActive = true, float delay = 0f)
        {
            if (!enabled || !clip)
                return 0f;

            bool playVoiceline = false;

            if (priority > activeDialoguePriority ||
                priority == activeDialoguePriority && overrideActive)
            {
                playVoiceline = true;
            }

            float playTime = 0f;
            if (playVoiceline)
            {
                m_AudioSource.clip = clip;
                m_AudioSource.time = 0f;

                if (mixer && m_AudioSource.outputAudioMixerGroup != mixer)
                    m_AudioSource.outputAudioMixerGroup = mixer;

                m_AudioSource.PlayDelayed(delay);

                m_DialoguePriority = priority;

                playTime = clip.length;
            }

            return playTime;
        }
    }
}
