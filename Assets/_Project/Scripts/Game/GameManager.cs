using System;
using Opoint8182.Altitude;
using Opoint8182.Fuel;
using Opoint8182.Game.States;
using Opoint8182.Health;
using Opoint8182.Lateral;
using TripleA.StateMachine.FSM;
using TripleA.Utils.Singletons;
using UnityEngine;

namespace Opoint8182.Game
{
    public class GameManager : GenericSingleton<GameManager>
    {
        public bool IsRunActive { get; private set; } = true;

        /// <summary>Raised once when the run ends, with what ended it.</summary>
        public event Action<RunEndCause> RunEnded;
        public event Action RunStarted;
        public event Action ReturnedToMenu;

        private FuelSystem m_fuelSystem;
        private HealthSystem m_healthSystem;
        private AltitudeSystem m_altitudeSystem;
        private LateralSystem m_lateralSystem;

        private StateMachine m_stateMachine;
        private MainMenuState m_mainMenuState;
        private PlayingState m_playingState;
        private bool m_playRequested;
        private bool m_menuRequested;

        public bool IsPlaying => m_stateMachine.CurrentState is PlayingState;

        // Set by BootstrapLoader.Awake(), before it kicks off any scene loads - a plain static
        // flag, not an async signal, so there's no race window to fall through. Consumed
        // (reset to false) the moment a GameManager reads it in Awake below, so a later
        // GameManager - e.g. the fresh one Restart's single-mode scene reload creates - always
        // sees it already false and defaults straight to Playing, same as standalone testing.
        public static bool ManagedByBootstrap;

        protected override void Awake()
        {
            base.Awake();

            var startInMenu = ManagedByBootstrap;
            ManagedByBootstrap = false;

            m_mainMenuState = new MainMenuState(this);
            m_playingState = new PlayingState(this);
            m_stateMachine = new StateMachine();
            // Registering a transition registers both its states as a side effect (a private
            // GetOrAddNode call) - this is what makes SetState below safe to call regardless of
            // which state it targets.
            m_stateMachine.AddTransition(m_mainMenuState, m_playingState, new FuncPredicate(() => m_playRequested));
            m_stateMachine.AddTransition(m_playingState, m_mainMenuState, new FuncPredicate(() => m_menuRequested));
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
