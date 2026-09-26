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
            m_gameManager.RaiseRunStarted();
        }
    }
}
