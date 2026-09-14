using Alchemy.Inspector;
using Opoint8182.Common;
using TripleA.Utils.Observables.Primaries;
using UnityEngine;

namespace Opoint8182.Score
{
    public class ScoreSystem : MonoBehaviour
    {
        [Title("Combo")]
        [FoldoutGroup("Combo")] [SerializeField] private float m_comboWindow = 3f;
        [FoldoutGroup("Combo")] [SerializeField] private int[] m_comboStepSizes = { 3, 5, 8 };
        [FoldoutGroup("Combo")] [SerializeField] private float m_hazardTimerPenalty = 1.5f;

        [Title("Debug")]
        [FoldoutGroup("Debug")] [ShowInInspector] public int CurrentScore => Score.Value;
        [FoldoutGroup("Debug")] [ShowInInspector] public int CurrentMultiplier => Multiplier.Value;
        [FoldoutGroup("Debug")] [ShowInInspector] public float CurrentComboTimer => ComboTimer.Value;
        [FoldoutGroup("Debug")] [ReadOnly, ShowInInspector] private int m_chainCrashCount;

        private ObservableInt m_score;
        private ObservableInt m_multiplier;
        private ObservableFloat m_comboTimer;

        // Lazily constructed for the same reason as FuelSystem.Fuel/HealthSystem.Health -
        // Unity doesn't guarantee Awake order across different GameObjects.
        private ObservableInt Score => m_score ??= new ObservableInt(0);
        private ObservableInt Multiplier => m_multiplier ??= new ObservableInt(1);
        private ObservableFloat ComboTimer => m_comboTimer ??= new ObservableFloat(0f);

        public ObservableInt ScoreValue => Score;
        public ObservableInt MultiplierValue => Multiplier;
        public ObservableFloat ComboTimerValue => ComboTimer;
        public float ComboTimerFraction => m_comboWindow > 0f ? Mathf.Clamp01(ComboTimer.Value / m_comboWindow) : 0f;

        private void OnEnable()
        {
            CombatEvents.Crashed += HandleCrashed;
            CombatEvents.DamageDealt += HandleHazardHit;
        }

        private void OnDisable()
        {
            CombatEvents.Crashed -= HandleCrashed;
            CombatEvents.DamageDealt -= HandleHazardHit;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void HandleCrashed(ICrashSource source, float quality)
        {
            var scoreValue = source.ScoreValue;
            var bonus = Mathf.RoundToInt(quality * scoreValue);
            var crashScore = (scoreValue + bonus) * Multiplier.Value;
            Score.Set(Score.Value + crashScore);

            m_chainCrashCount++;
            ComboTimer.Set(m_comboWindow);

            var tierIndex = Multiplier.Value - 1;
            if (tierIndex < m_comboStepSizes.Length && m_chainCrashCount >= m_comboStepSizes[tierIndex])
            {
                m_chainCrashCount = 0;
                Multiplier.Set(Multiplier.Value + 1);
            }
        }

        private void HandleHazardHit(IDamageDealer source, float damage)
        {
            if (ComboTimer.Value <= 0f) return;

            Tick(m_hazardTimerPenalty);
        }

        // Shared by the per-frame decay and the hazard-hit timer penalty, so the zero-crossing
        // step-down logic isn't duplicated between them. Drops the multiplier one tier at a time,
        // restarting the window each time until it bottoms out at x1 - not a single step down
        // followed by parking at 0.
        private void Tick(float deltaSeconds)
        {
            if (ComboTimer.Value <= 0f) return;

            var remaining = ComboTimer.Value - deltaSeconds;
            if (remaining > 0f)
            {
                ComboTimer.Set(remaining);
                return;
            }

            m_chainCrashCount = 0;

            if (Multiplier.Value > 1)
            {
                Multiplier.Set(Multiplier.Value - 1);

                // Still above the floor - restart the countdown so decay keeps going one tier per
                // window until it bottoms out, instead of parking at 0 after a single step-down.
                ComboTimer.Set(Multiplier.Value > 1 ? m_comboWindow : 0f);
            }
            else
            {
                ComboTimer.Set(0f);
            }
        }
    }
}
