using UnityEngine;

namespace Opoint8182.UI
{
	/// <summary>
	/// The Profile's live portrait (3D art pass, Pass 8): the equipped plane turning slowly in front of its own camera, drawn
	/// into a transparent render texture that the screen shows. It sits far below the world, where the game camera never
	/// looks, and is lit by whatever sun the loaded game scene has. It exists only while the Profile screen is open:
	/// <see cref="Create"/> on open, <see cref="Release"/> on close.
	/// </summary>
	public class PlanePortraitRig : MonoBehaviour
	{
		// Twice the slot's size in reference pixels, same shape.
		private const int k_Width = 840;
		private const int k_Height = 600;
		private const float k_FieldOfView = 24f;
		private const float k_CameraPitch = 20f;
		private const float k_DegreesPerSecond = 24f;
		private const float k_StartYaw = 215f;
		private const float k_Margin = 0.6f;
		private static readonly Vector3 m_S_Position = new(0f, -500f, 0f);

		private Camera m_camera;
		private Transform m_turntable;
		private GameObject m_model;

		/// <summary>The picture of the turning plane.</summary>
		public RenderTexture Texture { get; private set; }

		/// <summary>A running rig, or null when there is no plane to show.</summary>
		public static PlanePortraitRig Create()
		{
			if (PlaneSelection.Equipped == null) return null;

			var rig = new GameObject("PlanePortraitRig").AddComponent<PlanePortraitRig>();
			rig.Build();
			return rig;
		}

		public void Release()
		{
			Destroy(gameObject);
		}

		private void Build()
		{
			transform.position = m_S_Position;
			m_turntable = new GameObject("Turntable").transform;
			m_turntable.SetParent(transform, false);
			m_turntable.localRotation = Quaternion.Euler(0f, k_StartYaw, 0f);

			Texture = new RenderTexture(k_Width, k_Height, 24, RenderTextureFormat.ARGB32) { name = "PlanePortrait", antiAliasing = 4 };

			m_camera = new GameObject("Camera").AddComponent<Camera>();
			m_camera.transform.SetParent(transform, false);
			m_camera.clearFlags = CameraClearFlags.SolidColor;
			m_camera.backgroundColor = Color.clear;
			m_camera.fieldOfView = k_FieldOfView;
			m_camera.nearClipPlane = 0.3f;
			m_camera.farClipPlane = 60f;
			m_camera.allowHDR = false;
			m_camera.targetTexture = Texture;

			Spawn();
			PlaneSelection.Changed += Spawn;
		}

		private void Spawn()
		{
			if (m_model != null)
			{
				m_model.SetActive(false);
				Destroy(m_model);
			}

			m_model = PlaneVisualHost.Spawn(PlaneSelection.Equipped, m_turntable);
			if (m_model != null) Frame();
		}

		// Far enough that the plane fits at every angle of the turn: its widest reach from the axis, not just its width now.
		private void Frame()
		{
			var yaw = m_turntable.localRotation;
			m_turntable.localRotation = Quaternion.identity;
			var renderers = m_model.GetComponentsInChildren<Renderer>();
			var bounds = renderers[0].bounds;
			foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
			m_turntable.localRotation = yaw;

			var centre = bounds.center - m_turntable.position;
			var reach = new Vector2(Mathf.Abs(centre.x) + bounds.extents.x, Mathf.Abs(centre.z) + bounds.extents.z).magnitude;

			var tangent = Mathf.Tan(k_FieldOfView * 0.5f * Mathf.Deg2Rad);
			// The reach comes toward the camera as it turns, hence the extra distance on top of the width fit.
			var distance = (reach / (tangent * k_Width / k_Height) + reach * 0.1f) * k_Margin;
			var rotation = Quaternion.Euler(k_CameraPitch, 0f, 0f);
			m_camera.transform.localPosition = new Vector3(0f, centre.y, 0f) - rotation * Vector3.forward * distance;
			m_camera.transform.localRotation = rotation;
		}

		private void Update()
		{
			m_turntable.Rotate(0f, k_DegreesPerSecond * Time.unscaledDeltaTime, 0f, Space.Self);
		}

		private void OnDestroy()
		{
			PlaneSelection.Changed -= Spawn;
			if (m_camera != null) m_camera.targetTexture = null;
			if (Texture == null) return;

			Texture.Release();
			Destroy(Texture);
		}
	}
}
