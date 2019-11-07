using System.Collections.Generic;
using System.Runtime.InteropServices;
using CoreGraphics;
using UIKit;

namespace Sample.iOS.Helpers
{
    [StructLayout(LayoutKind.Sequential)]
    public class VertexFactory
    {
        public List<Vertex> Vertices;

        public bool IsComplete
        {
            get
            {
                if (Vertices == null || Vertices.Count == 0)
                {
                    return false;
                }

                return true;
            }
        }

        public VertexFactory()
        {
            Vertices = new List<Vertex>();
        }

        public void Update(UIInterfaceOrientation orientation, CGSize size)
        {
            var v1 = new Vertex();
            var v2 = new Vertex();
            var v3 = new Vertex();
            var v4 = new Vertex();

            switch (orientation)
            {
                case UIInterfaceOrientation.Portrait:
                    v1 = new Vertex(1, -1, 1, 0);
                    v2 = new Vertex(x: -1, y: -1, s: 1, t: 1);
                    v3 = new Vertex(x: 1, y: 1, s: 0, t: 0);
                    v4 = new Vertex(x: -1, y: 1, s: 0, t: 1);
                    break;

                case UIInterfaceOrientation.PortraitUpsideDown:
                    v1 = new Vertex(x: 1, y: -1, s: 0, t: 1);
                    v2 = new Vertex(x: -1, y: -1, s: 0, t: 0);
                    v3 = new Vertex(x: 1, y: 1, s: 1, t: 1);
                    v4 = new Vertex(x: -1, y: 1, s: 1, t: 0);
                    break;

                case UIInterfaceOrientation.LandscapeLeft:
                    v1 = new Vertex(x: 1, y: -1, s: 0, t: 0);
                    v2 = new Vertex(x: -1, y: -1, s: 1, t: 0);
                    v3 = new Vertex(x: 1, y: 1, s: 0, t: 1);
                    v4 = new Vertex(x: -1, y: 1, s: 1, t: 1);
                    break;

                case UIInterfaceOrientation.LandscapeRight:
                    v1 = new Vertex(x: 1, y: -1, s: 1, t: 1);
                    v2 = new Vertex(x: -1, y: -1, s: 0, t: 1);
                    v3 = new Vertex(x: 1, y: 1, s: 1, t: 0);
                    v4 = new Vertex(x: -1, y: 1, s: 0, t: 0);
                    break;
                default:
                    break;
            }

            Vertices.Add(v1);
            Vertices.Add(v2);
            Vertices.Add(v3);
            Vertices.Add(v4);
        }

        public int Size()
        {
            if (Vertices.Count > 0)
            { }
            else
            {
                return 0;
            }
            return 4 * Marshal.SizeOf(Vertices[0]);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct Vertex
    {
        public float X;
        public float Y;

        // Texture coordinates
        public float S;
        public float T;

        public Vertex(float x, float y, float s, float t)
        {
            X = x;
            Y = y;
            S = s;
            T = t;
        }
    }
}
