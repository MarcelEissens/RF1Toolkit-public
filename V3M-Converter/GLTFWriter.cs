using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using System.Numerics;

using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Runtime;
using SharpGLTF.Schema2;
using System.IO;

namespace V3M_Converter
{
    using VERTEX = SharpGLTF.Geometry.VertexTypes.VertexPosition;
    public class GLTFWriter
    {
        public int logLevel;
        public string fileName = "";
        public Header header;
        public List<Section> sections;
        #region GLTF




        #endregion
        public void Init(Header fileHeader, List<Section> fileSections, int loglevel, string filename)
        {
            fileName = filename.Split('\\').Last();
            print("", loglevel: 99);
            print($"Initializing GLTF-Writer...{filename}", ConsoleColor.Red, 99);

            logLevel = loglevel;
            header = fileHeader;
            sections = fileSections;         
            print("GLTF-Writer initialized!.\n---", ConsoleColor.Green, 99);      
            ParseData();
            print("Done!", ConsoleColor.Red, 99);
            print($"File is saved in: ./Export//{fileName}.gltf");
        }

        public void ParseData()
        {
            var scene = new SharpGLTF.Scenes.SceneBuilder($"{fileName}");
            foreach (var item in sections)
            {
                print($"{item.type}::{item.body.name}::{item.body.lods.Count}LOD(S)::VERSION{item.body.version}", ConsoleColor.Blue, 99);
                if (item.type != "SUBM")
                    return;
                

                for (int i = 0; i < item.body.lods.Count; i++)
                {
                    LODMesh lodMesh = item.body.lods[i];
                    print($"LOD{i}", ConsoleColor.Yellow, 99);
                    print($"Textures count: {lodMesh.textures.Count.ToString()}", ConsoleColor.Blue, 99);
                    int tmpIdx = 0;
                    string meshName = item.body.name.Replace(" ","").Replace("  ","").Replace("\0","")+ "LOD" + i.ToString();
                    var mesh = new MeshBuilder<VertexPosition, VertexTexture1>(meshName);
                    foreach (MeshBatchData batchData in lodMesh.data.batchData)
                    {
                        MeshBatchHeader batchheader = lodMesh.data.batchHeaders[tmpIdx];
                       
                        List<Vector3> vertexPositions = batchData.positions;
                        List<Vector3> vertexNormals = batchData.normals;
                        List<UV> texCoords = batchData.texCoords;
                        List<Triangle> triangles = batchData.triangles;
                        List<Plane> planes = batchData.planes;
                        List<int> samePosVertexOffsets = batchData.samePosVertexOffsets;
                        List<VertexBones> boneLinks = batchData.boneLinks;

                        print($"Triangle planes: {lodMesh.flags.trianglePlanes}", ConsoleColor.Green, 99);
                        print($"{vertexPositions.Count} vertPositions", ConsoleColor.Green, 99);
                        print($"{triangles.Count} tris", ConsoleColor.Green, 99);
                        print($"{planes.Count} planes", ConsoleColor.Green, 99);


                        var material = new MaterialBuilder($"{lodMesh.textures[batchheader.textureIdx].fileName}").WithUnlitShader();

                        

                        var prim = mesh.UsePrimitive(material);
                        for (int j = 0; j < triangles.Count; j++)
                        {

                            Triangle tri = triangles[j];
                            System.Numerics.Vector3 x0 = new System.Numerics.Vector3(0, 0, 0);
                            System.Numerics.Vector3 y0 = new System.Numerics.Vector3(0, 0, 0);
                            System.Numerics.Vector3 z0 = new System.Numerics.Vector3(0, 0, 0);

                            VertexTexture1 cor1 = new VertexTexture1(new Vector2(0f, 0f));
                            VertexTexture1 cor2 = new VertexTexture1(new Vector2(0f, 1f));
                            VertexTexture1 cor3 = new VertexTexture1(new Vector2(1f, 1f));

                            int indice0 = tri.indices[0];
                            int indice1 = tri.indices[1];
                            int indice2 = tri.indices[2];
                            if (indice0< texCoords.Count)
                                cor1 = new VertexTexture1(new Vector2(texCoords[indice0].U, texCoords[indice0].V));
                            if (indice1< texCoords.Count)
                                cor2 = new VertexTexture1(new Vector2(texCoords[indice1].U, texCoords[indice1].V));
                            if (indice2< texCoords.Count)
                                cor3 = new VertexTexture1(new Vector2(texCoords[indice2].U, texCoords[indice2].V));

                            x0 = V3mV3ToSystemV3(vertexPositions[indice0]);
                            y0 = V3mV3ToSystemV3(vertexPositions[indice1]);
                            z0 = V3mV3ToSystemV3(vertexPositions[indice2]);
                            
                            
                            VERTEX x = new VERTEX(x0);
                            VERTEX y = new VERTEX(y0);
                            VERTEX z = new VERTEX(z0);


                            VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty> ver1 = new VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>(x, cor1);
                            VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty> ver2 = new VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>(y, cor2);
                            VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty> ver3 = new VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>(z, cor3);
                            
                            prim.AddTriangle(ver1, ver2, ver3);
                        }
                        

                        tmpIdx++;
                    }
                    print($"MESH NAME: {mesh.Name}", ConsoleColor.Green, 99);
                    scene.AddRigidMesh(mesh, Matrix4x4.Identity);

                    for (int p = 0; p < lodMesh.data.propPoints.Count; p++)
                    {
                        PropPoint prpPoint = lodMesh.data.propPoints[p];
                        SharpGLTF.Scenes.NodeBuilder pPoint = new SharpGLTF.Scenes.NodeBuilder($"{prpPoint.name}LOD{i}").WithLocalTranslation(V3mV3ToSystemV3(prpPoint.pos)).WithLocalRotation(V3mQuatToSystemQuat(prpPoint.rot));
                        scene.AddNode(pPoint);
                    }
                    
                }
                
            }
            var model = scene.ToGltf2();
            
           
            print($"SavingFileAs {fileName.Split('\\').Last()}", ConsoleColor.White, 99);
            string output = fileName;
            model.SaveGLTF($"Export//{output}.gltf");
            
            

        }

