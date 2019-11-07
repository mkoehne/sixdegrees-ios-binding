using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Foundation;
using Sample.iOS.Helpers;
using SceneKit;
using SixDegrees.iOS;

namespace Sample.iOS.Controllers
{
    public class MeshController
    {
        public SCNNode MeshNode
        {
            get; set;
        }

        private SCNNode MakeMeshNode()
        {
            var node = new SCNNode();
            node.RenderingOrder = -1;
            return node;
        }

        public SCNMaterial MeshMaterial
        {
            get; set;
        }

        private SCNMaterial MakeMeshMaterial()
        {
            var material = new SCNMaterial();
            material.LightingModelName = SCNLightingModel.Constant;
            material.LitPerPixel = false;
            material.DoubleSided = true;
            material.ShaderModifiers = new SCNShaderModifiers() { EntryPointSurface = SurfaceShaderModifier, EntryPointGeometry = "#pragma transparent" };
            return material;
        }

        public string SurfaceShaderModifier { get; set; }

        public float BlockSize = 0; // in meters
        public Dictionary<Vector3, MeshBlock> Blocks;
        public int MeshVersion = -1;

        public MeshController()
        {
            Blocks = new Dictionary<Vector3, MeshBlock>();
            MeshNode = MakeMeshNode();
            MeshMaterial = MakeMeshMaterial();
        }

        public void Update()
        {
            // Get new mesh version and buffer sizes
            int blockBufferSize = 0;
            int vertexBufferSize = 0;
            int faceBufferSize = 0;
            unsafe
            {
                var newVersion = SDPlugin.SixDegreesSDK_GetBlockMeshInfo(&blockBufferSize, &vertexBufferSize, &faceBufferSize);

                if (newVersion > MeshVersion)
                {
                    if (blockBufferSize > 0 &&
                        vertexBufferSize > 0 &&
                        faceBufferSize > 0)
                    { }
                    else
                    {
                        return;
                    }

                    if (MeshVersion < 0)
                    {
                        BlockSize = SDPlugin.SixDegreesSDK_GetMeshBlockSize();
                    }
                    UpdateMesh(newVersion: newVersion, blockBufferSize: blockBufferSize, vertexBufferSize: vertexBufferSize, faceBufferSize: faceBufferSize);
                }
                else if (newVersion == 0 &&
                    MeshVersion > 0)
                {
                    ClearMesh();
                }
            }
        }

        void ClearMesh()
        {
            MeshVersion = 0;
        }

        public void ShowMesh()
        {
            MeshMaterial.ShaderModifiers = new SCNShaderModifiers() { EntryPointSurface = SurfaceShaderModifier, EntryPointGeometry = "#pragma transparent" };
        }

        public void HideMesh()
        {
            MeshMaterial.ShaderModifiers = new SCNShaderModifiers() { EntryPointSurface = "_surface.transparent.a = 0.0;", EntryPointGeometry = "#pragma transparent" };
        }

        MeshBlock GetOrCreateBlock(Vector3 blockCoords)
        {
            if (Blocks.Keys.Contains(blockCoords))
            {
                var block = Blocks[blockCoords];
                return block;
            }
            else
            {
                var newBlock = new MeshBlock();
                newBlock.Coordinates = blockCoords;
                Blocks.Add(blockCoords, newBlock);
                return newBlock;
            }
        }

