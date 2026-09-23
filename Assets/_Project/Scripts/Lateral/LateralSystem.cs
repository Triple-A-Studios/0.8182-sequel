using System;
using Alchemy.Inspector;
using TripleA.Utils.Observables.Primaries;
using UnityEngine;

namespace Opoint8182.Lateral
{
    public class LateralSystem : MonoBehaviour
    {
        [Title("Tunables")]
        [FoldoutGroup("Tunables")] [SerializeField] private float m_softBoundX = 12f;
        [FoldoutGroup("Tunables")] [SerializeField] private float m_hardBoundX = 16f;

        private bool m_hardBoundExceeded;

        private ObservableBool m_isWarning;

        public event Action HardBoundExceeded;

        // Lazily constructed for the same reason as AltitudeSystem.IsWarningValue -
        // Unity doesn't guarantee Awake order across different GameObjects.
        private ObservableBool IsWarningObservable => m_isWarning ??= new ObservableBool(false);

        public ObservableBool IsWarningValue => IsWarningObservable;

        private void FixedUpdate()
        {
            if (m_hardBoundExceeded) return;

            var lateralOffset = Mathf.Abs(transform.position.x);

            if (lateralOffset >= m_hardBoundX)
            {
                MarkHardBoundExceeded();
                return;
            }

            // No countdown here, unlike AltitudeSystem's ceiling warning - the developer
            // wants a plain in/out warning state, not a grace-period timer.
            IsWarningObservable.Set(lateralOffset >= m_softBoundX);
        }

        private void MarkHardBoundExceeded()
        {
            m_hardBoundExceeded = true;
            IsWarningObservable.Set(false);
            HardBoundExceeded?.Invoke();
        }
    }
}
