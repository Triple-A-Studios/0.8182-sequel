using TripleA.StateMachine.FSM;

namespace Opoint8182.Game.States
{
    public class MainMenuState : BaseState
    {
        private readonly GameManager m_gameManager;

        public MainMenuState(GameManager gameManager)
        {
            m_gameManager = gameManager;
        }

        public override void OnEnter()
        {
            m_gameManager.RaiseReturnedToMenu();
        }
    }
}
