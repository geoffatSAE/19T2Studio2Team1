using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Combat
{
    /// <summary>
    /// Manages the simulation, including scenarios and targets
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        [SerializeField] private bool m_AutoStart = true;       // If the simulation auto starts
        [SerializeField] private CombatController m_Player;     // Players controller
        [SerializeField] private Scenario[] m_Scenarios;        // Scenarios of the simulation

        private bool m_SimulationRunning = false;       // If simulation is running
        private bool m_ScenarioRunning = false;         // If scenario is running
        private Scenario m_ActiveScenario;              // The scenario being simulated
        private ScenarioManager m_ScenarioManager;      // Manager of current scenario
        private int m_ScenarioIndex = -1;               // Index of current scenario
        
        private List<Target> m_Targets;     // Targets from the active scenario

        void Start()
        {
            if (m_AutoStart)
                StartSimulation();
        }

        public void StartSimulation()
        {
            if (!m_SimulationRunning)
            {
                if (m_Scenarios.Length == 0)
                {
                    Debug.LogError("Unable to start simulation as no scenarios have been set", this);
                    return;
                }

                StartScenario(0);
            }
        }

        private void EndSimulation()
        {

        }

        private void StartScenario(int index)
        {
            if (index < 0 || index >= m_Scenarios.Length)
                return;

            Scenario scenario = m_Scenarios[index];
            if (!scenario)
            {
                Debug.LogError(string.Format("Scenario at index {0} is null", m_ActiveScenario), this);
                return;
            }

            m_ActiveScenario = scenario;
            m_ScenarioIndex = index;

            m_ScenarioManager = scenario.CreateInstance(transform);
            if (!m_ScenarioManager)
            {
                Debug.LogError("Failed to create instance for scenario " + scenario.name, this);
                return;
            }
        }

        private void StartNextScenario()
        {
            int nextScenario = m_ScenarioIndex + 1;
            if (nextScenario < m_Scenarios.Length)
                StartScenario(nextScenario);
            else
                EndSimulation();
        }
    }
}
