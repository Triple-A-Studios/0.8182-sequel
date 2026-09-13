using Alchemy.Inspector;
using Bldng = Opoint8182.Building.Building;
using TripleA.Utils.Observables.Primaries;
using UnityEngine;

namespace Opoint8182.Score
{
    public class ScoreSystem : MonoBehaviour
    {
        [Title("Sources")]
        [FoldoutGroup("Sources")] [SerializeField] private Bldng[] m_buildings;

        [Title("Debug")]
        [FoldoutGroup("Debug")] [ShowInInspector] public int CurrentScore => Score.Value;

        private ObservableInt m_score;

        // Lazily constructed for the same reason as FuelSystem.Fuel/HealthSystem.Health -
        // Unity doesn't guarantee Awake order across different GameObjects.
        private ObservableInt Score => m_score ??= new ObservableInt(0);

        public ObservableInt ScoreValue => Score;

        private void OnEnable()
        {
            foreach (var building in m_buildings)
            {
                if (building != null) building.Crashed += HandleCrashed;
            }
        }

        private void OnDisable()
        {
            foreach (var building in m_buildings)
            {
                if (building != null) building.Crashed -= HandleCrashed;
            }
        }

        private void HandleCrashed(float quality)
        {
            Score.Set(Score.Value + 1);
        }
    }
}
