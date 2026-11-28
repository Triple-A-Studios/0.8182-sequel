using System;
using Alchemy.Inspector;
using Opoint8182.Audio;
using Opoint8182.Game;
using Opoint8182.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Opoint8182.Menu3D
{
	/// <summary>
	/// The 3D backdrop of the main menu (3D art pass, Pass 7): the equipped plane parked on a rooftop in the game scene, and
	/// the short takeoff from there into a run. It only moves a stand-in model and the camera. The real plane stays at its
	/// start position with its model hidden, so a run begins exactly as it does without the takeoff: the stand-in ends on
	/// that position, moving at the flight speed, with the camera where the follow camera sits.
	/// While the stage shows, the Cinemachine brain is off and the stage drives the camera itself.
	/// </summary>
	public class MenuStage : MonoBehaviour
	{
		[Title("References")]
		[FoldoutGroup("References")] [SerializeField] private GameObject m_content;
		[FoldoutGroup("References")] [SerializeField] private Transform m_parkedPoint;
		[FoldoutGroup("References")] [SerializeField] private Transform m_planeAnchor;
		[FoldoutGroup("References")] [SerializeField] private ParticleSystem m_idleDust;
		[FoldoutGroup("References")] [SerializeField] private ParticleSystem m_launchDust;

		[Title("Menu Camera (relative to the parked plane)")]
		[FoldoutGroup("Menu Camera")] [SerializeField] private Vector3 m_cameraOffset = new(-6.5f, 2.8f, -10f);
		[FoldoutGroup("Menu Camera")] [SerializeField] private Vector3 m_cameraLookOffset = new(0f, 1.6f, 0f);
		[FoldoutGroup("Menu Camera")] [SerializeField] private float m_cameraFov = 40f;

		// A hand-held camera's drift on the menu view. It fades out as a takeoff starts. The Screen shake setting turned down
		// scales it down too (0% holds the camera still).
		[Title("Handheld Noise")]
		[FoldoutGroup("Handheld Noise")] [SerializeField] private Vector3 m_noiseDegrees = new(0.6f, 0.8f, 0.4f);
		[FoldoutGroup("Handheld Noise")] [SerializeField] private float m_noiseDistance = 0.06f;
		[FoldoutGroup("Handheld Noise")] [SerializeField] private float m_noiseSpeed = 0.35f;

		[Title("Idle")]
		[FoldoutGroup("Idle")] [SerializeField] private float m_bobHeight = 0.07f;
		[FoldoutGroup("Idle")] [SerializeField] private float m_bobCyclesPerSecond = 0.6f;
		[FoldoutGroup("Idle")] [SerializeField] private float m_rollDegrees = 1.5f;

		[Title("Takeoff (seconds from the start)")]
		[FoldoutGroup("Takeoff")] [SerializeField] private float m_windUpEnd = 0.45f;
		[FoldoutGroup("Takeoff")] [SerializeField] private float m_launchStart = 0.65f;
		[FoldoutGroup("Takeoff")] [SerializeField] private float m_duration = 1.55f;
		[FoldoutGroup("Takeoff")] [SerializeField] private float m_pullBackDistance = 2.2f;
		[FoldoutGroup("Takeoff")] [SerializeField] private float m_hopHeight = 2.8f;
		[FoldoutGroup("Takeoff")] [SerializeField] private Vector3 m_squashScale = new(1.15f, 0.72f, 0.85f);
		[FoldoutGroup("Takeoff")] [SerializeField] private Vector3 m_stretchScale = new(0.92f, 1.05f, 1.28f);
		[FoldoutGroup("Takeoff")] [SerializeField] private float m_cameraSwingStart = 0.15f;
		[FoldoutGroup("Takeoff")] [SerializeField] private float m_cameraSwingEnd = 1.25f;
		// Extra camera height over the launch, so it passes well above the roof's front wall; gone again when it arrives.
		[FoldoutGroup("Takeoff")] [SerializeField] private float m_cameraLift = 2f;

		private const float k_MaxStepSeconds = 0.05f;
		private const float k_WindUpPitch = -8f;

		private static bool m_s_hudEntrance;

		private GameManager m_gameManager;
		private PlaneController m_plane;
		private GameObject m_planeVisual;
		private Camera m_camera;
		private CinemachineBrain m_brain;
		private CinemachineCamera m_followCamera;
		private CinemachineFollow m_follow;
		private GameObject m_model;
		private float m_time;
		private bool m_takingOff;
		private bool m_takeoffDone;
		private bool m_launched;
		private Action m_onTakeoffDone;

		/// <summary>The stage of the loaded game scene, or null (the menu scene opened on its own).</summary>
		public static MenuStage Instance { get; private set; }

		/// <summary>The stage is what the camera is showing: the menu is up or a takeoff is running.</summary>
		public bool IsShowing { get; private set; }

		/// <summary>True once after a takeoff, for the HUD to drop in.</summary>
		public static bool ConsumeHudEntrance()
		{
			var play = m_s_hudEntrance;
			m_s_hudEntrance = false;
			return play;
		}

		private void Awake()
		{
			Instance = this;
			m_s_hudEntrance = false;
			m_content.SetActive(false);
		}

		// GameManager sets its first state in Awake, before this can listen, so Start reads the state as well as subscribing.
		private void Start()
		{
			m_gameManager = GameManager.TryGetInstance();
			m_plane = FindAnyObjectByType<PlaneController>();
			m_camera = Camera.main;
			m_follow = FindAnyObjectByType<CinemachineFollow>();
			if (m_gameManager == null || m_plane == null || m_camera == null || m_follow == null) return;

			m_followCamera = m_follow.GetComponent<CinemachineCamera>();
			m_brain = m_camera.GetComponent<CinemachineBrain>();
			var visual = m_plane.transform.Find("Visual");
			m_planeVisual = visual != null ? visual.gameObject : null;

			m_gameManager.ReturnedToMenu += Show;
			m_gameManager.RunStarted += Hide;
			PlaneSelection.Changed += RefreshModel;

			if (!m_gameManager.IsPlaying) Show();
		}

		private void OnDestroy()
		{
			if (Instance == this) Instance = null;
			PlaneSelection.Changed -= RefreshModel;
			if (m_gameManager == null) return;

			m_gameManager.ReturnedToMenu -= Show;
			m_gameManager.RunStarted -= Hide;
		}

		/// <summary>
		/// Plays the takeoff and calls <paramref name="onDone"/> once, when the stand-in has reached the start position. The
		/// stage holds that last frame until the run starts.
		/// </summary>
		public void PlayTakeoff(Action onDone)
		{
			if (!IsShowing || m_takingOff)
			{
				onDone?.Invoke();
				return;
			}

			m_takingOff = true;
			m_takeoffDone = false;
			m_launched = false;
			m_time = 0f;
			m_onTakeoffDone = onDone;
			if (m_idleDust != null) m_idleDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			Sfx.Play(SfxId.TakeoffWindUp);
		}

		/// <summary>Jumps to the end of a running takeoff.</summary>
		public void Skip()
		{
			if (m_takingOff && !m_takeoffDone) m_time = m_duration;
		}

		private void Show()
		{
			if (IsShowing) return;

			IsShowing = true;
			m_takingOff = false;
			m_takeoffDone = false;
			m_content.SetActive(true);
			RefreshModel();
			if (m_planeVisual != null) m_planeVisual.SetActive(false);
			if (m_brain != null) m_brain.enabled = false;
			if (m_idleDust != null) m_idleDust.Play(true);
			Apply();
		}

		private void Hide()
		{
			if (!IsShowing) return;

			IsShowing = false;
			m_takingOff = false;
			m_content.SetActive(false);
			if (m_planeVisual != null) m_planeVisual.SetActive(true);
			if (m_brain != null) m_brain.enabled = true;
		}

		private void RefreshModel()
		{
			if (m_model != null) Destroy(m_model);
			m_model = PlaneVisualHost.Spawn(PlaneSelection.Equipped, m_planeAnchor);
		}

		private void LateUpdate()
		{
			if (!IsShowing) return;

			// A long frame (the first after a tab switch) must not swallow the animation.
			if (m_takingOff && !m_takeoffDone) m_time = Mathf.Min(m_duration, m_time + Mathf.Min(Time.unscaledDeltaTime, k_MaxStepSeconds));

			Apply();

			if (!m_takingOff || m_takeoffDone || m_time < m_duration) return;

			m_takeoffDone = true;
			m_s_hudEntrance = true;
			var done = m_onTakeoffDone;
			m_onTakeoffDone = null;
			done?.Invoke();
		}

		private void Apply()
		{
			var parked = m_parkedPoint.position;
			Vector3 position;
			var scale = Vector3.one;
			var pitch = 0f;

			var idle = m_takingOff ? 1f - Mathf.Clamp01(m_time / 0.25f) : 1f;
			var cycle = Time.unscaledTime * m_bobCyclesPerSecond * Mathf.PI * 2f;
			var bob = new Vector3(0f, Mathf.Sin(cycle) * m_bobHeight * idle, 0f);
			var roll = Mathf.Sin(cycle * 0.5f) * m_rollDegrees * idle;

			var pulled = parked + Vector3.back * m_pullBackDistance;

			if (!m_takingOff)
			{
				position = parked;
			}
			else if (m_time < m_launchStart)
			{
				// Wind back like a pull-back toy, then squash while held.
				var wind = EaseOut(m_time / m_windUpEnd);
				position = Vector3.Lerp(parked, pulled, wind);
				pitch = k_WindUpPitch * wind;
				scale = Vector3.Lerp(Vector3.one, m_squashScale, Smooth((m_time - m_windUpEnd * 0.6f) / (m_launchStart - m_windUpEnd * 0.6f)));
			}
			else
			{
				if (!m_launched)
				{
					m_launched = true;
					Sfx.Play(SfxId.TakeoffLaunch);
					if (m_launchDust != null)
					{
						m_launchDust.transform.position = pulled;
						m_launchDust.Play(true);
					}
				}

				var length = m_duration - m_launchStart;
				var u = Mathf.Clamp01((m_time - m_launchStart) / length);
				var start = m_plane.transform.position;

				// Hermite from rest to the start position, arriving at flight speed so the run picks up without a hitch.
				var u2 = u * u;
				var u3 = u2 * u;
				var travelled = (-2f * u3 + 3f * u2) * (start.z - pulled.z) + (u3 - u2) * (m_plane.ForwardSpeed * length);

				position = new Vector3(
					Mathf.Lerp(pulled.x, start.x, Smooth(u)),
					Mathf.Lerp(pulled.y, start.y, Smooth(u)) + m_hopHeight * Mathf.Sin(Mathf.PI * u) * (1f - u),
					pulled.z + travelled);
				pitch = k_WindUpPitch * (1f - Smooth(u * 1.5f));
				scale = u < 0.15f
					? Vector3.Lerp(m_squashScale, m_stretchScale, u / 0.15f)
					: Vector3.Lerp(m_stretchScale, Vector3.one, Smooth((u - 0.15f) / 0.5f));
			}

			m_planeAnchor.SetPositionAndRotation(position + bob, Quaternion.Euler(pitch, 0f, roll));
			m_planeAnchor.localScale = scale;

			// Like the plane's bob, the camera noise is gone a moment into a takeoff.
			ApplyCamera(parked, position, idle);
		}

		// The camera stands still at the menu view until the swing, then moves to the follow camera's place behind the plane.
		// All along it turns to keep the plane in frame, from the wind-back until the player has control.
		private void ApplyCamera(Vector3 parked, Vector3 plane, float handheld)
		{
			var menuPosition = parked + m_cameraOffset;

			// The swing has to be over when the takeoff is, or the camera would jump when the follow camera takes over.
			var swingEnd = Mathf.Min(m_cameraSwingEnd, m_duration);
			var swing = m_takingOff ? Smooth((m_time - m_cameraSwingStart) / Mathf.Max(0.01f, swingEnd - m_cameraSwingStart)) : 0f;
			var chasePosition = plane + m_follow.FollowOffset;
			var lift = Vector3.up * (m_cameraLift * Mathf.Sin(Mathf.PI * swing));
			var position = Vector3.Lerp(menuPosition, chasePosition, swing) + lift;

			var aim = plane + Vector3.Lerp(m_cameraLookOffset, FollowAimOffset(), swing);

			var rotation = Quaternion.LookRotation(aim - position);

			var strength = handheld * Mathf.Clamp01(SettingsService.ScreenShake / 100f);
			if (strength > 0f)
			{
				var time = Time.unscaledTime * m_noiseSpeed;
				position += rotation * (new Vector3(Noise(time, 1.3f), Noise(time, 11.7f), 0f) * (m_noiseDistance * strength));
				rotation *= Quaternion.Euler(Vector3.Scale(new Vector3(Noise(time, 23.1f), Noise(time, 37.9f), Noise(time, 51.4f)), m_noiseDegrees) * strength);
			}

			m_camera.transform.SetPositionAndRotation(position, rotation);
			m_camera.fieldOfView = Mathf.Lerp(m_cameraFov, m_followCamera.Lens.FieldOfView, swing);
		}

		// The point, relative to the plane and level with it along Z, that the follow camera's fixed rotation looks through.
		// Aiming there from the follow camera's place gives exactly that rotation.
		private Vector3 FollowAimOffset()
		{
			var offset = m_follow.FollowOffset;
			var forward = m_followCamera.transform.forward;
			return offset + forward * (-offset.z / Mathf.Max(0.01f, forward.z));
		}

		// Smooth random drift around zero, roughly -1 to 1: a slow wave with a quicker, smaller one on top. Each channel is its own row.
		private static float Noise(float time, float row)
		{
			var slow = Mathf.PerlinNoise(time, row) - 0.5f;
			var quick = Mathf.PerlinNoise(time * 2.7f, row + 5.5f) - 0.5f;
			return (slow + quick * 0.35f) * 2f;
		}

		private static float Smooth(float t)
		{
			t = Mathf.Clamp01(t);
			return t * t * (3f - 2f * t);
		}

		private static float EaseOut(float t)
		{
			t = 1f - Mathf.Clamp01(t);
			return 1f - t * t * t;
		}
	}
}
