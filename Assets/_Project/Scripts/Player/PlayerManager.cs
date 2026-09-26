using Alchemy.Inspector;
using Opoint8182.Altitude;
using Opoint8182.Common;
using Opoint8182.Fuel;
using Opoint8182.Game;
using Opoint8182.Health;
using Opoint8182.Lateral;
using Opoint8182.Spawning;
using Unity.Cinemachine;
using UnityEngine;

namespace Opoint8182.Player
{
    [RequireComponent(typeof(PlaneController))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(FuelSystem))]
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(AltitudeSystem))]
    [RequireComponent(typeof(LateralSystem))]
    public class PlayerManager : MonoBehaviour
    {
        [Title("References")]
        [FoldoutGroup("References")] [SerializeField] private CinemachineFollow m_followCamera;
        // Scene-only reference (HUD isn't a Plane-prefab sibling) - left empty on Plane.prefab,
        // set via a PrefabInstance modification in Prototype.unity, same pattern as
        // m_followCamera. Gated the same way as PlaneController/FuelSystem/SpawnManager below -
        // without this, the HUD (sharing the same UI Toolkit PanelSettings as MainMenu's own UI)
        // renders on top of the menu the instant both scenes are loaded, before Play is pressed.
        [FoldoutGroup("References")] [SerializeField] private GameObject m_hud;

        private bool m_isRunEnded;

        private PlaneController m_planeController;
        private Rigidbody m_rigidbody;
        private FuelSystem m_fuelSystem;
        private HealthSystem m_healthSystem;
        private AltitudeSystem m_altitudeSystem;
        private LateralSystem m_lateralSystem;
        private GameManager m_gameManager;
        private SpawnManager m_spawnManager;

        private void Awake()
        {
            m_planeController = GetComponent<PlaneController>();
            m_rigidbody = GetComponent<Rigidbody>();
            m_fuelSystem = GetComponent<FuelSystem>();
            m_healthSystem = GetComponent<HealthSystem>();
            m_altitudeSystem = GetComponent<AltitudeSystem>();
            m_lateralSystem = GetComponent<LateralSystem>();
        }

        private void Start()
        {
            // Same Awake-before-Start safety reasoning GameOverUI.cs documents - GenericSingleton
            // sets its instance in Awake, so subscribing from OnEnable risks running before
            // GameManager's own Awake if this object happens to be processed first.
            m_gameManager = GameManager.TryGetInstance();
            m_spawnManager = SpawnManager.TryGetInstance();
            if (m_gameManager == null) return;

            m_gameManager.RunStarted += HandleRunStarted;
            m_gameManager.ReturnedToMenu += HandleReturnedToMenu;

            // Don't just wait to catch a live event - Start() ordering between GameManager and
            // this object isn't guaranteed, so sync to whatever state it's already in too.
            if (m_gameManager.IsPlaying) HandleRunStarted();
            else HandleReturnedToMenu();
        }

        private void OnEnable()
        {
            CombatEvents.Crashed += HandleCrashed;
            CombatEvents.DamageDealt += HandleDamageDealt;
            CombatEvents.Restored += HandleRestored;
            m_fuelSystem.Depleted += HandleDepleted;
            m_healthSystem.Depleted += HandleDepleted;
            m_altitudeSystem.GroundHit += HandleDepleted;
            m_altitudeSystem.CeilingExceeded += HandleFlyAway;
            m_lateralSystem.HardBoundExceeded += HandleFlyAway;
        }

        private void OnDisable()
        {
            CombatEvents.Crashed -= HandleCrashed;
            CombatEvents.DamageDealt -= HandleDamageDealt;
            CombatEvents.Restored -= HandleRestored;
            m_fuelSystem.Depleted -= HandleDepleted;
            m_healthSystem.Depleted -= HandleDepleted;
            m_altitudeSystem.GroundHit -= HandleDepleted;
            m_altitudeSystem.CeilingExceeded -= HandleFlyAway;
            m_lateralSystem.HardBoundExceeded -= HandleFlyAway;

            if (m_gameManager != null)
            {
                m_gameManager.RunStarted -= HandleRunStarted;
                m_gameManager.ReturnedToMenu -= HandleReturnedToMenu;
            }
        }

        private void HandleCrashed(ICrashSource source, float quality, bool countsForCombo)
        {
            if (m_isRunEnded) return;

            m_fuelSystem.Refuel(quality);
        }

        private void HandleDamageDealt(IDamageDealer source, float damage)
        {
            if (m_isRunEnded) return;

            m_fuelSystem.Drain(damage * source.FuelDamageMultiplier);
            m_healthSystem.TakeDamage(damage * source.HealthDamageMultiplier);
        }

        private void HandleRestored(IRestorer source, float amount)
        {
            if (m_isRunEnded) return;

            m_healthSystem.Heal(amount);
        }

        private void HandleDepleted()
        {
            if (m_isRunEnded) return;

            m_isRunEnded = true;
            m_planeController.enabled = false;
            m_rigidbody.linearVelocity = Vector3.zero;
        }

        private void HandleFlyAway()
        {
            if (m_isRunEnded) return;

            m_isRunEnded = true;
            // Unlike HandleDepleted, don't touch linearVelocity - the plane keeps flying off
            // in whatever direction it was last actually moving (no gravity/drag on this
            // Rigidbody, so it drifts on forever once PlaneController stops overwriting it).
            // Shared by AltitudeSystem.CeilingExceeded and LateralSystem.HardBoundExceeded -
            // same "flies off and leaves the camera behind" treatment on every bound.
            m_planeController.enabled = false;
            if (m_followCamera != null) m_followCamera.enabled = false;
        }

        private void HandleRunStarted()
        {
            m_planeController.enabled = true;
            m_fuelSystem.enabled = true;
            if (m_spawnManager != null) m_spawnManager.enabled = true;
            if (m_hud != null) m_hud.SetActive(true);
        }

        private void HandleReturnedToMenu()
        {
            m_planeController.enabled = false;
            m_fuelSystem.enabled = false;
            if (m_spawnManager != null) m_spawnManager.enabled = false;
            if (m_hud != null) m_hud.SetActive(false);
        }
    }
}
