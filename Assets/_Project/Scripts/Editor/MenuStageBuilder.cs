using System.Collections.Generic;
using System.Linq;
using Opoint8182.Menu3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Opoint8182.WorldEditor
{
	/// <summary>
	/// Builds <c>Prefabs/World/MenuStage.prefab</c> (3D art pass, Pass 7): the rooftop the main menu shows the plane on, made
	/// from pieces of the city pack (a flat-roofed shop turned lengthways, and traffic cones), with the parked point, the
	/// plane anchor and two small dust effects. The prefab is placed once in the game scene. Rebuilding replaces the prefab,
	/// so tune the numbers here, or tune the <see cref="MenuStage"/> fields on the scene instance.
	/// </summary>
	public static class MenuStageBuilder
	{
		private const string k_CityPath = "Assets/_Project/Prefabs/Misc/City.prefab";
		private const string k_DustSourcePath = "Assets/_Project/Prefabs/VFX/CFXR Explosion 1 Variant.prefab";
		private const string k_PrefabPath = "Assets/_Project/Prefabs/World/MenuStage.prefab";
		private const string k_RoofPiece = "Building_Coffee Shop";
		private const string k_ConePiece = "Props_Traffic cone";

		// Far enough behind the run's start camera (z -58) that the roof is never in view once flying.
		private const float k_RoofCentreZ = -80f;
		// From the roof surface to the plane's pivot: the equipped model hangs about this far below it.
		private const float k_PlanePivotHeight = 0.65f;

		[MenuItem("Tools/World/Build Menu Stage")]
		private static void BuildMenu() => Debug.Log(Build());

		/// <summary>Creates or replaces the prefab and returns a summary (also used from the Unity CLI).</summary>
		public static string Build()
		{
			var city = AssetDatabase.LoadAssetAtPath<GameObject>(k_CityPath);
			var filters = city.GetComponentsInChildren<MeshFilter>(true);
			var roofSource = filters.First(f => f.sharedMesh != null && f.sharedMesh.name == k_RoofPiece);
			var coneSource = filters.First(f => f.sharedMesh != null && f.sharedMesh.name == k_ConePiece);

			var root = new GameObject("MenuStage");
			try
			{
				var stage = root.AddComponent<MenuStage>();
				var content = Child(root.transform, "Content");

				var roof = CopyPiece(roofSource, content, "Rooftop", Quaternion.Euler(0f, 90f, 0f));
				var bounds = roof.GetComponent<Renderer>().bounds;
				roof.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, k_RoofCentreZ - bounds.center.z);
				bounds = roof.GetComponent<Renderer>().bounds;
				var floor = RoofFloorHeight(roof);

				var coneSpots = new[]
				{
					new Vector2(-4.6f, 6.5f), new Vector2(4.6f, 6.5f), new Vector2(4.6f, 1f), new Vector2(4.6f, -4.5f),
				};
				foreach (var spot in coneSpots)
				{
					var cone = CopyPiece(coneSource, content, "Cone", coneSource.transform.rotation);
					var coneBounds = cone.GetComponent<Renderer>().bounds;
					cone.transform.position += new Vector3(spot.x - coneBounds.center.x, floor - coneBounds.min.y, k_RoofCentreZ + spot.y - coneBounds.center.z);
				}

				var parked = Child(content, "ParkedPoint");
				parked.position = new Vector3(0f, floor + k_PlanePivotHeight, k_RoofCentreZ);
				var anchor = Child(content, "PlaneAnchor");
				anchor.position = parked.position;

				var idleDust = Dust(content, "IdleDust", new Vector3(0f, floor + 0.2f, k_RoofCentreZ - 2.6f), true);
				var launchDust = Dust(content, "LaunchDust", new Vector3(0f, floor + 0.2f, k_RoofCentreZ - 2.2f), false);

				var serialized = new SerializedObject(stage);
				serialized.FindProperty("m_content").objectReferenceValue = content.gameObject;
				serialized.FindProperty("m_parkedPoint").objectReferenceValue = parked;
				serialized.FindProperty("m_planeAnchor").objectReferenceValue = anchor;
				serialized.FindProperty("m_idleDust").objectReferenceValue = idleDust;
				serialized.FindProperty("m_launchDust").objectReferenceValue = launchDust;
				serialized.ApplyModifiedPropertiesWithoutUndo();

				PrefabUtility.SaveAsPrefabAsset(root, k_PrefabPath);
				var triangles = content.GetComponentsInChildren<MeshFilter>().Sum(f => (long)f.sharedMesh.GetIndexCount(0) / 3);
				return $"[MenuStage] built: roof {bounds.size}, floor {floor:0.00}, {content.GetComponentsInChildren<MeshRenderer>().Length} renderers, {triangles} triangles";
			}
			finally
			{
				Object.DestroyImmediate(root);
			}
		}

		private static Transform Child(Transform parent, string name)
		{
			var child = new GameObject(name).transform;
			child.SetParent(parent, false);
			return child;
		}

		// Mesh and materials only: no pack colliders on the visuals.
		private static GameObject CopyPiece(MeshFilter source, Transform parent, string name, Quaternion rotation)
		{
			var piece = Child(parent, name).gameObject;
			piece.transform.rotation = rotation;
			piece.transform.localScale = source.transform.lossyScale;
			piece.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
			piece.AddComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
			return piece;
		}

		// The roof surface is the height, below the parapet, that carries the most upward-facing area.
		private static float RoofFloorHeight(GameObject roof)
		{
			var mesh = roof.GetComponent<MeshFilter>().sharedMesh;
			var vertices = mesh.vertices.Select(v => roof.transform.TransformPoint(v)).ToArray();
			var areas = new Dictionary<int, float>();
			var triangles = mesh.triangles;
			for (var i = 0; i < triangles.Length; i += 3)
			{
				var a = vertices[triangles[i]];
				var cross = Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a);
				if (cross.normalized.y < 0.99f || a.y < 3f) continue;

				var key = Mathf.RoundToInt(a.y * 100f);
				areas.TryGetValue(key, out var area);
				areas[key] = area + cross.magnitude * 0.5f;
			}

			return areas.OrderByDescending(p => p.Value).First().Key / 100f;
		}

		// A few small puffs of the crash burst's smoke: a lazy trickle while parked, one puff at the launch.
		private static ParticleSystem Dust(Transform parent, string name, Vector3 position, bool idle)
		{
			var source = AssetDatabase.LoadAssetAtPath<GameObject>(k_DustSourcePath).transform.Find("Smoke").gameObject;
			var dust = Object.Instantiate(source, parent);
			dust.name = name;
			dust.transform.SetPositionAndRotation(position, Quaternion.Euler(-90f, 0f, 0f));
			dust.transform.localScale = Vector3.one;

			var particles = dust.GetComponent<ParticleSystem>();
			var main = particles.main;
			main.playOnAwake = false;
			main.loop = idle;
			main.duration = idle ? 2f : 0.3f;
			main.startDelay = 0f;
			main.useUnscaledTime = true;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.startLifetime = new ParticleSystem.MinMaxCurve(idle ? 0.7f : 0.4f, idle ? 1.2f : 0.7f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(idle ? 0.3f : 3f, idle ? 0.8f : 6f);
			main.startSize = new ParticleSystem.MinMaxCurve(idle ? 0.35f : 0.8f, idle ? 0.7f : 1.6f);
			main.maxParticles = idle ? 8 : 16;

			// The source smoke starts as fire: make it plain dust that fades out. Its custom data stays, the shader's dissolve reads it.
			main.startColor = new Color(0.86f, 0.82f, 0.74f, idle ? 0.55f : 0.8f);
			var fade = new Gradient();
			fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
				new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
			var colour = particles.colorOverLifetime;
			colour.enabled = true;
			colour.color = fade;

			var emission = particles.emission;
			emission.rateOverTime = idle ? 3f : 0f;
			emission.SetBursts(idle ? new ParticleSystem.Burst[0] : new[] { new ParticleSystem.Burst(0f, 14) });

			var shape = particles.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = idle ? 0.5f : 0.8f;

			var renderer = dust.GetComponent<ParticleSystemRenderer>();
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			return particles;
		}
	}
}
