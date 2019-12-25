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
        /// <summary>
        /// Loads a music track at index in factory asynchronously
        /// </summary>
        /// <param name="factory">Factory to load track for</param>
        /// <param name="index">Index of tracks to load</param>
        public void LoadMusicTrack(WireFactory factory, int index)
        {
            MusicTrack track = factory.GetOptionalMusicTrack(index);
            if (track != null && track.music == null)
                StartCoroutine(AsyncLoadMusicTrackRoutine(track));
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

            // Wait for asset bundle to load
            yield return bundleRequest;

            if (!bundleRequest.assetBundle)
                yield break;

            // Setting now without audio clip so it can later be unloaded
            AssetBundle bundle = bundleRequest.assetBundle;
            track.SetMusicAudioClip(bundle, null);

            AssetBundleRequest assetRequest = bundle.LoadAssetAsync(track.assetName);
            if (assetRequest == null)
                yield break;

            yield return assetRequest;

            if (!assetRequest.asset)
                yield break;

            // Now finished, update track to include audio
            AudioClip clip = assetRequest.asset as AudioClip;
            track.SetMusicAudioClip(bundle, clip);
        }
    }
}
