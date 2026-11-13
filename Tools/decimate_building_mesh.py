"""Cuts a building FBX down to a triangle budget with Blender's Decimate (collapse), for the 3D art pass.

Run headless (Blender 4.5 or newer):
    blender --background --python Tools/decimate_building_mesh.py -- <source.fbx> <output.fbx> <target_triangles>

Used for the Mellow Fox "Versatile Building Kit" (Assets/_Project/Art/Models/Buildings): the pack's models are 100k-370k
triangles, far too heavy for Android / WebGL. Notes from the milestone (see Docs/technical-design/building-crash-system.md):
- 25,000 triangles keeps clean windows and frames. 8,000 tears holes through the facade (the sky shows through).
- Welding vertices before decimating makes it worse, and planar dissolve barely helps (the detail is real geometry).
- The pack has no textures (flat grey material colours), so the UVs are dropped: it welds vertices and cuts the vertex count.
- Material slots and the model orientation / size are kept, so a decimated FBX can replace the original in a prefab.
In Unity, import the output with Normals = Calculate (smoothing angle 60), Tangents = None, Read/Write off.
"""
import bmesh
import bpy
import sys

src, dst, target = sys.argv[sys.argv.index("--") + 1:][:3]
target = int(target)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']

tri_total = 0
for o in meshes:
    o.data.calc_loop_triangles()
    tri_total += len(o.data.loop_triangles)
print("BEFORE tris", tri_total, "objects", len(meshes))

# Spread the triangle budget over the meshes by their share of the original.
for o in meshes:
    o.data.calc_loop_triangles()
    share = len(o.data.loop_triangles) / tri_total
    ratio = min(1.0, (target * share) / max(1, len(o.data.loop_triangles)))
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    modifier = o.modifiers.new("dec", 'DECIMATE')
    modifier.decimate_type = 'COLLAPSE'
    modifier.ratio = ratio
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)

for o in meshes:
    bpy.context.view_layer.objects.active = o
    try:
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except Exception as e:  # not every Blender version has custom split normals on the mesh
        print("clear normals:", e)
    bpy.ops.object.shade_smooth()
    while len(o.data.uv_layers):
        o.data.uv_layers.remove(o.data.uv_layers[0])
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
    bm.to_mesh(o.data)
    bm.free()

after = 0
for o in meshes:
    o.data.calc_loop_triangles()
    after += len(o.data.loop_triangles)
print("AFTER tris", after, "verts", sum(len(o.data.vertices) for o in meshes))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(
    filepath=dst, use_selection=True, apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z', axis_up='Y', path_mode='STRIP', mesh_smooth_type='OFF',
)