        public System.Numerics.Vector3 V3mV3ToSystemV3(Vector3 input)
        {
            System.Numerics.Vector3 vec3 = new System.Numerics.Vector3();
            vec3.X = input.X;
            vec3.Y = input.Y;
            vec3.Z = input.Z;
            return vec3;
        }

        public System.Numerics.Quaternion V3mQuatToSystemQuat(Quat input)
        {
            System.Numerics.Quaternion q = new System.Numerics.Quaternion();
            q.X = input.X;
            q.Y = input.Y;
            q.Z = input.Z;
            q.W = input.W;
            return q;
        }
        public System.Numerics.Vector3 FloatArrToSystemV3(float x, float y, float z)
        {
            System.Numerics.Vector3 vec3 = new System.Numerics.Vector3();
            vec3.X = x;
            vec3.Y = y;
            vec3.Z = z;
            return vec3;
        }
        void print(object message, ConsoleColor textColor = ConsoleColor.White, int loglevel = 0)
        {
            if (loglevel < logLevel)
                return;
            Console.ForegroundColor = textColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}

//REFERENCE FOR BUILDING MESH WITH TRIS AND MATERIAL AND UV'S
/*
 * var material = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelImage(KnownChannel.BaseColor, "D:\\New folder\\bricks.png");

            var mesh = new MeshBuilder<VertexPosition, VertexTexture1>("Mesh");


            VertexTexture1 cor1 = new VertexTexture1(new Vector2(0f, 0f));
            VertexTexture1 cor2 = new VertexTexture1(new Vector2(0f, 2f));
            VertexTexture1 cor3 = new VertexTexture1(new Vector2(2f, 2f));
            VertexTexture1 cor4 = new VertexTexture1(new Vector2(2f, 0f));
            


            VertexPosition pos1 = new VertexPosition(0, 0, 0);
            VertexPosition pos2 = new VertexPosition(0, 1, 0);
            VertexPosition pos3 = new VertexPosition(1, 1, 0);
            VertexPosition pos4 = new VertexPosition(1, 0, 0);


            VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty> ver1 = new VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>(pos1, cor2);
            VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty> ver2 = new VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>(pos2, cor1);
            VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty> ver3 = new VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>(pos3, cor4);
            VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty> ver4 = new VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>(pos4, cor3);

            var prim = mesh.UsePrimitive(material);


            prim.AddTriangle(ver1, ver2, ver3);
            prim.AddTriangle(ver1, ver3, ver4);



            var scene = new SharpGLTF.Scenes.SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            var model = scene.ToGltf2();
            

            model.SaveGLTF(@"D:\\New folder\\File.gltf");
 */ 
