using System;
using System.Collections.Generic;
using System.Text;

namespace V3M_Converter
{
    public class Vector3
    {
        public Vector3() { }

        // The following constructor has parameters for two of the three
        // properties.
        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
    public class Quat
    {
        public float X, Y, Z, W;
    }
    public class Texture
    {
        public int id;
        public string fileName;
    }
    public class RawData
    {
        public int start;
        public int end;
    }
    public class AxisAllignedBoundingBox
    {
        public AxisAllignedBoundingBox() { }
        public AxisAllignedBoundingBox(Vector3 P1, Vector3 P2)
        {
            point1 = P1;
            point2 = P2;
        }
        public Vector3 point1 { get; set; }
        public Vector3 point2 { get; set; }

    }
    public class LODMeshFlags
    {
        public bool unk80 = false;
        public bool unk40 = false;
        public bool trianglePlanes = true;
        public bool unk10 = false;
        public bool unk8 = false;
        public bool unk4 = false;
        public bool unk2 = false;
        public bool morphVerticesMap = false;

    }
    public class BatchInfo
    {
        public int numVertices;
        public int numTriangles;
        public int positionsSize;
        public int indicesSize;
        public int samePosVertexOffsetSize;
        public int boneLinksSize;
        public int texCoordsSize;
        public int renderflags;
    }
    public class MeshBatchHeader
    {
        public List<int> unknown = new List<int>();
        public int textureIdx;
        public List<int> unknown1 = new List<int>();
    }
    public class UV
    {
        public UV() { }

        // The following constructor has parameters for two of the three
        // properties.
        public UV(float u, float v)
        {
            U = u;
            V = v;
        }

        public float U { get; set; }
        public float V { get; set; }

    }
    public class Triangle
    {
        public List<int> indices = new List<int>();
        public int flags;
    }
    public class Plane
    {
        public Vector3 normal;
        public float dist;
    }
    public class VertexBones
    {
        public List<int> weights = new List<int>();
        public List<int> bones = new List<int>();
    }
    public class MeshBatchData
    {
        public int batchIndex;
        //side: _parent._parent.batch_info[batch_idx].positions_size / 12
        public List<Vector3> positions = new List<Vector3>();
        //size: (0x10 - _parent._io.pos) % 0x10
        public int[] padding0;
        //side: _parent._parent.batch_info[batch_idx].positions_size / 12
        public List<Vector3> normals = new List<Vector3>();
        //size: (0x10 - _parent._io.pos) % 0x10
        public int[] padding1;
        //size: _parent._parent.batch_info[batch_idx].tex_coords_size / 8
        public List<UV> texCoords = new List<UV>();
        //size: (0x10 - _parent._io.pos) % 0x10
        public int[] padding2;
        //size: _parent._parent.batch_info[batch_idx].indices_size / 8
        public List<Triangle> triangles = new List<Triangle>();
        //size: (0x10 - _parent._io.pos) % 0x10
        public int[] padding3;
        //use only if LODMeshFlags.triangulePlanes == true;
        //size: _parent._parent.batch_info[batch_idx].num_triangles
        public List<Plane> planes = new List<Plane>();
        //size: (0x10 - _parent._io.pos) % 0x10
        public int[] padding4;
        //size: _parent._parent.batch_info[batch_idx].same_pos_vertex_offsets_size / 2
        public List<int> samePosVertexOffsets = new List<int>();
        //size: (0x10 - _parent._io.pos) % 0x10
        public int[] padding5;
        //size: parent._parent.batch_info[batch_idx].bone_links_size / 8
        public List<VertexBones> boneLinks = new List<VertexBones>();
        //size: (0x10 - _parent._io.pos) % 0x10
        public int[] padding6;
        public List<int> morphVertsMap = new List<int>();
        //size: (0x10 - _parent._io.pos) % 0x10
        public int[] padding7;
    }
    public class PropPoint
    {
        public string name;
        public Quat rot;
        public Vector3 pos;
        public int bone;

    }
    public class LODMeshData
    {
        public List<MeshBatchHeader> batchHeaders = new List<MeshBatchHeader>();
        //size: (0x10 - _io.pos) % 0x10
        public int[] padding = new int[8];
        public List<MeshBatchData> batchData = new List<MeshBatchData>();
        //size: (0x10 - _io.pos) % 0x10
        public int[] padding1;
        public List<PropPoint> propPoints = new List<PropPoint>();
    }
    public class LODMesh
    {
        public LODMeshFlags flags;
        public int numVertices;
        public int numBatches;
        public int dataSize;
        public List<int> rawData = new List<int>();
        public int unknown = -0x1;
        public List<BatchInfo> batchInfo = new List<BatchInfo>();
        public int numPropPoints;
        public int numTextures;
        public List<Texture> textures = new List<Texture>();
        public LODMeshData data;
    }
    public class Material
    {
        public string diffuseMapName;
        public float emissiveFactor;
        public List<int> unknown = new List<int>();
        public float refCof;
        public string refMapName;
        public int flags;

    }
    public class SubMesh
    {
        public string name;
        public string unknown;
        public int version;

        public int numLods;
        public List<float> lodDistances = new List<float>();
        public Vector3 offset;
        public float radius;
        public AxisAllignedBoundingBox aabb;
        public List<LODMesh> lods = new List<LODMesh>();
        public int numMaterials;
        public List<Material> materials = new List<Material>();
        public int numUnknown1;
        /// <summary>
        /// 28 * numUnknown;
        /// </summary>
        public List<int> unknown1 = new List<int>();

    }
    public class Section
    {
        public string type;
        public int length;
        public SubMesh body;
    }

    public class Header
    {
        public int magic;
        public int version;
        public int submeshCount;
        public int vertCount;
        public int triCount;
        public int vertNormalCount;
        public int matCount;
        public int lodCount;
        public int dumbCount;
        public int sphereColCount;
        public bool isV3m;
        public bool isV3c;
    }
    class V3MData
    {
    }
}
