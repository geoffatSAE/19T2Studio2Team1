using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Saved state of a wires game
    /// </summary>
    [Serializable]
    public class WiresSaveData
    {
        [SerializeField] public float m_Score;
        [SerializeField] public int m_MultiplierStage;
        [SerializeField] public string m_WireFactoryPath;
    }

    /// <summary>
    /// Saves the state of an active game of Wires
    /// </summary>
    public class WiresSaveGame : MonoBehaviour
    {
        public static string path
        {
            get { return Application.persistentDataPath; }
        }

        public WiresGameMode m_GameMode;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.O))
                SaveGame(m_GameMode);
            else if (Input.GetKeyDown(KeyCode.P))
                LoadGame(m_GameMode);

        }

        public bool SaveGame(WiresGameMode gameMode)
        {
            if (!gameMode)
                return false;

            WiresSaveData data = SaveState(gameMode);

            string fullPath = path + "/player.fun";         
            FileStream stream = new FileStream(fullPath, FileMode.Create);

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, data);

            stream.Close();

            return true;
        }

        public bool LoadGame(WiresGameMode gameMode)
        {
            if (!gameMode)
                return false;

            WiresSaveData saveData = LoadState();
            if (saveData == null)
                return false;

            if (gameMode && gameMode.worldTheme)
            {
                WireFactory factory = AssetDatabase.LoadAssetAtPath<WireFactory>(saveData.m_WireFactoryPath);
                if (factory)
                {
                    gameMode.worldTheme.SetActiveTheme(factory);

                    if (gameMode.sparkJumper && gameMode.sparkJumper.wire)
                        gameMode.sparkJumper.wire.SetFactory(factory);
                }

            }

            if (gameMode && gameMode.scoreManager)
                gameMode.scoreManager.SetScoreAndStage(saveData.m_Score, saveData.m_MultiplierStage);

            return true;
        }

        private WiresSaveData SaveState(WiresGameMode gameMode)
        {
            if (!gameMode)
                return new WiresSaveData();

            WiresSaveData saveData = new WiresSaveData();

            if (gameMode.sparkJumper && gameMode.sparkJumper.wire)
                saveData.m_WireFactoryPath = AssetDatabase.GetAssetPath(gameMode.sparkJumper.wire.factory);

            if (gameMode.scoreManager)
            {
                saveData.m_Score = gameMode.scoreManager.score;
                saveData.m_MultiplierStage = gameMode.scoreManager.multiplierStage;
            }

            return saveData;
        }

        private WiresSaveData LoadState()
        {
            string fullPath = path + "/player.fun";
            if (File.Exists(fullPath))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(fullPath, FileMode.Open);

                WiresSaveData saveData = formatter.Deserialize(stream) as WiresSaveData;
                stream.Close();

                return saveData;
            }

            return null;
        }
    }
}
