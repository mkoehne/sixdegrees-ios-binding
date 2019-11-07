using Sample.iOS.Controllers;
using SceneKit;
using System;
using UIKit;

namespace Sample.iOS
{
    public class SixDegreesView : UIView
    {
        public SCNScene Scene { get; set; }
        public SCNNode CameraNode { get; set; }
        public MeshController MeshController { get; set; }

        public SixDegreesView(IntPtr handle) : base(handle)
        {
            Scene = new SCNScene();
            CameraNode = MakeCameraNode();
            MeshController = new MeshController();
        }

        SCNNode MakeCameraNode()
        {
            var node = new SCNNode();
            node.Camera = new SCNCamera();
            return node;
        }
    }
}