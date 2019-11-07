using System;
using OpenTK;
using SceneKit;
using UIKit;

namespace Sample.iOS.Controllers
{
    public class ThrowController
    {
        SixDegreesView sixDegreesView;

        public ThrowController(SixDegreesView sixDegreesView)
        {
            this.sixDegreesView = sixDegreesView;
        }

        public void ThrowBall()
        {
            // Make node and add it to the scene
            var ballNode = MakeBallNode();

            if (ballNode == null)
            {
                return;
            }

            var forcePower = 0.3f;
            var transform = sixDegreesView.CameraNode.Transform;
            var location = new SCNVector3(transform.M41, transform.M42, transform.M43);
            var orientation = new SCNVector3(-transform.M31, -transform.M32, -transform.M33);
            var position = location + orientation;

            //TODO Update throw
            //var throwPosition = MakeThrowPosition(transform);
            ballNode.Rotation = new SCNVector4(0, 1, 0, DegreeToRadian(90));
            ballNode.Position = position;
            ballNode.PhysicsBody.ApplyForce(new SCNVector3(orientation.X * forcePower, orientation.Y * forcePower, orientation.Z * forcePower) + new Vector3(0.0f, -0.1f, 0.0f), true);

            sixDegreesView.Scene.RootNode.AddChildNode(ballNode);
        }

        SCNNode MakeBallNode()
        {
            var sphere = SCNSphere.Create((nfloat)0.05);
            var sphereNode = SCNNode.Create();
            var material = new SCNMaterial();
            sphereNode.Geometry = sphere;
            material.Diffuse.Contents = MakeRandomColor();
            sphere.FirstMaterial = material;
            var shape = SCNPhysicsShape.Create(sphere);
            var body = SCNPhysicsBody.CreateBody(SCNPhysicsBodyType.Dynamic, shape);
            body.Restitution = (nfloat)0.1;
            body.Friction = (nfloat)0.9;
            body.Mass = (nfloat)0.1;
            sphereNode.PhysicsBody = body;

            return sphereNode;
        }

        UIColor MakeRandomColor()
        {
            return UIColor.Red;
        }

        private float DegreeToRadian(double angle)
        {
            return (float)(Math.PI * angle / 180.0);
        }

        SCNVector3 MakeThrowPosition(SCNMatrix4 transform)
        {
            var force = new Vector3(0.0f, 0.0f, -0.3f);
            var rotation = new SCNVector3(transform.M31, transform.M32, transform.M33);
            var rotatedForce = SCNVector3.Multiply(rotation, force) + new Vector3(0.0f, 0.1f, 0.0f);

            return new SCNVector3(rotatedForce);
        }
    }
}
