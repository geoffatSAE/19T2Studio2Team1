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
        
        [Min(0.01f)] public float m_BeatRate;       // Rate of musics beat
        [Min(0.01f)] public float m_BeatDelay;      // Initial delay of musics beat

        public AudioClip m_Music;                   // Music to play

        public bool LoadAudioClip()
        {

            return m_Music != null;

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

            // TODO: Check if asset importer has asset bundle already specified (use that instead)

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
        void OnEnable()
        {
            MusicTrack track = GetMusicTrack(0);
            if (track != null)
            {
                track.LoadAudioClip();
            }
        }

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

        /// <summary>
        /// Get music at track
        /// </summary>
        /// <param name="index">Index of track</param>
        /// <returns>Audio clip or default track</returns>
        public MusicTrack GetMusicTrack(int index)
        {
            if (m_MusicTracks == null || m_MusicTracks.Length == 0)
                return new MusicTrack();

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
            string directory = Directory.GetCurrentDirectory() + "/AssetBundles";
            if (!Directory.Exists(directory))
            {
                DirectoryInfo info = Directory.CreateDirectory(directory);
                Debug.Log(string.Format("Created new directory for asset bundles at {0}", info.FullName));
            }

            if (bundleBuilds.Count > 0)
                BuildPipeline.BuildAssetBundles(directory, bundleBuilds.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression, targetPlatform);
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

        [MenuItem("TO5/Build Asset Bundles (Factories)/Active Build Target", false, 0)]
        public static void BuildAssetBundles_TargetPlatform()
        {
            BuildAssetBundles(EditorUserBuildSettings.activeBuildTarget);
        }

        //[MenuItem("TO5/Build Asset Bundles (Factories)/Android")]
        public static void BuildAssetBundles_Android()
        {
            BuildAssetBundles(BuildTarget.Android);
        }

        //[MenuItem("TO5/Build Asset Bundles (Factories)/Windows")]
        public static void BuildAssetBundles_Windows()
        {
            BuildAssetBundles(BuildTarget.StandaloneWindows64);
        }
    }
#endif
}