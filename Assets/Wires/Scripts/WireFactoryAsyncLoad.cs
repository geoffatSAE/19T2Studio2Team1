using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Responsible for asynchronously loading in wire factory assets
    /// </summary>
    public class WireFactoryAsyncLoad : MonoBehaviour
    {
        private static readonly int MaxLoadOperationsAtOnce = 2;                // Max number of tracks to load in at a time
        private static readonly float PendingLoadInitiationDelay = 0.2f;        // Delay before initiating load of pending track

        private HashSet<int> m_loadingTracks = new HashSet<int>();              // Tracks currently being loaded in
        private List<MusicTrack> m_pendingTracks = new List<MusicTrack>();      // Tracks pending to be loaded in

        /// <summary>
        /// Loads the music track at index in factory asynchronously
        /// </summary>
        /// <param name="factory">Factory to load track for</param>
        /// <param name="index">Index of track to load</param>
        public void LoadMusicTrack(WireFactory factory, int index)
        {
            MusicTrack track = factory.GetOptionalMusicTrack(index);
            if (track != null && track.music == null)
            {
                // Track might already be loading
                if (m_loadingTracks.Contains(track.GetHashCode()))
                    return;

                // We only load in a few at a time so we don't cause
                // hitches by having multiple finish loading at once
                if (m_loadingTracks.Count >= MaxLoadOperationsAtOnce || m_pendingTracks.Count > 0)
                {
                    m_pendingTracks.Add(track);
                    return;
                }

                StartCoroutine(AsyncLoadMusicTrackRoutine(track));
            }
        }

        /// <summary>
        /// Unloads the music track at index in factory
        /// </summary>
        /// <param name="factory">Factory to unload track for</param>
        /// <param name="index">Index of track to unload</param>
        public void UnloadMusicTrack(WireFactory factory, int index)
        {
            MusicTrack track = factory.GetOptionalMusicTrack(index);
            if (track != null)
            {
                // Track might be pending
                m_pendingTracks.Remove(track);

                track.UnloadAudioClip();
            }
        }

        /// <summary>
        /// Routine for handling async load of music track
        /// </summary>
        private IEnumerator AsyncLoadMusicTrackRoutine(MusicTrack track)
        {
            // Bundle request will be null if already loaded
            AssetBundleCreateRequest bundleRequest = null;
            if (!track.LoadAudioClipAsync(ref bundleRequest) || bundleRequest == null)
                yield break;

            int hashCode = track.GetHashCode();
            if (!m_loadingTracks.Add(hashCode))
                Debug.LogWarning("Hashed Value in WireFactoryAsyncLoad already exists!");

            // Wait for asset bundle to load
            yield return bundleRequest;

            if (!bundleRequest.assetBundle)
            {
                FinishAsyncLoadMusicTrack(hashCode);
                yield break;
            }

            // Setting now without audio clip so it can later be unloaded
            AssetBundle bundle = bundleRequest.assetBundle;
            track.SetMusicAudioClip(bundle, null);

            AssetBundleRequest assetRequest = bundle.LoadAssetAsync(track.assetName);
            if (assetRequest == null)
            {
                FinishAsyncLoadMusicTrack(hashCode);
                yield break;
            }

            yield return assetRequest;

            if (assetRequest.asset)
            {
                // Now finished, update track to include audio
                AudioClip clip = assetRequest.asset as AudioClip;
                track.SetMusicAudioClip(bundle, clip);
            }

            FinishAsyncLoadMusicTrack(hashCode);
        }

        /// <summary>
        /// Finishes the loading routine for a music track
        /// Will handle initiating loading for any pending tracks
        /// </summary>
        /// <param name="hashCode">Hash of track that finished loading</param>
        private void FinishAsyncLoadMusicTrack(int hashCode)
        {
            m_loadingTracks.Remove(hashCode);

            // Try start playing any pending tracks
            StartCoroutine(DelayPendingTrackRoutine());
        }

        /// <summary>
        /// Routine for playing the first pending track after a delay
        /// </summary>
        private IEnumerator DelayPendingTrackRoutine()
        {
            yield return new WaitForSeconds(PendingLoadInitiationDelay);

            if (m_pendingTracks.Count > 0 && m_loadingTracks.Count < MaxLoadOperationsAtOnce)
            {
                MusicTrack track = m_pendingTracks[0];
                if (track != null)
                    StartCoroutine(AsyncLoadMusicTrackRoutine(track));

                m_pendingTracks.RemoveAt(0);
            }
        }
    }
}
