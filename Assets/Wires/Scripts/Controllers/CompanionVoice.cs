using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        // Get the priority of this dialogue
        public int priority { get { return m_Priority; } }

        /// <summary>
        /// Get a random audio clip to play from this dialogue
        /// </summary>
        /// <returns>Audio clip or null</returns>
        public AudioClip GetRandomVoiceline()
        {
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
    }

    /// <summary>
    /// Handles playing the voices clip the players companion
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CompanionVoice : MonoBehaviour
    {
        // Would ideally use a dictionary for these dialogues but dictionaries aren't exposed to the editor

        [Header("Game")]
        public AudioClip m_IntroDialogue;                               // Initial dialogue player hears
        public CompanionVoiceLines m_MultiplierIncreaseDialogue;        // Dialogue for multiplier increase
        //public CompanionVoiceLines m_MultiplierDecreaseDialogue;      // Dialogue for multiplier decrease
        public CompanionVoiceLines m_EndOfWireDialogue;                 // Dialogue for reaching the end of a wire
        //public CompanionVoiceLines m_BoostReadyDialogue;              // Dialogue for when boost is ready
        //public CompanionVoiceLines m_BoostStartDialogue;              // Dialogue for when boost is activated
        //public CompanionVoiceLines m_BoostActiveDialogue;             // Dialogue for when boost is active
        //public CompanionVoiceLines m_BoostEndDialogue;                // Dialogue for when boost has finished
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

        public void PlayIntroDialogue()
        {
            m_DialoguePriority = -1;
            PlayDialogue(m_IntroDialogue, int.MaxValue);
        }

        public void PlayMultiplierIncreaseDialogue()
        {
            PlayDialogue(ref m_MultiplierIncreaseDialogue);
        }

        public void PlayMultiplierDecreaseDialogue()
        {
            //PlayDialogue(ref m_MultiplierDecreaseDialogue);
        }

        public void PlayEndOfWireDialogue()
        {
            PlayDialogue(ref m_EndOfWireDialogue);
        }

        public void PlayBoostReadyDialogue()
        {
            //PlayDialogue(ref m_BoostReadyDialogue);
        }

        public void PlayBoostStartDialogue()
        {
            //PlayDialogue(ref m_BoostStartDialogue);
        }

        public void PlayBoostActiveDialogue(float delay = 1f)
        {
            //PlayDialogue(ref m_BoostActiveDialogue, true, delay);
        }

        public void PlayBoostEndDialogue()
        {
            //PlayDialogue(ref m_BoostEndDialogue);
        }

        public void PlayPacketCollectedDialogue()
        {
            PlayDialogue(ref m_PacketCollectedDialogue);
        }

        public void PlayJumpDialogue()
        {
            PlayDialogue(ref m_JumpDialogue);
        }

        public void PlayFinaleDialogue()
        {
            PlayDialogue(ref m_FinaleDialogue);
        }

        public void PlayFinaleCheerDialogue()
        {
            PlayDialogue(ref m_FinaleCheerDialogue);
        }

        public void PlayGameFinishedDialogue()
        {
            PlayDialogue(ref m_GameFinishedDialogue);
        }

        public void PlayTutorialDialogue(int index)
        {
            if (m_TutorialDialogue != null)
            {
                if (index >= 0 && index < m_TutorialDialogue.Length)
                {
                    m_DialoguePriority = -1;

                    AudioClip clip = m_TutorialDialogue[index];
                    PlayDialogue(clip, int.MaxValue);
                }
            }
        }

        public void PlayTutorialFinishedDialogue()
        {
            m_DialoguePriority = -1;
            PlayDialogue(ref m_TutorialFinishedDialogue);
        }

        public void PlayTutorialSkippedDialogue()
        {
            m_DialoguePriority = -1;
            PlayDialogue(ref m_TutorialSkippedDialogue);
        }

        /// <summary>
        /// Plays a random voiceline from given set of dialogue if priority is greater
        /// </summary>
        /// <param name="voiceLines">Voicelines to play</param>
        /// <param name="overrideActive">If dialogue should override current dialogue if priority is the same</param>
        /// <param name="delay">Delay before starting audio playback</param>
        private void PlayDialogue(ref CompanionVoiceLines voiceLines, bool overrideActive = true, float delay = 0f)
        {
            PlayDialogue(voiceLines.GetRandomVoiceline(), voiceLines.priority, overrideActive, delay);
        }

        /// <summary>
        /// Plays dialogue if priority than active priority
        /// </summary>
        /// <param name="clip">Dialogue to play</param>
        /// <param name="priority">Priority of dialogue</param>
        /// <param name="overrideActive">If dialogue should override current dialogue if priority is the same</param>
        /// <param name="delay">Delay before starting audio playback</param>
        private void PlayDialogue(AudioClip clip, int priority, bool overrideActive = true, float delay = 0f)
        {
            if (!clip)
                return;

            bool playVoiceline = false;

            if (priority > activeDialoguePriority ||
                priority == activeDialoguePriority && overrideActive)
            {
                playVoiceline = true;
            }

            if (playVoiceline)
            {
                m_AudioSource.clip = clip;
                m_AudioSource.time = 0f;
                m_AudioSource.PlayDelayed(delay);

                m_DialoguePriority = priority;
            }
        }
    }
}
