using Foundation;
using Sample.iOS.Controllers;
using SceneKit;
using SixDegrees.iOS;
using System;
using System.Text;
using UIKit;

namespace Sample.iOS
{
    public partial class ViewController : UIViewController
    {
        bool showDebug = false;
        ThrowController throwController;
        Int64 saveTime = 0;
        Int64 loadTime = 0;

        public int saveState = (int)SDPlugin.SDSaveState.None;
        public int loadState = (int)SDPlugin.SDLoadState.None;
        public int saveError = (int)SDPlugin.SDSaveError.None;
        public int loadError = (int)SDPlugin.SDLoadError.None;
        public long uploadSize = -1;
        public float uploadProgress = -1;
        public long downloadSize = -1;
        public float downloadProgress = -1;
        public bool isStopped = false;

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            throwController = new ThrowController(this.sixDegreesView);
            ConfigureController();
            ConfigureLights();

            meshButton.TouchUpInside += (sender, e) =>
            {
                meshButton.Selected = !meshButton.Selected;

                if (meshButton.Selected)
                {
                    sixDegreesView.MeshController.ShowMesh();
                }
                else
                {
                    sixDegreesView.MeshController.HideMesh();
                }

                SDPlugin.ShowMesh = meshButton.Selected;
            };

            loadButton.TouchUpInside += (sender, e) =>
            {
                if (SDPlugin.IsSDKReady)
                {
                    loadTime = SDPlugin.SixDegreesSDK_LoadFromARCloud();
                }
            };

            saveButton.TouchUpInside += (sender, e) =>
            {
                if (SDPlugin.IsSDKReady)
                {
                    saveTime = SDPlugin.SixDegreesSDK_SaveToARCloud();
                }
            };

            var updateTimer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromSeconds(0.2), delegate
            {
                UpdateDebug();
            });
            updateTimer.Fire();
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        void ConfigureController()
        {
            UIApplication.SharedApplication.IdleTimerDisabled = true;

            // Add gesture recognizer
            UITapGestureRecognizer tapRecongizer = new UITapGestureRecognizer(() =>
            {
                throwController.ThrowBall();
            });

            View.AddGestureRecognizer(tapRecongizer);
        }

        private float DegreeToRadian(double angle)
        {
            return (float)(Math.PI * angle / 180.0);
        }

        void ConfigureLights()
        {
            var directionalLightNode = MakeDirectionalLight(800);
            directionalLightNode.EulerAngles = new SCNVector3(DegreeToRadian(-80), DegreeToRadian(10), DegreeToRadian(0));
            sixDegreesView.Scene.RootNode.AddChildNode(directionalLightNode);

            var ambiantLightNode = MakeAmbiantLight(300);
            sixDegreesView.Scene.RootNode.AddChildNode(ambiantLightNode);
        }

        SCNNode MakeDirectionalLight(float intensity)
        {
            var light = new SCNLight();
            light.LightType = SCNLightType.Directional;
            light.Color = UIColor.White;
            light.Intensity = intensity;
            var node = new SCNNode();
            node.Light = light;
            return node;
        }

        SCNNode MakeAmbiantLight(float intensity)
        {
            var light = new SCNLight();
            light.LightType = SCNLightType.Ambient;
            light.Color = UIColor.White;
            light.Intensity = intensity;
            var node = new SCNNode();
            node.Light = light;
            return node;
        }

        void UpdateStatus()
        {
            saveState = (int)SDPlugin.SDSaveState.None;

            if (SDPlugin.IsSDKReady && (saveTime > 0 || loadTime > 0))
            { }
            else { return; }

            var status = "";
            if (saveTime > loadTime)
            {
                unsafe
                {
                    fixed (int* saveStatePtr = &saveState, saveErrorPtr = &saveError)
                    {
                        fixed (long* uploadSizePtr = &uploadSize)
                        {
                            fixed (float* uploadProgessPtr = &uploadProgress)
                            {
                                SDPlugin.SixDegreesSDK_GetSaveStatus(saveTime, saveStatePtr, saveErrorPtr, uploadSizePtr, uploadProgessPtr);
                            }
                        }
                    }
                }
            }

            StringBuilder sb = new StringBuilder(16);
            SDPlugin.SixDegreesSDK_GetLocationId(sb, sb.Capacity);
            SDPlugin.LocationID = sb.ToString();
        }

        void UpdateDebug()
        {
            if (SDPlugin.IsSDKReady)
            { }
            else { return; }

            float[] pose = new float[16];
            int mTrackingState = 0;
            unsafe
            {
                fixed (float* ptr = &pose[0])
                {
                    // R T
                    // 0 1
                    int bufferSize = 16;
                    mTrackingState = SDPlugin.SixDegreesSDK_GetPose(ptr, bufferSize);
                }

            }
            var angle = Math.Atan2(pose[8], pose[0]);

            string quality = "";

            switch (mTrackingState)
            {
                case 0:
                    quality = "None";
                    break;
                case 1:
                    quality = "Limited";
                    break;
                case 2:
                    quality = "Good";
                    break;
                default:
                    break;
            }
            statusLabel.Text = ("Tracking: " + quality + "\nX: " + Math.Round(pose[12], 2).ToString() + ", Y: " + Math.Round(pose[13], 2).ToString() + ", Z: " + Math.Round(pose[14], 2).ToString() + "\nHeading: " + Math.Round(ConvertRadiansToDegrees(angle), 2).ToString());
        }

        public static double ConvertRadiansToDegrees(double radians)
        {
            double degrees = (180 / Math.PI) * radians;
            return (degrees);
        }

        unsafe void UpdatePose()
        {
            float[] mPoseBuffer = new float[16];
            int mTrackingState = 0;

            fixed (float* ptr = &mPoseBuffer[0])
            {
                // R T
                // 0 1
                int bufferSize = 16;
                mTrackingState = SDPlugin.SixDegreesSDK_GetPose(ptr, bufferSize);
            }

            switch (mTrackingState)
            {
                case (int)SDPlugin.SDTrackingQuality.Good:
                case (int)SDPlugin.SDTrackingQuality.Limited:
                    {
                        if (mTrackingState > 0)
                        {
                            // Update camera pose
                            var row0 = new SCNVector4(mPoseBuffer[0], mPoseBuffer[1], mPoseBuffer[2], mPoseBuffer[3]);
                            var row1 = new SCNVector4(mPoseBuffer[4], mPoseBuffer[5], mPoseBuffer[6], mPoseBuffer[7]);
                            var row2 = new SCNVector4(mPoseBuffer[8], mPoseBuffer[9], mPoseBuffer[10], mPoseBuffer[11]);
                            var row3 = new SCNVector4(mPoseBuffer[12], mPoseBuffer[13], mPoseBuffer[14], mPoseBuffer[15]);
                            SCNMatrix4 poseMatrix = new SCNMatrix4(row0, row1, row2, row3);

                        }

                        break;
                    }
                case (int)SDPlugin.SDTrackingQuality.None:
                default:
                    {
                        break;
                    }
            }
        }
    }
}