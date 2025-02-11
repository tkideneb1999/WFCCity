bl_info = {
    "name": "WFC Prototype Generation",
    "blender": (3, 2, 0),
    "category": "Object"
}

import bpy
import mathutils
from pathlib import Path
import json
import math


class VIEW3D_PT_WFCPrototypeGeneration(bpy.types.Panel):
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "WFC Prototype Generation"
    bl_label = "Generation Settings"

    def draw(self, context):
        layout = self.layout
        layout.prop(context.scene, 'WFC_prototype_def_file_path', text="File")
        layout.prop(context.scene, 'WFC_bounds_size', text="Bounds Size")
        layout.prop(context.scene, 'WFC_match_resolution', text="Match Resolution")
        layout.operator('object.wfc_generate_prototypes')


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


class VIEW3D_PT_WFCPrototypeData(bpy.types.Panel):
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "WFC Prototype Generation"
    bl_label = "Object Data"

    def draw(self, context):
        layout = self.layout
        if context.object:
            layout.prop(context.object, 'WFC_proxy_object')
            layout.prop(context.object, 'WFC_type_name')
            layout.prop(context.object, 'WFC_level_name')
            col = layout.column(align=True, heading='Adjacent Types')
            col.prop(context.object, 'WFC_adj_types_Xp', text='Xp')
            col.prop(context.object, 'WFC_adj_types_Xn', text='Xn')
            col.prop(context.object, 'WFC_adj_types_Yp', text='Yp')
            col.prop(context.object, 'WFC_adj_types_Yn', text='Yn')
            col.prop(context.object, 'WFC_adj_types_Zp', text='Zp')
            col.prop(context.object, 'WFC_adj_types_Zn', text='Zn')
        else:
            layout.label(text="-no active object-")


