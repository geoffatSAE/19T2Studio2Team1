using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace TO5.Wires
{
    /// <summary>
    /// Properties to use when constructing a new orbiter
    /// </summary>
    [System.Serializable]
    public struct OrbiterColorSet
    {
        public Color m_LowColor;                // Color for the low shader parameter of shader
        public Color m_HighColor;               // Color for the high shader parameter of shader
        public Color m_TrailStartColor;         // Color at start of trails
        public Color m_TrailEndColor;           // Color at end of trails
    }

    /// <summary>
    /// Properties about the music to play
    /// </summary>
    [System.Serializable]
    public class MusicTrack
    {
        // Path to audio clip asset. We use this instead of a direct reference
        // to avoid loading in these clips at the start of the game
        [SerializeField, ObjectAssetPath(typeof(AudioClip), "Music")]
        private string m_MusicAssetPath;

        private AssetBundle m_MusicAssetBundle;     // Asset bundle containing music track

        [Min(0.01f)] public float m_BeatRate;       // Rate of musics beat
        [Min(0.01f)] public float m_BeatDelay;      // Initial delay of musics beat

        private AudioClip m_Music = null;           // Music to play (can be null)

        // Get musics asset name in asset bundle
        public string assetName { get { return Path.GetFileNameWithoutExtension(m_MusicAssetPath); } }

        // Get music track if loaded or null
        public AudioClip music { get { return m_Music; } }

        /// <summary>
        /// Loads in the music audio clip synchronously
        /// </summary>
        /// <returns>If audio clip was loaded in successfully</returns>
        public bool LoadAudioClipSync()
        {
#if UNITY_EDITOR
            m_Music = AssetDatabase.LoadAssetAtPath<AudioClip>(m_MusicAssetPath);
#else
            // Only load in once
            if (!m_MusicAssetBundle)
            {
                string name = Path.GetFileNameWithoutExtension(m_MusicAssetPath);
                string bundlePath = Path.Combine(Utility.assetBundleDirectory, name);

                m_MusicAssetBundle = AssetBundle.LoadFromFile(bundlePath);
                if (m_MusicAssetBundle)
                    m_Music = m_MusicAssetBundle.LoadAsset<AudioClip>(name);
            }
#endif

            return m_Music != null;
        }

        /// <summary>
        /// Prepares to load the music audio clip asynchronously
        /// </summary>
        /// <param name="request">Output for request. Null if request failed</param>
        /// <returns>Returns if either request has been made or music clip is already available</returns>
        public bool LoadAudioClipAsync(ref AssetBundleCreateRequest request)
        {
#if UNITY_EDITOR
            m_Music = AssetDatabase.LoadAssetAtPath<AudioClip>(m_MusicAssetPath);
            return true;
#else
            // Only load in once
            if (!m_MusicAssetBundle)
            {
                string name = Path.GetFileNameWithoutExtension(m_MusicAssetPath);
                string bundlePath = Path.Combine(Utility.assetBundleDirectory, name);

                request = AssetBundle.LoadFromFileAsync(bundlePath);
                return request != null;
            }

            // Already loaded in
            return m_Music != null;
#endif
        }

        /// <summary>
        /// Unloads the music audio clip
        /// </summary>
        public void UnloadAudioClip()
        {
            if (m_MusicAssetBundle)
            {
                // Using false, so any assets using music already
                // do not lose their reference (aka WorldMusic)
                m_MusicAssetBundle.Unload(false);
                m_Music = null;
            }
        }

        /// <summary>
        /// Do not call this function. It is used by WireFactoryAsyncLoad script.
        /// This function updates the asset bundle and audio clip of this music track
        /// </summary>
        /// <param name="assetBundle">Asset bundle that has been loaded</param>
        /// <param name="clip">Audio clip that has been loaded</param>
        public void SetMusicAudioClip(AssetBundle assetBundle, AudioClip clip)
        {
            m_MusicAssetBundle = assetBundle;
            m_Music = clip;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Saves the music audio clip to an asset bundle
        /// </summary>
        /// <param name="targetPlatform">Platform to save for</param>
        public AssetBundleBuild SaveToAssetBundle(BuildTarget targetPlatform)
        {
            // Stop here if no track has been set
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(m_MusicAssetPath);
            if (!asset)
                return new AssetBundleBuild();

            // New bundle build
            AssetBundleBuild assetBundleBuild = new AssetBundleBuild();
            assetBundleBuild.assetBundleName = asset.name;
            assetBundleBuild.assetNames = new string[1] { m_MusicAssetPath };

            return assetBundleBuild;
        }
#endif
    }

    /// <summary>
    /// Factory for the types of wires to spawn into the world
    /// Over time, this class has become more of a theme in general, rather than wires
    /// </summary>
    [CreateAssetMenu]
    public class WireFactory : ScriptableObject
    {
        // The color for this factory
        public Color color { get { return m_Color; } }

        // The color for the skybox
        public Color skyboxColor { get { return m_SkyboxColor; } }

        // The color for the outer wires particles
        public Color particleColor { get { return m_ParticleColor; } }

        // The color for sparks
        public Color sparkColor { get { return m_SparkColor; } }

        // The color for the boosts particles
        public ParticleSystem.MinMaxGradient boostColor { get { return m_BoostColor; } }

        // The colors for new orbiters
        public OrbiterColorSet orbiterColors { get { return m_OrbiterColors; } }

        // The texture for the outer wire
        public Texture2D borderTexture { get { return m_BorderTexture; } }

        [SerializeField] private Color m_Color;                                     // Wires color
        [SerializeField] private Color m_SkyboxColor;                               // Skyboxes color
        [SerializeField] private Color m_ParticleColor;                             // Particles color
        [SerializeField] private Color m_SparkColor;                                // Spark color
        [SerializeField] private ParticleSystem.MinMaxGradient m_BoostColor;        // Boost color
        [SerializeField] private OrbiterColorSet m_OrbiterColors;                   // Orbiter colors
        [SerializeField] private Texture2D m_BorderTexture;                         // Texture for outer border
        [SerializeField] private MusicTrack[] m_MusicTracks;                        // Wires music tracks (for each intensity)

#if UNITY_EDITOR
        void Awake()
        {
            // Load now so play in editor can start fast
            foreach (MusicTrack track in m_MusicTracks)
                track.LoadAudioClipSync();
        }
#endif

        void OnDestroy()
        {
            // Clean up
            foreach (MusicTrack track in m_MusicTracks)
                track.UnloadAudioClip();
        }

        public void LoadMusicTrack(int index, bool bAsync)
        {

        }

        /// <summary>
        /// Get music track at index
        /// </summary>
        /// <param name="index">Index of track</param>
        /// <returns>Audio clip or default track</returns>
        public MusicTrack GetMusicTrack(int index)
        {
            MusicTrack track = GetOptionalMusicTrack(index);
            if (track == null)
                track = new MusicTrack();

            return track;
        }

        /// <summary>
        /// Gets music track at index if it exists, otherwise null
        /// </summary>
        /// <param name="index">Index of track</param>
        /// <returns>Audio clip or null</returns>
        public MusicTrack GetOptionalMusicTrack(int index)
        {
            if (m_MusicTracks == null || m_MusicTracks.Length == 0)
                return null;

            index = Mathf.Clamp(index, 0, m_MusicTracks.Length - 1); ;
            return m_MusicTracks[index];
        }

#if UNITY_EDITOR
        /// <summary>
        /// Saves contents of this wire factory to an asset bundle
        /// This does NOT save the factory itself in the bundle
        /// </summary>
        /// <param name="targetPlatform">Platform to save for</param>
        public void SaveToAssetBundle(BuildTarget targetPlatform)
        {
            List<AssetBundleBuild> bundleBuilds = new List<AssetBundleBuild>();

            foreach (MusicTrack track in m_MusicTracks)
            {
                AssetBundleBuild build = track.SaveToAssetBundle(targetPlatform);
                if (!string.IsNullOrEmpty(build.assetBundleName))
                    bundleBuilds.Add(build);
            }

            // Create directory if it doesn't exist already
            string directory = Utility.assetBundleDirectory;
            if (!Directory.Exists(directory))
            {
                DirectoryInfo info = Directory.CreateDirectory(directory);
                Debug.Log(string.Format("Created new directory for asset bundles at {0}", info.FullName));
            }

            if (bundleBuilds.Count > 0)
                BuildPipeline.BuildAssetBundles(directory, bundleBuilds.ToArray(), BuildAssetBundleOptions.None, targetPlatform);
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Defines functions for building wire factory asset bundles
    /// </summary>
    public class WireFactoryBundleBuilder
    {
        public static void BuildAssetBundles(BuildTarget targetPlatform)
        {
            List<WireFactory> factories = new List<WireFactory>();

            string[] guids = AssetDatabase.FindAssets("t:WireFactory");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                factories.Add(AssetDatabase.LoadAssetAtPath<WireFactory>(assetPath));
            }

            foreach (WireFactory factory in factories)
                if (factory)
                    factory.SaveToAssetBundle(targetPlatform);

        }

        [MenuItem("TO5/Build Factory Asset Bundles")]
        public static void BuildAssetBundles_TargetPlatform()
        {
            BuildAssetBundles(EditorUserBuildSettings.activeBuildTarget);
        }
    }
#endif
}