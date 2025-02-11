import bpy
from pathlib import Path


class VIEW3D_PT_WFCMeshExport(bpy.types.Panel):
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "WFC Prototype Generation"
    bl_label = "Export Settings"

    def draw(self, context):
        layout = self.layout
        layout.prop(context.scene, 'WFC_export_dir', text="Export Dir")
        layout.prop(context.scene, 'WFC_use_proxies', text="Use Proxies")
        layout.prop(context.scene, 'WFC_export_with_children', text="Export with Children")
        layout.operator('object.wfc_export_meshes')


class OBJECT_OT_WFC_MeshExport(bpy.types.Operator):
    bl_idname = 'object.wfc_export_meshes'
    bl_label = 'WFC Export Prototype Meshes'

    def get_children(self, obj, recursive=True):
        children_list = []
        for c in obj.children:
            children_list.append(c)
            if recursive:
                child_list = self.get_children(c)
                for ce in child_list:
                    children_list.append(ce)
        return children_list

    def execute(self, context):
        export_dir = context.scene.WFC_export_dir
        if export_dir == "":
            self.report({'ERROR'}, "Export Directory does not exist")
            return {'CANCELLED'}
        export_path = Path(bpy.path.abspath(export_dir))
        if not export_path.exists():
            self.report({'ERROR'}, f"Export Directory {str(export_path)} does not exist")
            return {'CANCELLED'}
        proxy_dict = {}
        selection = []
        for m in bpy.context.selected_objects:
            selection.append(m)
        for m in selection:
            if context.scene.WFC_use_proxies:
                if m.WFC_proxy_object is None:
                    print(f"{m.name} has no proxy set, skipping")
                    continue
                export_name = m.WFC_proxy_object.name
                if proxy_dict.get(export_name) is not None:
                    proxy_dict[export_name] += 1
                else:
                    proxy_dict[export_name] = 0
                export_name = f"{export_name}_var{proxy_dict[export_name]}"
            else:
                export_name = m.name
            bpy.ops.object.select_all(action='DESELECT')
            m.select_set(True)
            if context.scene.WFC_export_with_children:
                children = self.get_children(m)
                for c in children:
                    c.select_set(True)
            original_loc = m.location.copy()
            path = export_path / f"{export_name}.fbx"
            m.location = [0.0, 0.0, 0.0]
            bpy.ops.export_scene.fbx(
                filepath=str(path),
                use_selection=True,
                apply_scale_options='FBX_SCALE_UNITS',
                bake_space_transform=True,
                use_tspace=True,
                use_triangles=True,
            )
            m.location = original_loc
            print(export_name)

        return {'FINISHED'}


def register_properties():
    bpy.types.Scene.WFC_export_dir = bpy.props.StringProperty(
        name="WFC Export Dir",
        subtype='DIR_PATH'
    )
    bpy.types.Scene.WFC_export_with_children = bpy.props.BoolProperty(
        name="WFC Export with Children",
        default=True,
    )
    bpy.types.Scene.WFC_use_proxies = bpy.props.BoolProperty(
        name="WFC Use Proxies",
        default=False,
        description="Use selected WFC Meshes as Proxies for Prototype but replace them with linked meshes"
    )

    bpy.types.Object.WFC_proxy_object = bpy.props.PointerProperty(
        type=bpy.types.Object,
        name='WFC Proxy'
    )


def unregister_properties():
    del bpy.types.Scene.WFC_export_dir
    del bpy.types.Scene.WFC_export_with_children
    del bpy.types.Scene.WFC_use_proxies
    del bpy.types.Object.WFC_proxy_object
