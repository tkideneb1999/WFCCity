import bpy
import json
from pathlib import Path


class VIEW3D_PT_WFCSocketCompat(bpy.types.Panel):
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "WFC Prototype Generation"
    bl_label = "Socket Export"

    def draw(self, context):
        layout = self.layout
        layout.prop(context.scene, 'WFC_socket_compat_filepath', text="File Path")
        layout.operator('object.wfc_generate_socket_compat')
        layout.label(text="Socket Data:")
        if context.object:
            layout.prop(context.object, 'WFC_sockets_collection', text="Socket Collection")
        else:
            layout.label(text="-no active object")


class OBJECT_OT_WFCSocketCompatGeneration(bpy.types.Operator):
    bl_idname = 'object.wfc_generate_socket_compat'
    bl_label = 'WFC Generate Socket Compatibility File'

    def execute(self, context):
        file_path_s = context.scene.WFC_socket_compat_filepath
        if file_path_s == "":
            self.report({'ERROR'}, "Dir does not exist")
            return {'CANCELLED'}
        file_path = Path(bpy.path.abspath(file_path_s))
        if not file_path.parent.exists():
            self.report({'ERROR'}, f"File Path Directory {str(file_path.parent)} does not exist")
            return {'CANCELLED'}
        socket_compat = []
        socket_list = set()
        for o in bpy.context.selected_objects:

            if context.scene.WFC_use_proxies:
                name = o.WFC_proxy_object.name
            else:
                name = o.name
            sockets = []
            for s in o.children:
                if s.WFC_sockets_collection is None:
                    print(f"{o.name} has no Socket collection set, skipping")
                    continue
                socket_meshes = [m.name for m in s.WFC_sockets_collection.objects]
                for sm in socket_meshes:
                    socket_list.add(sm)
                socket = {
                    "name": s.name,
                    "socketMeshes": socket_meshes
                }
                sockets.append(socket)
            base_prototype = {
                "basePrototype": name,
                "sockets": sockets,
            }
            socket_compat.append(base_prototype)
        print(socket_list)
        with file_path.open("w", encoding='utf-8') as f:
            f.write(json.dumps(
                {"socketList": list(socket_list), "socketCompat": socket_compat},
                indent=4))
        return {'FINISHED'}


def register_properties():
    bpy.types.Scene.WFC_socket_compat_filepath = bpy.props.StringProperty(
        name="WFC Socket Compat File",
        subtype="FILE_PATH",
        default="//socketCompat.json"
    )

    bpy.types.Object.WFC_sockets_collection = bpy.props.PointerProperty(
        type=bpy.types.Collection,
        name="Socket Collection"
    )


def unregister_properties():
    del bpy.types.Scene.WFC_socket_compat_filepath
    del bpy.types.Object.WFC_sockets_collection
