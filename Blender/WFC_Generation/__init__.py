bl_info = {
    "name": "WFC Prototype Generation",
    "blender": (3, 2, 0),
    "category": "Import-Export"
}

import bpy

from . import PrototypeGeneration
from . import MeshExport
from . import SocketCompat

classes = [
    PrototypeGeneration.OBJECT_OT_WFCPrototypeGeneration,
    PrototypeGeneration.VIEW3D_PT_WFCPrototypeGeneration,
    PrototypeGeneration.VIEW3D_PT_WFCPrototypeData,

    MeshExport.OBJECT_OT_WFC_MeshExport,
    MeshExport.VIEW3D_PT_WFCMeshExport,

    SocketCompat.OBJECT_OT_WFCSocketCompatGeneration,
    SocketCompat.VIEW3D_PT_WFCSocketCompat,
]


def register():
    PrototypeGeneration.register_properties()
    MeshExport.register_properties()
    SocketCompat.register_properties()

    for c in classes:
        bpy.utils.register_class(c)


def unregister():
    PrototypeGeneration.unregister_properties()
    MeshExport.unregister_properties()
    SocketCompat.unregister_properties()

    for c in list(reversed(classes)):
        bpy.utils.unregister_class(c)