class OBJECT_OT_WFCPrototypeGeneration(bpy.types.Operator):
    bl_idname = 'object.wfc_generate_prototypes'
    bl_label = 'WFC Generate Prototypes'

    def mirror_vertices(self, vertices: list, coordinate: str):
        index = 0
        if coordinate == 'x':
            index = 0
        elif coordinate == 'y':
            index = 1
        mirrored = []
        for v in vertices:
            new_pos = v.copy()
            new_pos[index] = -new_pos[index]
            mirrored.append(new_pos)
        return mirrored

    def rotate_vertices90(self, vertices: list, angle90: int):
        print("---Rotating Vertices---")
        print("Input Vertices:")
        print(vertices)
        rotation_list = [0, 1, 0, -1]
        rotated_vertices = []
        for v in vertices:
            v_rot = [
                v[0] * rotation_list[(angle90 + 1) % 4] - v[1] * rotation_list[angle90],
                v[0] * rotation_list[angle90] + v[1] * rotation_list[(angle90 + 1) % 4]
            ]
            rotated_vertices.append(v_rot)
        print("Output Vertices:")
        print(rotated_vertices)
        return rotated_vertices

    def is_symmetrical(self, vertices: list, mirrored_vertices: list):  # TODO: DOES NOT WORK
        for v in vertices:
            if v not in mirrored_vertices:
                return False
        return True

    def is_profile(self, profile: tuple, target_profile: tuple):
        # print("---Checking Profile---")
        # print("Profile:")
        # print(profile)
        # print("Target Profile:")
        # print(target_profile)
        if not profile:
            return False
        # False if they do not have the same amount of vertices
        if not (len(profile[0]) == len(target_profile[0])):
            # print("Profiles do not match")
            # print("")
            return False
        # if profile[1] == target_profile[1]:
        #     aligned_profile = target_profile[0]
        # else:
        #     aligned_profile = self.mirror_vertices(target_profile[0], 'x')

        for p in profile[0]:
            if p not in target_profile[0]:
                # print("Profiles do not match")
                # print("")
                return False
        # print("Profiles match")
        # print("")
        return True

    def point_close(self, point: list, target_point: list) -> bool:
        for i in range(len(target_point)):
            if not math.isclose(point[i], target_point[i], abs_tol=1e-6):
                return False
        return True

    def point_in_list(self, point: list, target_list: list) -> bool:  # TODO: PROBABLY DOES NOT WORK
        for tp in target_list:
            if self.point_close(point, tp):
                return True
        return False

    def swizzle_vertical_socket(self, socket: dict, rotation):
        if socket["socket"] == "-1":
            return socket
        parts = socket["socket"].split('_')
        return {
            "socket": f"{parts[0]}_{(int(parts[1]) + rotation) % 4}",
            "adjacentTypes": socket["adjacentTypes"],
        }

    def round_half_up(self, n, decimals=0):
        multiplier = 10 ** decimals
        return math.floor(n*multiplier + 0.5) / multiplier

    def execute(self, context):
        h_connector_counter = 0
        h_connection_dict = {}
        v_connector_counter = 0
        v_connection_dict = {}
        match_resolution = context.scene.WFC_match_resolution
        bounds_size = int(context.scene.WFC_bounds_size * match_resolution)

        prototypes = []
        base_prototypes = []

        for m in bpy.context.selected_objects:
            vertices = m.data.vertices
            mesh_profiles = {
                "Xp": [],
                "Xn": [],
                "Yp": [],
                "Yn": [],
            }

            profile_side = {
                "Xp": 1,
                "Xn": -1,
                "Yp": 1,
                "Yn": -1,
            }

            v_mesh_profiles = {
                "Zp": [],
                "Zn": []
            }

            adjacent_types = {
                "Xp": m.WFC_adj_types_Xp,
                "Xn": m.WFC_adj_types_Xn,
                "Yp": m.WFC_adj_types_Yp,
                "Yn": m.WFC_adj_types_Yn,
                "Zp": m.WFC_adj_types_Zp,
                "Zn": m.WFC_adj_types_Zn,
            }
            for adjType in adjacent_types:
                adj_type_string = adjacent_types[adjType].replace(' ', '')
                split_types = adj_type_string.split(',')
                adjacent_types[adjType] = split_types
            # Capture Profiles
            print("=== CAPTURING PROFILES ===")
            for i in vertices:
                q_pos = [int(self.round_half_up(i.co[j] * match_resolution)) for j in range(len(i.co))]
                # X
                print("Vertex: " + str(q_pos))
                if q_pos[0] == bounds_size:
                    pos_x = [q_pos[1], q_pos[2]]
                    mesh_profiles["Xp"].append(pos_x)
                    print("Assigned to: Xp")
                elif q_pos[0] == -bounds_size:
                    pos = i.co.copy()
                    pos.rotate(mathutils.Euler((0, 0, math.pi)))
                    pos = [int(self.round_half_up(pos[1] * match_resolution)),
                           int(self.round_half_up(pos[2] * match_resolution))]
                    pos_x = [q_pos[1], q_pos[2]]
                    mesh_profiles["Xn"].append(pos)
                    print("Assigned to: Xn")
                # Y
                if q_pos[1] == bounds_size:
                    pos = i.co.copy()
                    pos.rotate(mathutils.Euler((0, 0, -0.5 * math.pi)))
                    pos = [int(self.round_half_up(pos[1] * match_resolution)),
                           int(self.round_half_up(pos[2] * match_resolution))]
                    pos_y = [q_pos[0], q_pos[2]]
                    mesh_profiles["Yp"].append(pos)
                    print("Assigned to: Yp")
                elif q_pos[1] == -bounds_size:
                    pos = i.co.copy()
                    pos.rotate(mathutils.Euler((0, 0, 0.5 * math.pi)))
                    pos = [int(self.round_half_up(pos[1] * match_resolution)),
                           int(self.round_half_up(pos[2] * match_resolution))]
                    pos_y = [q_pos[0], q_pos[2]]
                    mesh_profiles["Yn"].append(pos)
                    print("Assigned to: Yn")
                # Z
                if q_pos[2] == bounds_size:
                    pos_z = [q_pos[0], q_pos[1]]
                    v_mesh_profiles["Zp"].append(pos_z)
                    print("Assigned to: Zp")
                elif q_pos[2] == -bounds_size:
                    pos_z = [q_pos[0], q_pos[1]]
                    v_mesh_profiles["Zn"].append(pos_z)
                    print("Assigned to: Zn")
            print("---Vertical Profiles---")
            for p in mesh_profiles:
                print("Side: " + p)
                print(mesh_profiles[p])
            print("")
            print("")
            print("=== HORIZONTAL SOCKETS===")
            # Horizontal Profiles
            mirrored_profiles = {
                "Xp": self.mirror_vertices(mesh_profiles["Xp"], 'x'),
                "Xn": self.mirror_vertices(mesh_profiles["Xn"], 'x'),
                "Yp": self.mirror_vertices(mesh_profiles["Yp"], 'x'),
                "Yn": self.mirror_vertices(mesh_profiles["Yn"], 'x')
            }

            socket_dict = {}
            for mp in mesh_profiles:
                print("Checking for Mesh: " + m.name)
                print("Checking Side: " + mp)
                socket = None
                if not mesh_profiles[mp]:
                    socket = "-1"

                is_symmetrical = self.is_symmetrical(mesh_profiles[mp], mirrored_profiles[mp])

                if socket is None:
                    profile_list = list(h_connection_dict.values())
                    profile_ids = list(h_connection_dict.keys())
                    profile = None
                    for i in range(len(profile_list)):
                        print("Checking against Profile: " + profile_ids[i])
                        if self.is_profile((mesh_profiles[mp], profile_side[mp] if not is_symmetrical else 0), profile_list[i]):
                            profile = profile_list[i]
                            break
                    if profile:
                        index = profile_list.index(profile)
                        socket = list(h_connection_dict.keys())[index]

                if socket is None:
                    if is_symmetrical:
                        h_connection_dict[f"{h_connector_counter}s"] = (mesh_profiles[mp], 0)
                        socket = f"{h_connector_counter}s"
                    else:
                        h_connection_dict[f"{h_connector_counter}"] = (mesh_profiles[mp], profile_side[mp])
                        h_connection_dict[f"{h_connector_counter}f"] = (mirrored_profiles[mp], profile_side[mp])
                        socket = f"{h_connector_counter}"
                    h_connector_counter += 1

                socket_dict[mp] = {
                    "socket": socket,
                    "adjacentTypes": adjacent_types[mp]
                }
                print("")

            print("---Horizontal Connections---")
            for c in h_connection_dict:
                print("Name: " + c)
                print(h_connection_dict[c])

            print("")
            print("")
            print("===VERTICAL SOCKETS===")
            # Vertical profiles
            for mp in v_mesh_profiles:
                # Check if already exists
                socket = None
                if not v_mesh_profiles[mp]:
                    socket = "-1"

                if socket is None:
                    profile_list = list(v_connection_dict.values())
                    profile = None
                    for p in profile_list:
                        if self.is_profile((v_mesh_profiles[mp], 0), p):
                            profile = p
                            break
                    if profile:
                        index = profile_list.index(profile)
                        socket = list(v_connection_dict.keys())[index]
                if socket is None:
                    rotations = [
                        (v_mesh_profiles[mp], 0),
                        (self.rotate_vertices90(v_mesh_profiles[mp], 3), 0),
                        (self.rotate_vertices90(v_mesh_profiles[mp], 2), 0),
                        (self.rotate_vertices90(v_mesh_profiles[mp], 1), 0)
                    ]
                    # Vertical Socket Symmetrical
                    for i in range(4):
                        v_connection_dict[f"v{v_connector_counter}_{i}"] = rotations[i]
                    socket = f"v{v_connector_counter}_0"
                    v_connector_counter += 1
                socket_dict[mp] = {
                    "socket": socket,
                    "adjacentTypes": adjacent_types[mp]
                }

            # Assemble Prototypes

            base_prototypes.append({
                "name": m.name,
                "type": m.WFC_type_name,
                "mesh": m.name,
            })

            prototypes.append({
                "name": f"{m.name}_0",
                "level": m.WFC_level_name,
                "basePrototype": m.name,
                "mesh": m.name,
                "rotation": 0,
                "sockets": socket_dict,
            })
            prototypes.append({
                "name": f"{m.name}_1",
                "level": m.WFC_level_name,
                "basePrototype": m.name,
                "mesh": m.name,
                "rotation": 1,
                "sockets": {
                    "Xp": socket_dict["Yp"],
                    "Xn": socket_dict["Yn"],
                    "Yp": socket_dict["Xn"],
                    "Yn": socket_dict["Xp"],
                    "Zp": self.swizzle_vertical_socket(socket_dict["Zp"], 1),
                    "Zn": self.swizzle_vertical_socket(socket_dict["Zn"], 1)
                },
            })
            prototypes.append({
                "name": f"{m.name}_2",
                "level": m.WFC_level_name,
                "basePrototype": m.name,
                "mesh": m.name,
                "rotation": 2,
                "sockets": {
                    "Xp": socket_dict["Xn"],
                    "Xn": socket_dict["Xp"],
                    "Yp": socket_dict["Yn"],
                    "Yn": socket_dict["Yp"],
                    "Zp": self.swizzle_vertical_socket(socket_dict["Zp"], 2),
                    "Zn": self.swizzle_vertical_socket(socket_dict["Zn"], 2)
                },
            })
            prototypes.append({
                "name": f"{m.name}_3",
                "level": m.WFC_level_name,
                "basePrototype": m.name,
                "mesh": m.name,
                "rotation": 3,
                "sockets": {
                    "Xp": socket_dict["Yn"],
                    "Xn": socket_dict["Yp"],
                    "Yp": socket_dict["Xp"],
                    "Yn": socket_dict["Xn"],
                    "Zp": self.swizzle_vertical_socket(socket_dict["Zp"], 3),
                    "Zn": self.swizzle_vertical_socket(socket_dict["Zn"], 3)
                },
            })
        null_prototype_socket = {
                    "socket": "-1",
                    "adjacentTypes": "None"
                }
        prototypes.append({
            "name": "nullPrototype",
            "basePrototype": "nullBase",
            "mesh": "",
            "level": "",
            "rotation": 0,
            "sockets": {
                "Xp": null_prototype_socket,
                "Xn": null_prototype_socket,
                "Yp": null_prototype_socket,
                "Yn": null_prototype_socket,
                "Zp": null_prototype_socket,
                "Zn": null_prototype_socket,
            },
        })

        base_prototypes.append({
            "name": "nullBase",
            "type": "None",
        })

        export_path = Path(bpy.path.abspath(context.scene.WFC_prototype_def_file_path))
        with export_path.open("w", encoding='utf-8') as f:
            f.write(json.dumps({"basePrototypes": base_prototypes, "prototypes": prototypes}, indent=4))
        print("")
        print("")
        return {'FINISHED'}


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


