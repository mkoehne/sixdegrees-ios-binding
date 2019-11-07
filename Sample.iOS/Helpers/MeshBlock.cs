using System.Collections.Generic;
using System.Numerics;
using SceneKit;

namespace Sample.iOS.Helpers
{
    public class MeshBlock
    {
        public Vector3 Coordinates = new Vector3(0, 0, 0);
        public List<SCNVector3> Vertices = new List<SCNVector3>();
        public List<SCNVector3> Normals = new List<SCNVector3>();
        public List<int> Faces = new List<int>();
        public int Version = -1;
        public int MeshVersion = -1;
        public int PhysicsVersion = -1;
        public SCNNode Node { get; set; }
    }
}
