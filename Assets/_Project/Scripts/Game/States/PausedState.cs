using TripleA.StateMachine.FSM;
using UnityEngine;

namespace Opoint8182.Game.States
{
    /// <summary>
    /// The run is frozen. This is the only place that touches <see cref="Time.timeScale"/> for pausing: every gameplay
    /// script ticks on scaled time (and physics does not step at 0), so freezing the clock freezes the world without
    /// each system knowing about pause. Entered only from <see cref="PlayingState"/> and left only back to it.
    /// </summary>
    public class PausedState : BaseState
    {
        private readonly GameManager m_gameManager;

        public PausedState(GameManager gameManager)
        {
            m_gameManager = gameManager;
        }

        public override void OnEnter()
        {
            Time.timeScale = 0f;
            m_gameManager.RaisePaused();
        }

        public override void OnExit()
        {
            Time.timeScale = 1f;
            m_gameManager.RaiseResumed();
        }
    }
}
