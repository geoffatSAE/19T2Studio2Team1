using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Saved state of a wires game
    /// </summary>
    [Serializable]
    public struct WiresSaveData
    {
        [SerializeField] float m_Score;
        [SerializeField] float m_Distance;
        [SerializeField] int m_MultiplierStage;
        [SerializeField] WireFactory m_Factory;
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


        void Update()
        {
            if (Input.GetKeyDown(KeyCode.O))
                SaveGame(null);
            else if (Input.GetKeyDown(KeyCode.P))
                LoadGame(null);

        }

        public bool SaveGame(WiresGameMode gameMode)
        {
            if (!gameMode)
                return false;

            WiresSaveData data = GetState(gameMode);

            string fullPath = path + "/save.dat";         
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

            return true;
        }

        /// <summary>
        /// Gets the current state of the game if active
        /// </summary>
        /// <param name="gameMode">Game mode to base off of</param>
        /// <returns>Save data of current state</returns>
        private WiresSaveData GetState(WiresGameMode gameMode)
        {
            return new WiresSaveData();
        }
    }
}
