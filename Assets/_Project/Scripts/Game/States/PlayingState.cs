using TripleA.StateMachine.FSM;

namespace Opoint8182.Game.States
{
    public class PlayingState : BaseState
    {
        private readonly GameManager m_gameManager;

        public PlayingState(GameManager gameManager)
        {
            m_gameManager = gameManager;
        }

        public override void OnEnter()
        {
            // Coming back from Paused is not a new run: RunStarted would re-enable and re-gate everything.
            if (m_gameManager.ConsumeResumeFlag()) return;

            m_gameManager.RaiseRunStarted();
        }
    }
}