        void UpdateMesh(int newVersion, int blockBufferSize, int vertexBufferSize, int faceBufferSize)
        {
            MeshVersion = newVersion;

            int[] blockArray = new int[blockBufferSize];
            float[] vertexArray = new float[vertexBufferSize];
            int[] faceArray = new int[faceBufferSize];

            int fullBlocks = 0;

            unsafe
            {
                fixed (int* blockBufferPtr = &blockArray[0], faceBufferPtr = &faceArray[0])
                {
                    fixed (float* vertexBufferPtr = &vertexArray[0])
                    {
                        fullBlocks = SDPlugin.SixDegreesSDK_GetBlockMesh(blockBufferPtr, vertexBufferPtr, faceBufferPtr, blockBufferSize, vertexBufferSize, faceBufferSize);
                    }
                }
            }
            bool gotAllBlocks = (fullBlocks == blockBufferSize / 6);

            if (fullBlocks > 0)
            { }
            else
            {
                Console.WriteLine("SixDegreesSDK_GetMeshBlocks() gave us an empty mesh, will not update.");
                return;
            }

            if (!gotAllBlocks)
            {
                Console.WriteLine("SixDegreesSDK_GetMeshBlocks() returned %d full blocks, expected %d, will not update", fullBlocks, (blockBufferSize / 6));
                return;
            }

            var vertexCount = vertexBufferSize / 6;
            var blocksToUpdate = new List<Vector3>();

            var firstBlockVertex = 0;
            var firstBlockFace = 0;

            // Update all the full blocks returned by the API
            for (int b = 0; b < blockBufferSize; b += 6)
            {
                // Transform block coordinates from 6D right-handed coordinates to Unity left-handed coordinates
                // By flipping the sign of Z
                Vector3 blockCoords = new Vector3(blockArray[b], blockArray[b + 1], blockArray[b + 2]);
                int blockVertexCount = blockArray[b + 3];
                int blockFaceCount = blockArray[b + 4];
                int blockVersion = blockArray[b + 5];

                var block = GetOrCreateBlock(blockCoords);
                block.MeshVersion = MeshVersion;

                // Update block if it is outdated
                if (block.Version < blockVersion)
                {
                    blocksToUpdate.Add(blockCoords);
                    block.Version = blockVersion;
                    block.Vertices = new List<SCNVector3>();
                    block.Normals = new List<SCNVector3>();
                    block.Faces = new List<int>();

                    // copy vertices
                    for (int j = firstBlockVertex; j < firstBlockVertex + blockVertexCount; j++)
                    {
                        var vertex = j;
                        var pos = vertex * 3;
                        block.Vertices.Add(new SCNVector3(vertexArray[pos],
                                                         vertexArray[pos + 1],
                                                         vertexArray[pos + 2]));

                        var norm = (vertex + vertexCount) * 3;
                        block.Normals.Add(new SCNVector3(vertexArray[norm],
                                                        vertexArray[norm + 1],
                                                        vertexArray[norm + 2]));
                    }

                    // copy faces
                    var offset = firstBlockVertex;
                    for (int face = firstBlockFace; face < firstBlockFace + blockFaceCount; face++)
                    {
                        var f = face * 3;
                        block.Faces.Add(faceArray[f] - offset);
                        block.Faces.Add(faceArray[f + 1] - offset);
                        block.Faces.Add(faceArray[f + 2] - offset);
                    }
                }
                firstBlockVertex += blockVertexCount;
                firstBlockFace += blockFaceCount;
            }

            var blocksToDelete = new List<Vector3>();

            // Clean up outdated blocks
            foreach (var item in Blocks.Values)
            {
                if (item.MeshVersion != MeshVersion)
                {
                    blocksToDelete.Add(item.Coordinates);
                }
            }

            DeleteBlocks(blocksToDelete);
            UpdateBlocks(blocksToUpdate);
        }

        public SCNGeometry MakeMeshGeometry(SCNVector3[] vertices, SCNVector3[] normals, List<int> faces)
        {
            var geometrySources = new[] { SCNGeometrySource.FromVertices(vertices), SCNGeometrySource.FromNormals(normals) };
            var array = faces.SelectMany(v => BitConverter.GetBytes(v)).ToArray();
            var indexData = NSData.FromArray(array);
            var elementSource = SCNGeometryElement.FromData(indexData, SCNGeometryPrimitiveType.Triangles, (nint)(faces.Count / 3), (nint)sizeof(int));
            var geometry = SCNGeometry.Create(geometrySources, new[] { elementSource });
            geometry.WantsAdaptiveSubdivision = false;

            return geometry;
        }

        public void UpdateBlock(Vector3 blockToUpdate)
        {
            if (!Blocks.ContainsKey(blockToUpdate))
            {
                return;
            }

            var block = Blocks[blockToUpdate];

            if (block.MeshVersion != MeshVersion || block.Vertices.Count <= 0 || block.Faces.Count <= 0)
            {
                DeleteBlock(blockToUpdate);
            }
            else
            {
                if (block.Node == null)
                {
                    block.Node = MakeMeshNode();
                    block.Node.Name = $"Mesh Block {block.Coordinates.X},{block.Coordinates.Y},{block.Coordinates.Z}";
                    MeshNode.AddChildNode(block.Node);
                }

                var meshGeometry = MakeMeshGeometry(block.Vertices.ToArray(), block.Normals.ToArray(), block.Faces);
                meshGeometry.FirstMaterial = MeshMaterial;

                block.Node.Geometry = meshGeometry;

                // update physics less often as it's costly and not as critical
                if (block.Node.PhysicsBody == null || block.PhysicsVersion + 10 < block.Version)
                {
                    block.Node.PhysicsBody = MakePhysicsBody(meshGeometry);
                    block.PhysicsVersion = block.Version;
                }
            }
        }

        public SCNPhysicsBody MakePhysicsBody(SCNGeometry geometry)
        {
            var option = new SCNPhysicsShapeOptions();
            option.KeepAsCompound = true;
            option.ShapeType = SCNPhysicsShapeType.ConcavePolyhedron;

            var shape = SCNPhysicsShape.Create(geometry, option);
            return SCNPhysicsBody.CreateBody(type: SCNPhysicsBodyType.Static, shape);
        }

        public void UpdateBlocks(List<Vector3> blocksToUpdate)
        {
            foreach (var blockCoords in blocksToUpdate)
            {
                UpdateBlock(blockCoords);
            }
        }

        public void DeleteBlocks(List<Vector3> blocksToDelete)
        {
            foreach (var blockCoords in blocksToDelete)
            {
                DeleteBlock(blockCoords);
            }
        }

        public void DeleteBlock(Vector3 blockToDelete)
        {
            var block = Blocks[blockToDelete];

            if (block != null)
            {
                block.Node.RemoveFromParentNode();
            }

            Blocks.Remove(blockToDelete);
        }
    }
}

