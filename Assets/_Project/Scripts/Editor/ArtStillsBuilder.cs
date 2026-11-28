using System.IO;
using System.Linq;
using Opoint8182.Common;
using Opoint8182.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Opoint8182.ArtEditor
{
	/// <summary>
	/// Renders the still images the UI uses (3D art pass, Pass 8) into <c>Art/Textures/Stills</c>: a picture of every plane
	/// in the catalog (assigned as its thumbnail), the crash effect, and the loading screen's key art. Run
	/// <c>Tools > Art > Render Art Stills</c> again after a plane, building or effect changes. Nothing in any scene is
	/// saved: the key art is staged in the game scene, opened for its sky, sun and fog, and closed again.
	/// </summary>
	public static class ArtStillsBuilder
	{
		private const string k_Folder = "Assets/_Project/Art/Textures/Stills";
		private const string k_GameScenePath = "Assets/_Project/Scenes/Prototype.unity";
		private const string k_CrashFxPath = "Assets/_Project/Prefabs/VFX/CFXR Explosion 1 Variant.prefab";
		private const string k_GroundTilePath = "Assets/_Project/Prefabs/World/GroundTile.prefab";
		private const string k_BuildingFolder = "Assets/_Project/Prefabs/Buildings/";
		private const float k_GroundPitch = 56.6f;

		private const int k_PlaneWidth = 640;
		private const int k_PlaneHeight = 400;
		private const int k_FxSize = 256;
		// Multiples of four, so the texture compresses.
		private const int k_KeyArtWidth = 1600;
		private const int k_KeyArtHeight = 624;

		[MenuItem("Tools/Art/Render Art Stills")]
		private static void RenderMenu() => Debug.Log(RenderAll());

		public static string RenderAll()
		{
			if (EditorApplication.isPlaying) return "[ArtStills] stop Play mode first";

			Directory.CreateDirectory(k_Folder);
			return $"[ArtStills] {RenderPlanes()}; {RenderCrashFx()}; {RenderKeyArt()}";
		}

		public static string RenderPlanes()
		{
			var catalog = AssetDatabase.LoadAssetAtPath<PlaneCatalog>("Assets/_Project/Resources/PlaneCatalog.asset");
			var count = 0;
			foreach (var plane in catalog.Planes)
			{
				if (plane == null || plane.VisualPrefab == null) continue;

				var scene = EditorSceneManager.NewPreviewScene();
				try
				{
					var anchor = NewIn(scene, "anchor");
					PlaneVisualHost.Spawn(plane, anchor.transform);
					AddSun(scene);

					var camera = PreviewCamera(scene, new Color(0f, 0f, 0f, 0f), 20f);
					Frame(camera, anchor, new Vector3(-0.75f, 0.42f, 1f), k_PlaneWidth / (float)k_PlaneHeight, 1.04f);

					var path = $"{k_Folder}/Tex_Plane_{plane.Id}.png";
					Save(camera, k_PlaneWidth, k_PlaneHeight, true, path);
					Import(path, importer =>
					{
						importer.textureType = TextureImporterType.Sprite;
						importer.spriteImportMode = SpriteImportMode.Single;
						importer.alphaIsTransparency = true;
						importer.maxTextureSize = 1024;
					});

					var serialized = new SerializedObject(plane);
					serialized.FindProperty("m_thumbnail").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
					serialized.ApplyModifiedPropertiesWithoutUndo();
					AssetDatabase.SaveAssetIfDirty(plane);
					count++;
				}
				finally
				{
					EditorSceneManager.ClosePreviewScene(scene);
				}
			}

			return $"{count} plane stills";
		}

		public static string RenderCrashFx()
		{
			var scene = EditorSceneManager.NewPreviewScene();
			try
			{
				var effect = NewIn(scene, "fx");
				var burst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(k_CrashFxPath), scene);
				burst.transform.SetParent(effect.transform);
				burst.transform.localPosition = Vector3.zero;
				Simulate(burst, 0.2f);

				var camera = PreviewCamera(scene, new Color(0.20f, 0.29f, 0.40f, 1f), 30f);
				camera.transform.position = new Vector3(0f, 0f, -15f);
				camera.transform.rotation = Quaternion.identity;

				var path = $"{k_Folder}/Tex_CrashFx_ClassicBoom.png";
				Save(camera, k_FxSize, k_FxSize, false, path);
				Import(path, importer => importer.maxTextureSize = 256);
				return "crash effect still";
			}
			finally
			{
				EditorSceneManager.ClosePreviewScene(scene);
			}
		}

		public static string RenderKeyArt()
		{
			var game = SceneManager.GetSceneByPath(k_GameScenePath);
			if (game.isLoaded) return "key art SKIPPED: close the game scene first (it is staged in it and closed without saving)";

			var previous = SceneManager.GetActiveScene();
			game = EditorSceneManager.OpenScene(k_GameScenePath, OpenSceneMode.Additive);
			try
			{
				SceneManager.SetActiveScene(game);
				foreach (var root in game.GetRootGameObjects().Where(r => r.name == "MenuStage")) root.SetActive(false);
				foreach (var camera in game.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>())) camera.enabled = false;

				var tile = AssetDatabase.LoadAssetAtPath<GameObject>(k_GroundTilePath);
				for (var k = -2; k <= 5; k++)
				{
					var ground = (GameObject)PrefabUtility.InstantiatePrefab(tile, game);
					ground.transform.position = new Vector3(0f, 0f, k * k_GroundPitch);
					var view = ground.GetComponent<GroundTileView>();
					if (view != null) view.SetDressing(Mathf.Abs(k) % 3);
				}

				// The building being hit, then a skyline behind it.
				var target = Building(game, "Building_Normal", new Vector3(5.5f, 0f, 9f), 20f);
				Building(game, "Building_Tough", new Vector3(-9f, 0f, 38f), -12f);
				Building(game, "Building_Heal", new Vector3(12f, 0f, 52f), 8f);
				Building(game, "Building_Normal", new Vector3(-17f, 0f, 70f), 30f);
				Building(game, "Building_Normal", new Vector3(24f, 0f, 30f), -25f);
				Building(game, "Building_Tough", new Vector3(3f, 0f, 88f), 0f);

				// The plane goes nose first into the weak point.
				var hit = target.GetComponent<ICrashSource>().WorldPosition;
				var heading = Quaternion.Euler(-4f, 46f, -24f);
				var forward = heading * Vector3.forward;
				const float planeScale = 1.9f;

				var holder = new GameObject("KeyArtPlane");
				PlaneVisualHost.Spawn(AssetDatabase.LoadAssetAtPath<PlaneCatalog>("Assets/_Project/Resources/PlaneCatalog.asset").Default, holder.transform);
				holder.transform.SetPositionAndRotation(hit - forward * (2.3f * planeScale), heading);
				holder.transform.localScale = Vector3.one * planeScale;

				var effect = AssetDatabase.LoadAssetAtPath<GameObject>(k_CrashFxPath);
				var burst = (GameObject)PrefabUtility.InstantiatePrefab(effect, game);
				burst.transform.position = hit - forward * 0.4f + Vector3.back * 0.6f;
				burst.transform.localScale = Vector3.one * 1.35f;
				Simulate(burst, 0.19f);

				var cameraObject = new GameObject("KeyArtCamera");
				var keyCamera = cameraObject.AddComponent<Camera>();
				keyCamera.clearFlags = CameraClearFlags.Skybox;
				keyCamera.fieldOfView = 27f;
				keyCamera.nearClipPlane = 0.3f;
				keyCamera.farClipPlane = 600f;
				cameraObject.transform.position = hit + new Vector3(-17f, 2.5f, -19f);
				cameraObject.transform.LookAt(hit - forward * 3f + Vector3.up * 0.4f);

				var path = $"{k_Folder}/Tex_KeyArt.png";
				Save(keyCamera, k_KeyArtWidth, k_KeyArtHeight, false, path);
				Import(path, importer =>
				{
					importer.maxTextureSize = 2048;
					importer.wrapMode = TextureWrapMode.Clamp;
					importer.textureCompression = TextureImporterCompression.Compressed;
				});
				return "key art";
			}
			finally
			{
				SceneManager.SetActiveScene(previous);
				EditorSceneManager.CloseScene(game, true);
			}
		}

		// A building prefab as it looks in a run: its tint is applied at run time, so it is put on here as an override.
		private static GameObject Building(Scene scene, string name, Vector3 position, float yaw)
		{
			var building = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(k_BuildingFolder + name + ".prefab"), scene);
			building.transform.position = position;
			building.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

			foreach (var tint in building.GetComponentsInChildren<Opoint8182.Building.BuildingTint>())
			{
				var serialized = new SerializedObject(tint);
				var renderer = (Renderer)serialized.FindProperty("m_renderer").objectReferenceValue;
				var index = serialized.FindProperty("m_materialIndex").intValue;
				var block = new MaterialPropertyBlock();
				block.SetColor("_BaseColor", serialized.FindProperty("m_color").colorValue);
				renderer.SetPropertyBlock(block, index);
			}

			return building;
		}

		private static void Simulate(GameObject effect, float seconds)
		{
			var systems = effect.GetComponentsInChildren<ParticleSystem>();
			foreach (var system in systems)
			{
				system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
				system.useAutoRandomSeed = false;
				system.randomSeed = 8182;
			}

			effect.GetComponent<ParticleSystem>().Simulate(seconds, true, true);
		}

		private static GameObject NewIn(Scene scene, string name)
		{
			var created = new GameObject(name);
			SceneManager.MoveGameObjectToScene(created, scene);
			return created;
		}

		private static void AddSun(Scene scene)
		{
			var sun = NewIn(scene, "sun").AddComponent<Light>();
			sun.type = LightType.Directional;
			sun.intensity = 1.15f;
			sun.color = new Color(1f, 0.96f, 0.88f);
			sun.transform.rotation = Quaternion.Euler(45f, 150f, 0f);

			var fill = NewIn(scene, "fill").AddComponent<Light>();
			fill.type = LightType.Directional;
			fill.intensity = 0.45f;
			fill.color = new Color(0.75f, 0.85f, 1f);
			fill.transform.rotation = Quaternion.Euler(20f, -40f, 0f);
		}

		private static Camera PreviewCamera(Scene scene, Color background, float fieldOfView)
		{
			var camera = NewIn(scene, "camera").AddComponent<Camera>();
			camera.scene = scene;
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = background;
			camera.fieldOfView = fieldOfView;
			camera.nearClipPlane = 0.1f;
			camera.farClipPlane = 200f;
			camera.allowHDR = false;
			return camera;
		}

		// Looks at the object from `direction`, as close as keeps its whole bounding box in the picture.
		private static void Frame(Camera camera, GameObject target, Vector3 direction, float aspect, float margin)
		{
			var renderers = target.GetComponentsInChildren<Renderer>();
			var bounds = renderers[0].bounds;
			foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);

			var forward = -direction.normalized;
			var rotation = Quaternion.LookRotation(forward);
			var tangentY = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
			var tangentX = tangentY * aspect;
			var inverse = Quaternion.Inverse(rotation);
			var distance = 0f;
			for (var i = 0; i < 8; i++)
			{
				var corner = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
				var local = inverse * Vector3.Scale(bounds.extents, corner);
				distance = Mathf.Max(distance, Mathf.Abs(local.x) / tangentX - local.z, Mathf.Abs(local.y) / tangentY - local.z);
			}

			camera.transform.SetPositionAndRotation(bounds.center - forward * (distance * margin), rotation);
		}

		private static void Save(Camera camera, int width, int height, bool transparent, string path)
		{
			var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
			var image = new Texture2D(width, height, transparent ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
			var previous = RenderTexture.active;
			try
			{
				camera.targetTexture = target;
				camera.aspect = width / (float)height;
				camera.Render();
				RenderTexture.active = target;
				image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
				image.Apply();
				File.WriteAllBytes(path, image.EncodeToPNG());
			}
			finally
			{
				RenderTexture.active = previous;
				camera.targetTexture = null;
				Object.DestroyImmediate(image);
				Object.DestroyImmediate(target);
			}
		}

		private static void Import(string path, System.Action<TextureImporter> configure)
		{
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
			var importer = (TextureImporter)AssetImporter.GetAtPath(path);
			importer.mipmapEnabled = false;
			importer.npotScale = TextureImporterNPOTScale.None;
			configure(importer);
			importer.SaveAndReimport();
		}
	}
}
