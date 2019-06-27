using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Game mode for a session of wires, handles starting and finishing a session
    /// </summary>
    public abstract class WiresGameMode : MonoBehaviour
    {
        [SerializeField] protected WireManager m_WireManager;      // Wire manager to interact with
    }
}
