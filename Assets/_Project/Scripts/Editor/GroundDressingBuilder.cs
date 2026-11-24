using System.Collections.Generic;
using System.IO;
using System.Linq;
using Opoint8182.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Opoint8182.WorldEditor
{
	/// <summary>
	/// Bakes the side dressing of the ground tile: a few low buildings, trees, bushes and street lights from the SimplePoly
	/// City pack, scattered on the tile's grey lots that lie outside the flight lane, merged into one mesh per variant.
	/// The pack gives every piece its own 512 px texture, so the pieces' textures are packed into one 512 px atlas
	/// (128 px per piece) and every variant is a single mesh with a single material. Output (in <c>Prefabs/World</c>):
	/// the atlas, <c>GroundDressing_Mat</c>, one mesh per variant, and the variants added to <c>GroundTile.prefab</c> as
	/// inactive children next to a <see cref="GroundTileView"/>. Seeded: a rebuild gives the same result.
	/// </summary>
	public static class GroundDressingBuilder
	{
		private const string k_PackFolder = "Assets/_Project/Art/Models/World";
		private const string k_GroundSourcePath = "Assets/_Project/Prefabs/Misc/Ground.prefab";
		private const string k_TilePath = "Assets/_Project/Prefabs/World/GroundTile.prefab";
		private const string k_OutputFolder = "Assets/_Project/Prefabs/World";
		private const string k_AtlasPath = k_OutputFolder + "/GroundDressing_Atlas.png";
		private const string k_MaterialPath = k_OutputFolder + "/GroundDressing_Mat.mat";

		private const int k_VariantCount = 3;
		private const int k_GridSize = 4;
		private const int k_CellSize = 128;
		private const float k_CellMargin = 2f / k_CellSize;

		// Nothing decorative closer to the centre line than this: the plane's hard lateral bound is 16 and buildings spawn
		// within +/-10 and are about 7 wide (see Docs/technical-design/world-environment.md).
		private const float k_LaneHalfWidth = 24f;
		private const float k_LotHalf = 10f;
		private const float k_MaxBuildingHeight = 12f;
		private const float k_LotFitWidth = 19f;

		private static readonly string[] m_S_Buildings =
		{
			"Building_Bakery", "Building_Fast Food", "Building_Coffee Shop", "Building_Gift Shop", "Building_Pizza",
			"Building_Chicken Shop", "Building_Books Shop", "Building_Music Store", "Building_Super Market", "Building_House_01_color01",
		};

		private static readonly string[] m_S_Greenery =
		{
			"Natures_Big Tree", "Natures_Fir Tree", "Natures_Cube Tree", "Natures_Bush_01", "Natures_Rock_Big", "Props_Street Light",
		};

		private class Piece
		{
			public string name;
			public bool isBuilding;
			public Mesh mesh;
			public Matrix4x4 local;
			public Bounds bounds;
			public int triangles;
		}

		private struct Placed
		{
			public Piece piece;
			public Vector3 position;
			public float yaw;
			public Rect footprint;
		}

		[MenuItem("Tools/World/Build Ground Dressing")]
		private static void BuildMenu() => Debug.Log(Build());

		public static string Build()
		{
			var tile = AssetDatabase.LoadAssetAtPath<GameObject>(k_TilePath);
			if (tile == null) return $"[World] {k_TilePath} not found: run Build Ground Tile first.";

			var palette = m_S_Buildings.Concat(m_S_Greenery).ToArray();
			if (palette.Length > k_GridSize * k_GridSize) return $"[World] Palette has {palette.Length} pieces, the atlas holds {k_GridSize * k_GridSize}.";

			// 1. The pieces and the atlas.
			var sources = new List<GameObject>();
			foreach (var pieceName in palette)
			{
				var source = FindPackPrefab(pieceName);
				if (source == null) return $"[World] Pack prefab '{pieceName}' not found under {k_PackFolder}.";
				sources.Add(source);
			}

			var atlas = BuildAtlas(sources);
			var pieces = new List<Piece>();
			for (var i = 0; i < sources.Count; i++)
			{
				var piece = MakePiece(sources[i], i, i < m_S_Buildings.Length);
				if (piece == null) return $"[World] '{sources[i].name}' needs exactly one mesh with one material.";
				if (piece.isBuilding && piece.bounds.size.y > k_MaxBuildingHeight) return $"[World] '{piece.name}' is {piece.bounds.size.y:0.0} high, over {k_MaxBuildingHeight}.";
				pieces.Add(piece);
			}

			var material = MakeMaterial(sources[0], atlas);

			// 2. The lots outside the lane.
			var lots = ReadLots();
			if (lots.Count == 0) return "[World] No lots outside the lane found in Ground.prefab.";

			// 3. The variants.
			var meshes = new List<Mesh>();
			var report = new List<string>();
			for (var v = 0; v < k_VariantCount; v++)
			{
				var random = new System.Random(7919 * (v + 1));
				var placed = new List<Placed>();
				foreach (var lotX in lots) PlaceOnLot(lotX, pieces, random, placed);

				var mesh = BakeVariant(v, placed);
				meshes.Add(mesh);
				var buildings = placed.Count(p => p.piece.isBuilding);
				report.Add($"v{v}: {placed.Count} pieces ({buildings} buildings), {mesh.vertexCount} vertices, {mesh.GetIndexCount(0) / 3} triangles");
			}

			// 4. The prefab.
			AddVariantsToTile(meshes, material);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			return $"[World] Built the ground dressing: {lots.Count} lots outside the lane (|x| {k_LaneHalfWidth}+), {pieces.Count} pieces, atlas {k_GridSize * k_CellSize} px.\n  " + string.Join("\n  ", report);
		}

		private static GameObject FindPackPrefab(string pieceName)
		{
			foreach (var guid in AssetDatabase.FindAssets($"{pieceName} t:Prefab", new[] { k_PackFolder }))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (Path.GetFileNameWithoutExtension(path) == pieceName && path.Contains("/Prefab/")) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
			}

			return null;
		}

		private static Texture2D BuildAtlas(List<GameObject> sources)
		{
			var size = k_GridSize * k_CellSize;
			var atlas = new Texture2D(size, size, TextureFormat.RGBA32, true, false);
			var fill = new Color32[size * size];
			for (var i = 0; i < fill.Length; i++) fill[i] = new Color32(200, 200, 200, 255);
			atlas.SetPixels32(fill);

			var temp = new Texture2D(k_CellSize, k_CellSize, TextureFormat.RGBA32, false, false);
			for (var i = 0; i < sources.Count; i++)
			{
				var source = sources[i].GetComponentInChildren<Renderer>().sharedMaterial.GetTexture("_BaseMap");
				var target = RenderTexture.GetTemporary(k_CellSize, k_CellSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
				Graphics.Blit(source, target);
				var previous = RenderTexture.active;
				RenderTexture.active = target;
				temp.ReadPixels(new Rect(0, 0, k_CellSize, k_CellSize), 0, 0);
				RenderTexture.active = previous;
				RenderTexture.ReleaseTemporary(target);

				atlas.SetPixels32((i % k_GridSize) * k_CellSize, (i / k_GridSize) * k_CellSize, k_CellSize, k_CellSize, temp.GetPixels32());
			}

			Object.DestroyImmediate(temp);
			atlas.Apply(true);

			File.WriteAllBytes(k_AtlasPath, atlas.EncodeToPNG());
			Object.DestroyImmediate(atlas);
			AssetDatabase.ImportAsset(k_AtlasPath, ImportAssetOptions.ForceUpdate);

			var importer = (TextureImporter)AssetImporter.GetAtPath(k_AtlasPath);
			importer.textureType = TextureImporterType.Default;
			importer.sRGBTexture = true;
			importer.mipmapEnabled = true;
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.filterMode = FilterMode.Bilinear;
			importer.alphaSource = TextureImporterAlphaSource.None;
			importer.textureCompression = TextureImporterCompression.Compressed;
			importer.maxTextureSize = 512;
			importer.SaveAndReimport();
			return AssetDatabase.LoadAssetAtPath<Texture2D>(k_AtlasPath);
		}

		private static Piece MakePiece(GameObject source, int cell, bool isBuilding)
		{
			var filters = source.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToArray();
			if (filters.Length != 1) return null;
			var filter = filters[0];
			var renderer = filter.GetComponent<MeshRenderer>();
			if (renderer == null || renderer.sharedMaterials.Length != 1 || filter.sharedMesh.subMeshCount != 1) return null;

			// A copy of the mesh with its UVs moved into this piece's atlas cell (and pulled in from the cell edge, so
			// mip levels do not blend a neighbour's colour in).
			var mesh = Object.Instantiate(filter.sharedMesh);
			mesh.name = filter.sharedMesh.name + "_atlas";
			var uvs = new List<Vector2>();
			mesh.GetUVs(0, uvs);
			var origin = new Vector2((cell % k_GridSize) / (float)k_GridSize, (cell / k_GridSize) / (float)k_GridSize);
			for (var i = 0; i < uvs.Count; i++)
			{
				var uv = new Vector2(Mathf.Clamp01(uvs[i].x), Mathf.Clamp01(uvs[i].y));
				uvs[i] = origin + (new Vector2(k_CellMargin, k_CellMargin) + uv * (1f - 2f * k_CellMargin)) / k_GridSize;
			}

			mesh.SetUVs(0, uvs);
			mesh.uv2 = null;

			var local = source.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
			return new Piece
			{
				name = source.name,
				isBuilding = isBuilding,
				mesh = mesh,
				local = local,
				bounds = TransformBounds(mesh.bounds, local),
				triangles = mesh.triangles.Length / 3,
			};
		}

		private static Material MakeMaterial(GameObject template, Texture2D atlas)
		{
			var material = AssetDatabase.LoadAssetAtPath<Material>(k_MaterialPath);
			if (material == null)
			{
				material = new Material(template.GetComponentInChildren<Renderer>().sharedMaterial) { name = "GroundDressing_Mat" };
				AssetDatabase.CreateAsset(material, k_MaterialPath);
			}

			material.SetTexture("_BaseMap", atlas);
			material.SetTexture("_MainTex", atlas);
			material.SetColor("_BaseColor", Color.white);
			EditorUtility.SetDirty(material);
			return material;
		}

		// The x of every grey lot (the pack's 'Road Concrete Tile' pieces, 20 x 20) whose whole width is outside the lane.
		private static List<float> ReadLots()
		{
			var ground = AssetDatabase.LoadAssetAtPath<GameObject>(k_GroundSourcePath);
			var lots = new List<float>();
			if (ground == null) return lots;

			foreach (Transform child in ground.transform)
			{
				if (!child.name.StartsWith("Road Concrete Tile")) continue;
				if (Mathf.Abs(child.localPosition.x) - k_LotHalf >= k_LaneHalfWidth) lots.Add(child.localPosition.x);
			}

			lots.Sort();
			return lots;
		}

		private static void PlaceOnLot(float lotX, List<Piece> pieces, System.Random random, List<Placed> output)
		{
			var buildings = pieces.Where(p => p.isBuilding).ToList();
			var greenery = pieces.Where(p => !p.isBuilding).ToList();
			var firstOfLot = output.Count;

			// One building, turned a quarter at random, as near the middle as the jitter allows.
			for (var attempt = 0; attempt < 8; attempt++)
			{
				var piece = buildings[random.Next(buildings.Count)];
				var yaw = random.Next(4) * 90f;
				var footprint = Footprint(piece, Vector3.zero, yaw);
				if (footprint.width > k_LotFitWidth || footprint.height > k_LotFitWidth) continue;

				var spareX = Mathf.Max(0f, k_LotHalf - footprint.width / 2f - 0.5f);
				var spareZ = Mathf.Max(0f, k_LotHalf - footprint.height / 2f - 0.5f);
				var position = new Vector3(lotX + Jitter(random, spareX), 0f, Jitter(random, spareZ));
				output.Add(new Placed { piece = piece, position = position, yaw = yaw, footprint = Footprint(piece, position, yaw) });
				break;
			}

			// Then 1 to 3 trees, bushes, rocks or lights in the space left on the lot.
			var extras = 1 + random.Next(3);
			for (var n = 0; n < extras; n++)
			{
				for (var attempt = 0; attempt < 30; attempt++)
				{
					var piece = greenery[random.Next(greenery.Count)];
					var yaw = random.Next(360);
					var position = new Vector3(lotX + Jitter(random, k_LotHalf - 1.5f), 0f, Jitter(random, k_LotHalf - 1.5f));
					var footprint = Footprint(piece, position, yaw);
					if (!InsideLot(footprint, lotX) || Overlaps(footprint, output, firstOfLot)) continue;

					output.Add(new Placed { piece = piece, position = position, yaw = yaw, footprint = footprint });
					break;
				}
			}
		}

		private static float Jitter(System.Random random, float spread) => (float)(random.NextDouble() * 2.0 - 1.0) * spread;

		private static bool InsideLot(Rect footprint, float lotX)
		{
			return footprint.xMin >= lotX - k_LotHalf && footprint.xMax <= lotX + k_LotHalf && footprint.yMin >= -k_LotHalf && footprint.yMax <= k_LotHalf;
		}

		private static bool Overlaps(Rect footprint, List<Placed> placed, int from)
		{
			for (var i = from; i < placed.Count; i++)
			{
				var other = placed[i].footprint;
				other.xMin -= 0.4f; other.yMin -= 0.4f; other.xMax += 0.4f; other.yMax += 0.4f;
				if (other.Overlaps(footprint)) return true;
			}

			return false;
		}

		// The piece's outline on the ground (x and z), in tile space.
		private static Rect Footprint(Piece piece, Vector3 position, float yaw)
		{
			var bounds = TransformBounds(piece.bounds, Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one));
			return new Rect(bounds.min.x, bounds.min.z, bounds.size.x, bounds.size.z);
		}

		private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
		{
			var result = new Bounds(matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, -1, -1))), Vector3.zero);
			for (var i = 1; i < 8; i++)
			{
				var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
				result.Encapsulate(matrix.MultiplyPoint3x4(corner));
			}

			return result;
		}

		private static Mesh BakeVariant(int variant, List<Placed> placed)
		{
			var combine = new List<CombineInstance>();
			foreach (var item in placed)
			{
				// The lane is never dressed: a piece that reaches inside it is dropped (the lot filter makes this a safety net).
				if (item.footprint.xMax > -k_LaneHalfWidth && item.footprint.xMin < k_LaneHalfWidth)
				{
					Debug.LogWarning($"[World] Dropped '{item.piece.name}' at x {item.position.x:0.0}: inside the lane.");
					continue;
				}

				combine.Add(new CombineInstance
				{
					mesh = item.piece.mesh,
					transform = Matrix4x4.TRS(item.position, Quaternion.Euler(0f, item.yaw, 0f), Vector3.one) * item.piece.local,
				});
			}

			var path = $"{k_OutputFolder}/GroundDressing_v{variant}_Mesh.asset";
			var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
			var isNew = mesh == null;
			if (isNew) mesh = new Mesh { name = $"GroundDressing_v{variant}_Mesh" };
			else mesh.Clear();

			// The index format has to be set before combining: changing it afterwards throws the indices away.
			mesh.indexFormat = combine.Sum(c => c.mesh.vertexCount) > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
			mesh.CombineMeshes(combine.ToArray(), true, true);
			mesh.RecalculateBounds();
			mesh.UploadMeshData(false);
			if (isNew) AssetDatabase.CreateAsset(mesh, path);
			else EditorUtility.SetDirty(mesh);
			return mesh;
		}

		private static void AddVariantsToTile(List<Mesh> meshes, Material material)
		{
			var root = PrefabUtility.LoadPrefabContents(k_TilePath);
			try
			{
				foreach (var old in root.GetComponentsInChildren<Transform>(true).Where(t => t != root.transform && t.name.StartsWith("Dressing_")).ToArray())
				{
					Object.DestroyImmediate(old.gameObject);
				}

				var variants = new List<GameObject>();
				for (var v = 0; v < meshes.Count; v++)
				{
					var child = new GameObject($"Dressing_{v}");
					child.transform.SetParent(root.transform, false);
					child.AddComponent<MeshFilter>().sharedMesh = meshes[v];
					var renderer = child.AddComponent<MeshRenderer>();
					renderer.sharedMaterial = material;
					// Dressing only receives light, like the ground itself: no shadow casting.
					renderer.shadowCastingMode = ShadowCastingMode.Off;
					renderer.receiveShadows = true;
					child.SetActive(false);
					variants.Add(child);
				}

				var view = root.GetComponent<GroundTileView>();
				if (view == null) view = root.AddComponent<GroundTileView>();
				var serialized = new SerializedObject(view);
				var property = serialized.FindProperty("m_dressings");
				property.arraySize = variants.Count;
				for (var i = 0; i < variants.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
				serialized.ApplyModifiedPropertiesWithoutUndo();

				PrefabUtility.SaveAsPrefabAsset(root, k_TilePath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}
	}
}
