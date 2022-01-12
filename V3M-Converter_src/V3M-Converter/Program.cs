using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace V3M_Converter
{

    class Program
    {
        //Name of the file to read
        static string filename;
        //All bytes in the file
        static byte[] fileBytes;
        //Log level
        static int logLevel = 3;

        #region data
        static Header fileHeader;
        static List<int> rawData = new List<int>();
        static List<Section> sections = new List<Section>();
        #endregion

        #region Offsets
        static int currentBytePos = 0;
        static int rawDataStart = 0;
        #endregion

        #region MultiFile
        static bool readDirectory = false;
        static string[] files;
        #endregion
        static GLTFWriter writer;

        //Entry point
        static void Main(string[] args)
        {
            //Cleanup the console
            Console.Clear();
            print("V3M-Converter. Written by Marcel E. aka >P-Clan<PuTa> \nFile spec by Rafalh.", ConsoleColor.Red, 3);
            print("------------------------------", ConsoleColor.Red, 99);
            print("------------------------------", loglevel: 99);
            //If no argument is passed, return
            if (!ReadArgs(args))
                return;
            //Multi File
            if (readDirectory)
            {
                ReadMultiFile();
                return;
            }

            print($"File to read: {filename}, loglevel {logLevel}", loglevel: 99);
            if (filename.Split('.').Last().ToUpper() == "V3C")
            {
                print("WARNING: Only static meshes are supported at this moment. V3C Files may or may not work. Do you want to continue? [Y][N]\n");
                string answer = Console.ReadLine();
                if (answer.ToLower() == "n")
                {
                    print("Thank you for using the RF1-Toolkit!");
                    return;
                }
            }
            try
            {
                fileBytes = File.ReadAllBytes(filename);
            }
            catch (Exception)
            {
                print("Error reading file!", ConsoleColor.Red, 3);
                return;
            }

            fileHeader = GetHeader();
            for (int i = 0; i < fileHeader.submeshCount; i++)
            {
                sections.Add(GetSection());
            }

            print($"", ConsoleColor.Red, 99);
            print($"------------", ConsoleColor.Red, 99);

            print($"------------", ConsoleColor.Red, 99);
            print($"End of file.", ConsoleColor.Red, 99);

            writer = new GLTFWriter();
            writer.Init(fileHeader, sections, logLevel, filename.Split('\\').Last());
        }

        //Parse All files in Input Folder
        static void ReadMultiFile()
        {
            //Get all files in the Input folder
            files = Directory.GetFiles("Input");
            //Return if no files are present
            if (files.Length < 1)
            {
                print("No files in folder 'Input'.", ConsoleColor.Red, 3);
                print("Specify a file or add files to the folder 'Input'", ConsoleColor.Red, 3);
                return;
            }

            foreach (string file in files)
            {
                //Reset previous data
                currentBytePos = 0;
                fileHeader = null;
                rawData?.Clear();
                sections?.Clear();
                filename = file;
                print($"File to read: {filename}, loglevel {logLevel}", loglevel: 99);
                try
                {
                    fileBytes = File.ReadAllBytes(filename);
                }
                catch (Exception)
                {
                    print("Error reading file!", ConsoleColor.Red, 3);
                    continue;
                }

                fileHeader = GetHeader();
                for (int i = 0; i < fileHeader.submeshCount; i++)
                {
                    sections.Add(GetSection());
                }
                print($"", ConsoleColor.Red, 99);
                print($"------------", ConsoleColor.Red, 99);

                print($"------------", ConsoleColor.Red, 99);
                print($"End of file.", ConsoleColor.Red, 99);

                writer = new GLTFWriter();
                writer.Init(fileHeader, sections, logLevel, filename.Split('\\').Last());
            }
            return;
        }

        //Populate Header
        static Header GetHeader()
        {
            print("\n\n------------------------------", loglevel: 1);
            print("------------HEADER------------", loglevel: 1);
            print("\n", loglevel: 1);
            Header header = new Header();
            header.magic = GetNextInt32();
            header.version = GetNextInt32();
            header.submeshCount = GetNextInt32();
            header.vertCount = GetNextInt32();
            header.triCount = GetNextInt32();
            header.vertNormalCount = GetNextInt32();
            header.matCount = GetNextInt32();
            header.lodCount = GetNextInt32();
            header.dumbCount = GetNextInt32();
            header.sphereColCount = GetNextInt32();

            //CHECK IF THIS IS CORRECT!!!

            header.isV3m = true;//         GetNextBool();
            header.isV3c = false;//          GetNextBool();

            print("\n", loglevel: 1);
            print("----------HEADER-END----------", loglevel: 1);
            print("------------------------------", loglevel: 1);
            return header;
        }
        //Populate Section
        static Section GetSection()
        {
            print("\n\n------------------------------", loglevel: 1);
            print("-------------SECTION-----------", loglevel: 1);
            print("\n", loglevel: 1);


            Section section = new Section();
            section.type = GetNexString(4, true);
            if (section.type != "SUBM")
                return section;
            section.length = GetNextInt32();
            section.body = GetSubMesh();
            return section;
        }
        //Generate SubMesh
        static SubMesh GetSubMesh()
        {
            print("Reading SubMesh...", ConsoleColor.Yellow, 3);
            SubMesh subMesh = new SubMesh();
            subMesh.name = GetNexString(24, false);
            subMesh.unknown = GetNexString(24, false);
            subMesh.version = GetNextInt32();
            subMesh.numLods = GetNextInt32();
            for (int i = 0; i < subMesh.numLods; i++)
            {
                subMesh.lodDistances.Add(GetNextFloat());
            }
            subMesh.offset = GetNextVector();
            subMesh.radius = GetNextFloat();
            subMesh.aabb = GetNextAABB();

            for (int i = 0; i < subMesh.numLods; i++)
            {
                subMesh.lods.Add(GetNextLODMesh());
            }
            subMesh.numMaterials = GetNextInt32();
            print($"ForEach Material at: 0x{currentBytePos.ToString("X2")}", ConsoleColor.Red, 2);
            for (int i = 0; i < subMesh.numMaterials; i++)
            {
                subMesh.materials.Add(GetNextMaterial());
            }
            subMesh.numUnknown1 = GetNextInt32();
            for (int i = 0; i < 28 * subMesh.numUnknown1; i++)
            {
                subMesh.unknown1.Add(GetNextInt8());
            }
            print("Done reading SubMesh", ConsoleColor.Green, 3);

            return subMesh;
        }

        static AxisAllignedBoundingBox GetNextAABB()
        {
            AxisAllignedBoundingBox aabb = new AxisAllignedBoundingBox();
            aabb.point1 = GetNextVector();
            aabb.point2 = GetNextVector();
            return aabb;
        }

        static LODMesh GetNextLODMesh()
        {
            print("Reading LODMesh...", ConsoleColor.Yellow, 3);
            LODMesh lodMesh = new LODMesh();
            lodMesh.flags = GetNextLODMeshFlags();
            lodMesh.numVertices = GetNextInt32();
            lodMesh.numBatches = GetNextInt16();
            lodMesh.dataSize = GetNextInt32();
            rawDataStart = currentBytePos;
            for (int i = 0; i < lodMesh.dataSize; i++)
            {
                lodMesh.rawData.Add(GetNextInt8());
                rawData = lodMesh.rawData;
            }
            lodMesh.unknown = GetNextInt32();
            for (int i = 0; i < lodMesh.numBatches; i++)
            {
                lodMesh.batchInfo.Add(GetNextBatchInfo());
            }
            lodMesh.numPropPoints = GetNextInt32();
            lodMesh.numTextures = GetNextInt32();
            for (int i = 0; i < lodMesh.numTextures; i++)
            {
                lodMesh.textures.Add(GetNextTexture());
            }
            lodMesh.data = GetNextLODMeshData(lodMesh.numBatches, lodMesh);
            print("Done reading LODMesh.", ConsoleColor.Green, 3);
            return lodMesh;
        }
        static LODMeshData GetNextLODMeshData(int batchCount, LODMesh lodMesh)
        {
            print($"LOD Mesh data starts at: 0x{(rawDataStart).ToString("X2")}", ConsoleColor.Red, 2);
            print("Reading LODMeshData...", ConsoleColor.Yellow, 3);
            LODMeshData lodMeshData = new LODMeshData();
            int currentRawDataIndex = 0;
            int currentRelativeDataIndex = 0;
            for (int i = 0; i < batchCount; i++)
            {
                print($"meshBatchHeader starts at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Red, 2);
                MeshBatchHeader meshBatchHeader = new MeshBatchHeader();
                for (int j = 0; j < 0x20; j++)
                {
                    meshBatchHeader.unknown.Add(GetInt8At(rawDataStart + currentRawDataIndex));
                    currentRawDataIndex++;
                    currentRelativeDataIndex++;
                }
                meshBatchHeader.textureIdx = GetInt32At(rawDataStart + currentRawDataIndex);
                currentRawDataIndex += 4;
                currentRelativeDataIndex += 4;
                print($"TEXTURE Idx {meshBatchHeader.textureIdx}", ConsoleColor.Cyan, 2);
                for (int j = 0; j < 0x14; j++)
                {
                    meshBatchHeader.unknown1.Add(GetInt8At(rawDataStart + currentRawDataIndex));

                }

                currentRawDataIndex += 0x14;
                currentRelativeDataIndex += 0x14;

                print($"After unKnown1 = {(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.White, 2);
                lodMeshData.batchHeaders.Add(meshBatchHeader);
            }
            int padding0 = CalculatePadding(currentRelativeDataIndex);
            currentRawDataIndex += padding0;
            currentRelativeDataIndex += padding0;

            print($"batchcount loop starts at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Red, 2);
            for (int i = 0; i < batchCount; i++)
            {


                MeshBatchData meshBatchData = new MeshBatchData();
                print($"meshBatchData starts at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Yellow, 2);
                meshBatchData.batchIndex = i;
                #region POSITIONS
                print($"POSITIONS start at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Yellow, 2);
                int p0 = 0;
                for (int j = 0; j < lodMesh.batchInfo[i].positionsSize / 12; j++)
                {
                    meshBatchData.positions.Add(GetVectorAt(rawDataStart + currentRawDataIndex));
                    currentRawDataIndex += 12;
                }
                int pad0 = CalculatePadding(currentRawDataIndex);
                print($"End of posiiton + padding, padding = {pad0}", ConsoleColor.Blue, 2);
                print($"POSITIONS end at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Yellow, 2);
                currentRawDataIndex += pad0;
                #endregion

                #region NORMALS
                int p1 = 0;
                print($"NORMALS start at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Yellow, 2);
                for (int j = 0; j < lodMesh.batchInfo[i].positionsSize / 12; j++)
                {
                    meshBatchData.normals.Add(GetVectorAt(rawDataStart + currentRawDataIndex));
                    currentRawDataIndex += 12;
                    p1 += 12;
                }
                int pad1 = CalculatePadding(currentRawDataIndex);
                print($"End of normals + padding1, padding = {pad1}", ConsoleColor.Blue, 2);
                currentRawDataIndex += pad1;
                #endregion

                #region TEXCOORDS
                int p2 = 0;
                print($"TEXCOORDS start at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Yellow, 2);
                for (int j = 0; j < lodMesh.batchInfo[i].texCoordsSize / 8; j++)
                {
                    meshBatchData.texCoords.Add(GetUVAt(rawDataStart + currentRawDataIndex));
                    currentRawDataIndex += 8;
                    p2 += 8;
                }
                int pad2 = CalculatePadding(currentRawDataIndex);
                print($"End of texcoords + padding2, padding = {pad2}", ConsoleColor.Blue, 2);
                currentRawDataIndex += pad2;
                #endregion

                #region TRIANGLE
                int p3 = 0;
                print($"TRIANGLE start at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Yellow, 2);

                for (int j = 0; j < lodMesh.batchInfo[i].indicesSize / 8; j++)
                {
                    meshBatchData.triangles.Add(GetTriangleAt(rawDataStart + currentRawDataIndex));
                    currentRawDataIndex += 8;
                    p3 += 8;
                }
                int pad3 = CalculatePadding(currentRawDataIndex);
                print($"End of texcoords + padding2, padding = {pad3}", ConsoleColor.Blue, 2);
                currentRawDataIndex += pad3;
                #endregion

                #region PLANES
                int p4 = 0;
                print($"PLANES start at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Yellow, 2);

                if (lodMesh.flags.trianglePlanes)
                {


                    for (int j = 0; j < lodMesh.batchInfo[i].numTriangles; j++)
                    {
                        meshBatchData.planes.Add(GetPlaneAt(rawDataStart + currentRawDataIndex));
                        currentRawDataIndex += 16;
                        p4 += 16;
                    }
                    int pad4 = CalculatePadding(currentRawDataIndex);
                    print($"End of texcoords + padding2, padding = {pad4}", ConsoleColor.Blue, 2);
                    currentRawDataIndex += pad4;
                }
                #endregion
                print($"SAMEPOSVERTEXOFFSETS start at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Yellow, 2);
                #region SAMEPOSVERTEXOFFSETS
                int p5 = 0;
                for (int j = 0; j < lodMesh.batchInfo[i].samePosVertexOffsetSize / 2; j++)
                {
                    meshBatchData.samePosVertexOffsets.Add(GetInt16At(rawDataStart + currentRawDataIndex));
                    currentRawDataIndex += 2;
                    p5 += 2;
                }
                int pad5 = CalculatePadding(currentRawDataIndex);
                print($"End of SAMEPOSVERTEXOFFSETS + padding2, padding = {pad5}", ConsoleColor.Blue, 2);
                print($"SAMEPOSVERTEXOFFSETS ends at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Yellow, 2);
                currentRawDataIndex += pad5;

                #endregion

                #region BONELINKS
                int p6 = 0;
                if (lodMesh.batchInfo[i].boneLinksSize > 0)
                {
                    print($"Bonelinks starts at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Yellow, 2);
                    for (int j = 0; j < lodMesh.batchInfo[i].boneLinksSize / 8; j++)
                    {
                        meshBatchData.boneLinks.Add(GetVertexBoneAt(rawDataStart + currentRawDataIndex));
                        currentRawDataIndex += 8;
                        p6 += 8;
                    }
                    print($"Bonelinks end at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Yellow, 2);
                    int pad6 = CalculatePadding(currentRawDataIndex);
                    print($"End of BONELINKS + padding2, padding = {pad6}", ConsoleColor.Blue, 2);
                    currentRawDataIndex += pad6;
                }
                #endregion

                #region MORPHVERTSMAP
                print($"MORPHVERTSMAP starts at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Cyan, 2);
                int p7 = 0;
                if (lodMesh.flags.morphVerticesMap)
                {
                    for (int j = 0; j < lodMesh.numVertices; j++)
                    {
                        meshBatchData.morphVertsMap.Add(GetInt16At(rawDataStart + currentRawDataIndex));
                        currentRawDataIndex += 2;
                        p7 += 2;
                    }
                    int pad7 = CalculatePadding(currentRawDataIndex);

                    currentRawDataIndex += pad7;
                }
                print($"MORPHVERTSMAP ends at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")},,,{p7}bytes long", ConsoleColor.Cyan, 2);
                #endregion
                print($"meshBatchData ends at: 0x{(rawDataStart + currentRawDataIndex).ToString("X2")}", ConsoleColor.Yellow, 2);
                lodMeshData.batchData.Add(meshBatchData);
            }
            int padding1 = CalculatePadding(currentRawDataIndex);
            currentRawDataIndex += padding1;
            for (int i = 0; i < lodMesh.numPropPoints; i++)
            {
                lodMeshData.propPoints.Add(GetPropPointAt(rawDataStart + currentRawDataIndex));
            }
            print("Done reading LODMeshData.", ConsoleColor.Green, 3);

            return lodMeshData;
        }
        static LODMeshFlags GetNextLODMeshFlags()
        {
            LODMeshFlags flags = new LODMeshFlags();
            flags.unk80 = false;
            flags.unk40 = false;
            //FIX ME, V3M = true, V3C = false;
            if (filename.Split('.').Last() == "v3c")
                flags.trianglePlanes = false;
            flags.unk10 = false;
            flags.unk8 = false;
            flags.unk4 = false;
            flags.unk2 = false;
            if (filename.Split('.').Last() == "v3c")
                flags.morphVerticesMap = true;
            currentBytePos += 4;
            return flags;
        }
        static BatchInfo GetNextBatchInfo()
        {
            BatchInfo batchInfo = new BatchInfo();
            batchInfo.numVertices = GetNextInt16();
            batchInfo.numTriangles = GetNextInt16();
            batchInfo.positionsSize = GetNextInt16();
            batchInfo.indicesSize = GetNextInt16();
            batchInfo.samePosVertexOffsetSize = GetNextInt16();
            batchInfo.boneLinksSize = GetNextInt16();
            batchInfo.texCoordsSize = GetNextInt16();
            batchInfo.renderflags = GetNextInt32();

            return batchInfo;
        }
        static Texture GetNextTexture()
        {
            Texture texture = new Texture();
            texture.id = GetNextInt8();
            texture.fileName = GetNexStringUntilEOS();
            return texture;
        }
        static Material GetNextMaterial()
        {
            Material material = new Material();
            material.diffuseMapName = GetNexString(32, false);
            material.emissiveFactor = GetNextInt32();
            for (int i = 0; i < 2; i++)
            {
                material.unknown.Add(GetNextInt32());
            }
            material.refCof = GetNextInt32();
            material.refMapName = GetNexString(32, false);
            material.flags = GetNextInt32();
            return material;
        }

        #region GetDataTypes
        static int GetNextInt32()
        {
            byte[] myByteArray = new byte[] { fileBytes[currentBytePos], fileBytes[currentBytePos + 1], fileBytes[currentBytePos + 2], fileBytes[currentBytePos + 3] };
            UInt32 fromBytes = System.BitConverter.ToUInt32(myByteArray, 0);
            currentBytePos += 4;
            return (int)fromBytes;
        }
        static int GetInt32At(int start)
        {
            byte[] myByteArray = new byte[] { fileBytes[start], fileBytes[start + 1], fileBytes[start + 2], fileBytes[start + 3] };
            UInt32 fromBytes = System.BitConverter.ToUInt32(myByteArray, 0);
            return (int)fromBytes;
        }

        static int GetNextInt16()
        {
            byte[] myByteArray = new byte[] { fileBytes[currentBytePos], fileBytes[currentBytePos + 1] };
            UInt16 fromBytes = System.BitConverter.ToUInt16(myByteArray, 0);
            currentBytePos += 2;
            return (int)fromBytes;
        }
        static int GetInt16At(int start)
        {
            byte[] myByteArray = new byte[] { fileBytes[start], fileBytes[start + 1] };
            UInt16 fromBytes = System.BitConverter.ToUInt16(myByteArray, 0);
            return (int)fromBytes;
        }
        static int GetNextInt8()
        {
            int i = fileBytes[currentBytePos];
            currentBytePos += 1;
            return i;
        }

        static int GetInt8At(int start)
        {
            int i = fileBytes[start];
            return i;
        }
        static float GetNextFloat()
        {
            byte[] myByteArray = new byte[] { fileBytes[currentBytePos], fileBytes[currentBytePos + 1], fileBytes[currentBytePos + 2], fileBytes[currentBytePos + 3] };
            float fromBytes = System.BitConverter.ToSingle(myByteArray, 0);
            currentBytePos += 4;
            return fromBytes;
        }
        static float GetFloatAt(int start)
        {
            byte[] myByteArray = new byte[] { fileBytes[start], fileBytes[start + 1], fileBytes[start + 2], fileBytes[start + 3] };
            float fromBytes = System.BitConverter.ToSingle(myByteArray, 0);
            return fromBytes;
        }
        static Vector3 GetNextVector()
        {
            Vector3 vector3 = new Vector3();
            vector3.X = GetNextFloat();
            vector3.Y = GetNextFloat();
            vector3.Z = GetNextFloat();
            return vector3;
        }
        static Vector3 GetVectorAt(int start)
        {
            Vector3 vector3 = new Vector3();
            vector3.X = GetFloatAt(start);
            vector3.Y = GetFloatAt(start + 4);
            vector3.Z = GetFloatAt(start + 8);
            return vector3;
        }

        static Quat GetQuatrAt(int start)
        {
            Quat quat = new Quat();
            quat.X = GetFloatAt(start);
            quat.Y = GetFloatAt(start + 4);
            quat.Z = GetFloatAt(start + 8);
            quat.W = GetFloatAt(start + 12);
            return quat;
        }
        static UV GetUVAt(int start)
        {
            UV uv = new UV();
            uv.U = GetFloatAt(start);
            uv.V = GetFloatAt(start + 4);
            return uv;
        }

        static VertexBones GetVertexBoneAt(int start)
        {
            VertexBones vertexBones = new VertexBones();
            vertexBones.weights.Add(GetInt8At(start));
            vertexBones.weights.Add(GetInt8At(start + 1));
            vertexBones.weights.Add(GetInt8At(start + 2));
            vertexBones.weights.Add(GetInt8At(start + 3));

            vertexBones.bones.Add(GetInt8At(start + 4));
            vertexBones.bones.Add(GetInt8At(start + 5));
            vertexBones.bones.Add(GetInt8At(start + 6));
            vertexBones.bones.Add(GetInt8At(start + 7));
            return vertexBones;
        }
        static Triangle GetTriangleAt(int start)
        {
            Triangle triangle = new Triangle();
            triangle.indices.Add(GetInt16At(start));
            triangle.indices.Add(GetInt16At(start + 2));
            triangle.indices.Add(GetInt16At(start + 4));
            triangle.flags = GetInt16At(start + 6);
            return triangle;
        }
        static Plane GetPlaneAt(int start)
        {
            Plane plane = new Plane();
            plane.normal = GetVectorAt(start);
            plane.dist = GetFloatAt(start + 12);
            return plane;
        }
        static PropPoint GetPropPointAt(int start)
        {
            PropPoint propPoint = new PropPoint();
            propPoint.name = GetStringAt(start, 0x44, false);
            propPoint.rot = GetQuatrAt(start + 0x44);
            propPoint.pos = GetVectorAt(start + 0x44 + 16);
            propPoint.bone = GetInt32At(start + 0x44 + 16 + 12);
            return propPoint;
        }
        static bool GetNextBool()
        {
            byte[] myByteArray = new byte[] { fileBytes[currentBytePos], fileBytes[currentBytePos + 1], fileBytes[currentBytePos + 2], fileBytes[currentBytePos + 3] };
            bool fromBytes = System.BitConverter.ToBoolean(myByteArray, 0);
            currentBytePos += 4;
            print($"::{(bool)fromBytes}::", loglevel: 2);
            return fromBytes;
        }
        public static string GetNexString(int length, bool reverse = false)
        {
            string hex = "";
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = (fileBytes[currentBytePos + i]);
            }
            hex = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            currentBytePos += length;


            if (reverse)
            {
                return ReverseString(hex);
            }
            return hex;
        }
        public static string GetStringAt(int start, int length, bool reverse = false)
        {
            string hex = "";
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = (fileBytes[start + i]);
            }
            hex = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);

            if (reverse)
            {
                return ReverseString(hex);
            }
            return hex;
        }
        public static string GetNexStringUntilEOS()
        {
            string hex = "";
            int i = 0;
            while ((fileBytes[i + currentBytePos].ToString("X2") != "00"))
            {
                hex += (fileBytes[i + currentBytePos].ToString("X2"));
                i++;
            }
            i++;
            currentBytePos += i;
            return FromHexString(hex);
        }

        #endregion

        #region Helpers
        static string ReverseString(string text)
        {
            if (text == null) return null;

            char[] array = text.ToCharArray();
            Array.Reverse(array);
            return new string(array);
        }

        public static string FromHexString(string hexString)
        {
            if (hexString == null || (hexString.Length & 1) == 1)
            {
                throw new ArgumentException();
            }
            var sb = new StringBuilder();
            for (var i = 0; i < hexString.Length; i += 2)
            {
                var hexChar = hexString.Substring(i, 2);
                sb.Append((char)Convert.ToByte(hexChar, 16));
            }
            return sb.ToString();
        }

        public static int CalculatePadding(int currentOffsetInRawData)
        {
            int padding = 0;
            while (((currentOffsetInRawData + padding) % 16) != 0)
            {
                padding++;
            }
            return padding;
        }
        #endregion
        #region DEBUG
        static bool ReadArgs(string[] args)
        {
            if (args.Length == 0)
            {
                print("No file specified! Trying to read folder", ConsoleColor.Red);
                print("Usage: V3M-Converter filename.v3m, loglevel", ConsoleColor.Green);
                readDirectory = true;
                return true;
            }
            if (args[0] != null)
                if (!string.IsNullOrEmpty(args[0]))
                {
                    filename = args[0];
                }
            if (args.Length > 1)
                if (args[1] != null)
                    logLevel = int.Parse(args[1]);

            return true;


        }
        static void print(object message, ConsoleColor textColor = ConsoleColor.White, int loglevel = 0)
        {
            if (loglevel < logLevel)
                return;
            Console.ForegroundColor = textColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        #endregion
    }
}
