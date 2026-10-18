using System;
using Opoint8182.Altitude;
using Opoint8182.Fuel;
using Opoint8182.Game.States;
using Opoint8182.Health;
using Opoint8182.Lateral;
using TripleA.StateMachine.FSM;
using TripleA.Utils.Singletons;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Opoint8182.Game
{
    public class GameManager : GenericSingleton<GameManager>
    {
        [Tooltip("Practice run (no leaderboard attempt). Set before pressing Play: the HUD tag and the pause popup read it when the HUD builds. Daily play structure replaces this with the real mode.")]
        [SerializeField] private bool m_isPracticeRun;

        public bool IsRunActive { get; private set; } = true;

        /// <summary>This run does not count as a leaderboard attempt, so leaving it costs nothing.</summary>
        public bool IsPracticeRun => m_isPracticeRun;

        /// <summary>Raised once when the run ends, with what ended it.</summary>
        public event Action<RunEndCause> RunEnded;
        public event Action RunStarted;
        public event Action ReturnedToMenu;

        /// <summary>The run froze (<c>Time.timeScale</c> is 0 by the time this fires).</summary>
        public event Action Paused;

        /// <summary>The run unfroze (<c>Time.timeScale</c> is back to 1).</summary>
        public event Action Resumed;

        private FuelSystem m_fuelSystem;
        private HealthSystem m_healthSystem;
        private AltitudeSystem m_altitudeSystem;
        private LateralSystem m_lateralSystem;

        private StateMachine m_stateMachine;
        private MainMenuState m_mainMenuState;
        private PlayingState m_playingState;
        private PausedState m_pausedState;
        private bool m_playRequested;
        private bool m_menuRequested;
        private bool m_pauseRequested;
        private bool m_resumeRequested;
        private bool m_resuming;

        public bool IsPlaying => m_stateMachine.CurrentState is PlayingState;

        /// <summary>
        /// The run is paused, or a pause has been requested and takes effect on the next Update. Counting the pending
        /// request means a hit-stop ending in that one-frame gap still sees the pause and leaves the clock alone.
        /// </summary>
        public bool IsPaused => (m_pauseRequested || m_stateMachine.CurrentState is PausedState) && !m_resuming;

        // Set by BootstrapLoader.Awake(), before it kicks off any scene loads - a plain static
        // flag, not an async signal, so there's no race window to fall through. Consumed
        // (reset to false) the moment a GameManager reads it in Awake below, so a later
        // GameManager - e.g. the fresh one Restart's single-mode scene reload creates - always
        // sees it already false and defaults straight to Playing, same as standalone testing.
        public static bool ManagedByBootstrap;

        protected override void Awake()
        {
            base.Awake();

            // A pause (or hit-stop) cut off by a scene reload must not leak its timeScale into the new scene.
            Time.timeScale = 1f;

            var startInMenu = ManagedByBootstrap;
            ManagedByBootstrap = false;

            m_mainMenuState = new MainMenuState(this);
            m_playingState = new PlayingState(this);
            m_pausedState = new PausedState(this);
            m_stateMachine = new StateMachine();
            // Registering a transition registers both its states as a side effect (a private
            // GetOrAddNode call) - this is what makes SetState below safe to call regardless of
            // which state it targets.
            m_stateMachine.AddTransition(m_mainMenuState, m_playingState, new FuncPredicate(() => m_playRequested));
            m_stateMachine.AddTransition(m_playingState, m_mainMenuState, new FuncPredicate(() => m_menuRequested));
            m_stateMachine.AddTransition(m_playingState, m_pausedState, new FuncPredicate(() => m_pauseRequested));
            m_stateMachine.AddTransition(m_pausedState, m_playingState, new FuncPredicate(() => m_resumeRequested));
            m_stateMachine.SetState(startInMenu ? m_mainMenuState : m_playingState);
        }

        private void Update()
        {
            m_stateMachine.OnUpdate();
        }

        // Flips a flag rather than calling SetState directly - the transition fires on the next
        // OnUpdate (one-frame delay), imperceptible for a UI button click or a startup sequence
        // a real build's splash screen already covers.
        public void EnterPlayingState()
        {
            m_playRequested = true;
        }

        public void EnterMainMenuState()
        {
            m_menuRequested = true;
        }

        /// <summary>
        /// Freezes the run. Ignored unless the run is being played and still active, so it is safe to call from a button or
        /// the back key at any time. Takes effect on the next Update, like the other state requests.
        /// </summary>
        public void Pause()
        {
            if (!IsPlaying || !IsRunActive || m_pauseRequested) return;

            m_pauseRequested = true;
        }

        /// <summary>Unfreezes a paused run (next Update). Ignored when not paused.</summary>
        public void Resume()
        {
            if (!IsPaused || m_resumeRequested) return;

            m_resumeRequested = true;
        }

        /// <summary>Reloads the game scene for a fresh run. Shared by Game Over RETRY and the pause popup's RESTART RUN.</summary>
        public void RestartRun()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Called only from PlayingState/MainMenuState.OnEnter - GameManager owns the events,
        // the states just notify it when they become active.
        internal void RaiseRunStarted()
        {
            m_playRequested = false;
            RunStarted?.Invoke();
        }

        internal void RaiseReturnedToMenu()
        {
            m_menuRequested = false;
            ReturnedToMenu?.Invoke();
        }

        internal void RaisePaused()
        {
            // Cleared after the event: the state machine only switches node once OnEnter returns, so until then the
            // pending request is what keeps IsPaused true for the handlers.
            Paused?.Invoke();
            m_pauseRequested = false;
        }

        internal void RaiseResumed()
        {
            m_resumeRequested = false;
            m_resuming = true;
            Resumed?.Invoke();
        }

        // Read by PlayingState.OnEnter: true exactly once after leaving Paused, so that entry skips RunStarted.
        internal bool ConsumeResumeFlag()
        {
            var resuming = m_resuming;
            m_resuming = false;
            return resuming;
        }

        private void OnEnable()
        {
            m_fuelSystem = FindAnyObjectByType<FuelSystem>();
            m_healthSystem = FindAnyObjectByType<HealthSystem>();
            m_altitudeSystem = FindAnyObjectByType<AltitudeSystem>();
            m_lateralSystem = FindAnyObjectByType<LateralSystem>();

            if (m_fuelSystem != null) m_fuelSystem.Depleted += HandleFuelDepleted;
            if (m_healthSystem != null) m_healthSystem.Depleted += HandleHealthDepleted;
            if (m_altitudeSystem != null)
            {
                m_altitudeSystem.CeilingExceeded += HandleCeilingExceeded;
                m_altitudeSystem.GroundHit += HandleGroundHit;
            }
            if (m_lateralSystem != null) m_lateralSystem.HardBoundExceeded += HandleHardBoundExceeded;
        }

        private void OnDisable()
        {
            if (m_fuelSystem != null) m_fuelSystem.Depleted -= HandleFuelDepleted;
            if (m_healthSystem != null) m_healthSystem.Depleted -= HandleHealthDepleted;
            if (m_altitudeSystem != null)
            {
                m_altitudeSystem.CeilingExceeded -= HandleCeilingExceeded;
                m_altitudeSystem.GroundHit -= HandleGroundHit;
            }
            if (m_lateralSystem != null) m_lateralSystem.HardBoundExceeded -= HandleHardBoundExceeded;
        }

        private void HandleFuelDepleted() => EndRun(RunEndCause.OutOfFuel);

        private void HandleHealthDepleted() => EndRun(RunEndCause.Wrecked);

        private void HandleCeilingExceeded() => EndRun(RunEndCause.FlewTooHigh);

        private void HandleGroundHit() => EndRun(RunEndCause.HitTheDeck);

        private void HandleHardBoundExceeded() => EndRun(RunEndCause.OutOfBounds);

        // First trigger wins: a second one in the same frame (fuel and health, ground and fuel) must not
        // overwrite the cause the player is about to be shown.
        private void EndRun(RunEndCause cause)
        {
            if (!IsRunActive) return;

            IsRunActive = false;
            RunEnded?.Invoke(cause);
        }
    }
}