classes = [
    OBJECT_OT_WFCPrototypeGeneration,
    OBJECT_OT_WFC_MeshExport,
    VIEW3D_PT_WFCPrototypeData,
    VIEW3D_PT_WFCPrototypeGeneration,
    VIEW3D_PT_WFCMeshExport
]


def register():
    # PROTOTYPE GENERATION
    bpy.types.Scene.WFC_prototype_def_file_path = bpy.props.StringProperty(
        name='WFC Prototype File Path',
        subtype='FILE_PATH',
        default='//prototypes.json'
    )
    bpy.types.Scene.WFC_bounds_size = bpy.props.FloatProperty(
        name='WFC Bounds Size',
        min=0.0,
        default=1.0
    )
    bpy.types.Scene.WFC_match_resolution = bpy.props.IntProperty(
        name="WFC Matching Resolution",
        min=1,
        default=256
    )

    # EXPORT
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

    # OBJECT DATA
    bpy.types.Object.WFC_proxy_object = bpy.props.PointerProperty(
        type=bpy.types.Object,
        name='WFC Proxy'
    )
    bpy.types.Object.WFC_type_name = bpy.props.StringProperty(
        name='WFC Type',
        default=''
    )
    bpy.types.Object.WFC_level_name = bpy.props.StringProperty(
        name='WFC Level',
        default=''
    )
    bpy.types.Object.WFC_adj_types_Xp = bpy.props.StringProperty(
        name='WFC Adjacent Types Xp',
        description='List of types that can be placed in X+ Direction',
        default='None',
    )
    bpy.types.Object.WFC_adj_types_Xn = bpy.props.StringProperty(
        name='WFC Adjacent Types Xn',
        description='List of types that can be placed in X- Direction',
        default='None',
    )
    bpy.types.Object.WFC_adj_types_Yp = bpy.props.StringProperty(
        name='WFC Adjacent Types Yp',
        description='List of types that can be placed in Y+ Direction',
        default='None',
    )
    bpy.types.Object.WFC_adj_types_Yn = bpy.props.StringProperty(
        name='WFC Adjacent Types Yn',
        description='List of types that can be placed in Y- Direction',
        default='None',
    )
    bpy.types.Object.WFC_adj_types_Zp = bpy.props.StringProperty(
        name='WFC Adjacent Types Zp',
        description='List of types that can be placed in Z+ Direction',
        default='None',
    )
    bpy.types.Object.WFC_adj_types_Zn = bpy.props.StringProperty(
        name='WFC Adjacent Types Zn',
        description='List of types that can be placed in Z- Direction',
        default='None',
    )

    for c in classes:
        bpy.utils.register_class(c)


def unregister():
    del bpy.types.Scene.WFC_prototype_def_file_path
    del bpy.types.Scene.WFC_bounds_size
    del bpy.types.Scene.WFC_match_resolution

    del bpy.types.Scene.WFC_export_with_children
    del bpy.types.Scene.WFC_use_proxies
    del bpy.types.Scene.WFC_export_dir

    del bpy.types.Object.WFC_proxy_object
    del bpy.types.Object.WFC_type_name
    del bpy.types.Object.WFC_level_name
    del bpy.types.Object.WFC_adj_types_Xp
    del bpy.types.Object.WFC_adj_types_Xn
    del bpy.types.Object.WFC_adj_types_Yp
    del bpy.types.Object.WFC_adj_types_Yn
    del bpy.types.Object.WFC_adj_types_Zp
    del bpy.types.Object.WFC_adj_types_Zn

    for c in list(reversed(classes)):
        bpy.utils.unregister_class(c)
