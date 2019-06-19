using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    public class WireManagerAlt : MonoBehaviour
    {
        [SerializeField] private int m_MinSegments = 5;         // Min amount of segments per wire
        [SerializeField] private int m_MaxSegments = 15;        // Max amount of segments per wire

        List<Wire> m_Wires;

        void Start()
        {

        }

        void Update()
        {

        }
    }
}
