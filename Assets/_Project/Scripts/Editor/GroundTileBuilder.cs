using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Opoint8182.WorldEditor
{
	/// <summary>
	/// Merges the pack's hand-built <c>Prefabs/Misc/Ground.prefab</c> (45 road and lot pieces, one material) into one mesh and
	/// one renderer, so a recycled ground tile costs a single draw call. Output: <c>Prefabs/World/GroundTile.prefab</c> and
	/// its mesh. Re-running updates both in place, so references to the prefab and the mesh stay valid. It ends by running
	/// <see cref="GroundDressingBuilder"/>, which adds the side dressing variants to the tile prefab.
	/// </summary>
	public static class GroundTileBuilder
	{
		private const string k_SourcePath = "Assets/_Project/Prefabs/Misc/Ground.prefab";
		private const string k_OutputFolder = "Assets/_Project/Prefabs/World";
		private const string k_MeshPath = k_OutputFolder + "/GroundTile_Mesh.asset";
		private const string k_PrefabPath = k_OutputFolder + "/GroundTile.prefab";

		[MenuItem("Tools/World/Build Ground Tile")]
		private static void BuildMenu() => Debug.Log(Build());

		public static string Build()
		{
			var source = AssetDatabase.LoadAssetAtPath<GameObject>(k_SourcePath);
			if (source == null) return $"[World] {k_SourcePath} not found.";

			var filters = source.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToArray();
			if (filters.Length == 0) return "[World] The ground prefab has no meshes.";

			var material = filters[0].GetComponent<MeshRenderer>().sharedMaterial;
			var toRoot = source.transform.worldToLocalMatrix;
			var combine = filters.Select(f => new CombineInstance
			{
				mesh = f.sharedMesh,
				transform = toRoot * f.transform.localToWorldMatrix,
			}).ToArray();

			if (!AssetDatabase.IsValidFolder(k_OutputFolder)) AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "World");

			var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(k_MeshPath);
			var isNew = mesh == null;
			if (isNew) mesh = new Mesh { name = "GroundTile_Mesh" };
			else mesh.Clear();

			mesh.CombineMeshes(combine, true, true);
			mesh.RecalculateBounds();
			mesh.UploadMeshData(false);
			if (isNew) AssetDatabase.CreateAsset(mesh, k_MeshPath);
			else EditorUtility.SetDirty(mesh);

			var root = new GameObject("GroundTile");
			root.AddComponent<MeshFilter>().sharedMesh = mesh;
			var renderer = root.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = material;
			// The tile only receives light: no shadow casting, which would cost a pass for a flat floor.
			renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			renderer.receiveShadows = true;
			PrefabUtility.SaveAsPrefabAsset(root, k_PrefabPath);
			Object.DestroyImmediate(root);
			AssetDatabase.SaveAssets();

			var result = $"[World] Built {k_PrefabPath}: {filters.Length} pieces merged, {mesh.vertexCount} vertices, {mesh.triangles.Length / 3} triangles, bounds {mesh.bounds.size}.";

			// The prefab was just rewritten from scratch, so the side dressing has to be put back on it.
			return result + "\n" + GroundDressingBuilder.Build();
		}
	}
}
