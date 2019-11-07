using CoreAnimation;
using CoreGraphics;
using Foundation;
using Metal;
using MetalKit;
using OpenTK;
using Sample.iOS.Helpers;
using SceneKit;
using SixDegrees.iOS;
using System;
using System.Runtime.InteropServices;
using UIKit;

namespace Sample.iOS
{
    public partial class SixDegreesMetalView : SixDegreesView, IMTKViewDelegate
    {
        // UI
        MTKView mtkView;

        // Variables
        IMTLDevice device;

        IMTLRenderPipelineState renderPipelineState;
        IMTLCommandQueue commandQueue;

        SCNRenderer renderer;

        CGSize currentSize = CGSize.Empty;
        CGSize backgroundTextureSize;

        UIInterfaceOrientation orientation = UIApplication.SharedApplication.StatusBarOrientation;
        VertexFactory vertexFactory = new VertexFactory();

        // Flags
        bool isInitialized = false;

        public SixDegreesMetalView(IntPtr handle) : base(handle)
        {
            renderer = MakeRenderer();
            MakeMTKView();
            device = MTLDevice.SystemDefault;
            CommonInit();
        }

        void CommonInit()
        {
            // Configure everything
            ConfigureView();
            ConfigureMeshController();
            ConfigureMetal();

            // Initialize 6D SDK
            SDPlugin.SixDegreesSDK_Initialize();

            MeshController.ShowMesh();
        }

        void ConfigureMetal()
        {
            var library = device.CreateDefaultLibrary();
            if (library == null)
            {
                return;
            }
            MTLRenderPipelineDescriptor pipelineDescriptor = new MTLRenderPipelineDescriptor();
            pipelineDescriptor.SampleCount = 1;
            pipelineDescriptor.ColorAttachments[0].PixelFormat = MTLPixelFormat.BGRA8Unorm;
            pipelineDescriptor.DepthAttachmentPixelFormat = MTLPixelFormat.Invalid;
            pipelineDescriptor.VertexFunction = library.CreateFunction("simpleVertex");
            pipelineDescriptor.FragmentFunction = library.CreateFunction("simpleTexture");

            NSError error1 = null;
            renderPipelineState = device.CreateRenderPipelineState(pipelineDescriptor, out error1);

            if (error1 != null)
            {
                return;
            }
            commandQueue = device.CreateCommandQueue();
        }

        void ConfigureMeshController()
        {
            MeshController.SurfaceShaderModifier =
                "constexpr float transparency = 0.4;" +
                "constexpr float nearDist = 0.4;" +
                "constexpr float farDist = 3.4;" +
                "constexpr float slope = 1.0 / (farDist - nearDist);" +
                "float4 surfaceNormal = float4(_surface.normal, 0.0f);" +
                "float4 normal = scn_frame.inverseViewTransform * surfaceNormal;" +
                "float alpha = min(1.0, max(1.0 + nearDist * slope + _surface.position.z * slope, 0.0));" +
                "_surface.diffuse.rgb = abs(normal.xyz);" +
                "_surface.transparent.a = alpha * transparency;";
        }

        void ConfigureView()
        {
            BackgroundColor = UIColor.Black;
            ClipsToBounds = true;

            // Add child nodes to the scene
            Scene.RootNode.AddChildNode(MeshController.MeshNode);
            Scene.RootNode.AddChildNode(CameraNode);

            // Set renderer scene and pointOfView
            renderer.Scene = Scene;
            renderer.PointOfView = CameraNode;

            // Update mtkView
            mtkView.Device = device;
            mtkView.WeakDelegate = this;

            AddSubview(mtkView);
            SendSubviewToBack(mtkView);
        }

        MTKView MakeMTKView()
        {
            mtkView = new MTKView(this.Frame, MTLDevice.SystemDefault);
            mtkView.BackgroundColor = UIColor.Blue;
            mtkView.ColorPixelFormat = MTLPixelFormat.BGRA8Unorm;
            mtkView.DepthStencilPixelFormat = MTLPixelFormat.Invalid;
            mtkView.EnableSetNeedsDisplay = false;
            return mtkView;
        }

        SCNRenderer MakeRenderer()
        {
            var renderer = SCNRenderer.FromDevice(device, null);
            renderer.AutoenablesDefaultLighting = true;
            return renderer;
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            if (currentSize != Frame.Size)
            {
                currentSize = Frame.Size;
                UpdateMTKViewFrame();
            }
        }

        public void DrawableSizeWillChange(MTKView view, CGSize size)
        {
        }

        CGSize MakeBackgroundTextureSize()
        {
            int width = 0;
            int height = 0;
            unsafe
            {
                SDPlugin.SixDegreesSDK_GetBackgroundTextureSize(&width, &height);
            }
            if (width > 0 && height > 0)
            {
                return new CGSize(width, height);
            }
            else
            {
                return CGSize.Empty;
            }
        }

        void UpdateMTKViewFrame()
        {
            var localBackgroundTextureSize = backgroundTextureSize;

            float aspect;

            if (Frame.Width > Frame.Height)
            {
                aspect = (float)(localBackgroundTextureSize.Width / localBackgroundTextureSize.Height);
            }
            else
            {
                aspect = (float)(localBackgroundTextureSize.Height / localBackgroundTextureSize.Width);
            }

            var targetWidth = Frame.Height * aspect;

            if (localBackgroundTextureSize.Height > 0 && localBackgroundTextureSize.Width > 0)
            {
                if (targetWidth < Frame.Width)
                {
                    // Aspect fill to fit width
                    var height = Frame.Width / aspect;
                    var originY = (Frame.Height - height) / 2;
                    mtkView.Frame = new CGRect(x: 0, y: originY, width: Frame.Width, height: height);
                }
                else
                {
                    // Aspect fill to fit height
                    var width = Frame.Height * aspect;
                    var originX = (Frame.Width - width) / 2;
                    mtkView.Frame = new CGRect(x: originX, y: 0, width: width, height: Frame.Height);
                }
            }
        }

        SCNMatrix4 MakeProjectionMatrix(UIInterfaceOrientation orientation)
        {
            const int bufferSize = 16;
            float[] projectionBuffer = new float[bufferSize];

            unsafe
            {
                fixed (float* ptr = &projectionBuffer[0])
                {
                    SDPlugin.SixDegreesSDK_GetProjection(ptr, bufferSize);
                }
            }

            var matrix = new Matrix4
            {
                Row0 = new Vector4(projectionBuffer[0], projectionBuffer[1], projectionBuffer[2], projectionBuffer[3]),
                Row1 = new Vector4(projectionBuffer[4], projectionBuffer[5], projectionBuffer[6], projectionBuffer[7]),
                Row2 = new Vector4(projectionBuffer[8], projectionBuffer[9], projectionBuffer[10], projectionBuffer[11]),
                Row3 = new Vector4(projectionBuffer[12], projectionBuffer[13], projectionBuffer[14], projectionBuffer[15])
            };

            var rotation = MakeInterfaceRotationRadians(orientation);

            var matrixRotated = CreateMatrixFromRotation(rotation, 0, 0, 1);
            var newMatrix = matrix * matrixRotated;
            SCNMatrix4 poseMatrix = new SCNMatrix4(newMatrix.Row0, newMatrix.Row1, newMatrix.Row2, newMatrix.Row3);

            return poseMatrix;
        }

        float MakeInterfaceRotationRadians(UIInterfaceOrientation orientation)
        {
            var angle = 0;
            switch (orientation)
            {
                case UIInterfaceOrientation.Portrait:
                    angle = -90;
                    break;
                case UIInterfaceOrientation.PortraitUpsideDown:
                    angle = -270;
                    break;
                case UIInterfaceOrientation.LandscapeLeft:
                    angle = -180;
                    break;
                default:
                    break;
            }

            return DegreeToRadian(angle);
        }

        Matrix4 CreateMatrixFromRotation(float radians, float x, float y, float z)
        {
            Vector3 v = Vector3.Normalize(new Vector3(x, y, z));
            var cos = (float)Math.Cos(radians);
            var sin = (float)Math.Sin(radians);
            float cosp = 1.0f - cos;

            var m = new Matrix4
            {
                Row0 = new Vector4(cos + cosp * v.X * v.X, cosp * v.X * v.Y - v.Z * sin, cosp * v.X * v.Z + v.Y * sin, 0.0f),
                Row1 = new Vector4(cosp * v.X * v.Y + v.Z * sin, cos + cosp * v.Y * v.Y, cosp * v.Y * v.Z - v.X * sin, 0.0f),
                Row2 = new Vector4(cosp * v.X * v.Z - v.Y * sin, cosp * v.Y * v.Z + v.X * sin, cos + cosp * v.Z * v.Z, 0.0f),
                Row3 = new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
            };

            return m;
        }

        private float DegreeToRadian(double angle)
        {
            return (float)(Math.PI * angle / 180.0);
        }

        void Draw(IMTLTexture texture)
        {
            GCHandle pinnedTransform = GCHandle.Alloc(vertexFactory.Vertices.ToArray(), GCHandleType.Pinned);
            IntPtr ptr = pinnedTransform.AddrOfPinnedObject();
            pinnedTransform.Free();

            var rstate = renderPipelineState;
            var commandBuffer = commandQueue.CommandBuffer();
            var currentRenderPassDescriptor = mtkView.CurrentRenderPassDescriptor;
            var encoder = commandBuffer.CreateRenderCommandEncoder(currentRenderPassDescriptor);

            encoder.PushDebugGroup("RenderFrame");
            encoder.SetRenderPipelineState(rstate);
            encoder.SetVertexBytes(ptr, (nuint)vertexFactory.Size(), index: 0);
            encoder.SetFragmentTexture(texture, index: 0);
            encoder.DrawPrimitives(MTLPrimitiveType.TriangleStrip, vertexStart: 0, vertexCount: 4, instanceCount: 1);
            encoder.PopDebugGroup();
            encoder.EndEncoding();
            commandBuffer.Commit();
        }

        [Export("drawInMTKView:")]
        public void Draw(MTKView view)
        {
            if (!SDPlugin.IsSDKReady)
            {
                return;
            }

            // Update sizes
            if (backgroundTextureSize.Width == 0 && backgroundTextureSize.Height == 0)
            {
                var size = MakeBackgroundTextureSize();
                backgroundTextureSize = size;
                UpdateMTKViewFrame();
            }

            // Draw meshes
            if (SDPlugin.ShowMesh)
            {
                MeshController.Update();
            }

            // Get pose and tracking quality
            var localOrientation = UIApplication.SharedApplication.StatusBarOrientation;
            float[] mPoseBuffer = new float[16];
            int trackingQuality = 0;

            unsafe
            {
                fixed (float* ptr = &mPoseBuffer[0])
                {
                    // R T
                    // 0 1
                    int bufferSize = 16;
                    trackingQuality = SDPlugin.SixDegreesSDK_GetPose(ptr, bufferSize);
                }
            }

            if (trackingQuality > 0)
            {
                // Update camera pose
                var row0 = new SCNVector4(mPoseBuffer[0], mPoseBuffer[1], mPoseBuffer[2], mPoseBuffer[3]);
                var row1 = new SCNVector4(mPoseBuffer[4], mPoseBuffer[5], mPoseBuffer[6], mPoseBuffer[7]);
                var row2 = new SCNVector4(mPoseBuffer[8], mPoseBuffer[9], mPoseBuffer[10], mPoseBuffer[11]);
                var row3 = new SCNVector4(mPoseBuffer[12], mPoseBuffer[13], mPoseBuffer[14], mPoseBuffer[15]);
                SCNMatrix4 poseMatrix = new SCNMatrix4(row0, row1, row2, row3);

                CameraNode.WorldTransform = poseMatrix;

                // Update camera projection
                var projectionTransform = MakeProjectionMatrix(localOrientation);
                CameraNode.Camera.ProjectionTransform = projectionTransform;
            }

            // Update vertex factory
            if (vertexFactory.IsComplete == false || orientation != localOrientation)
            {
                vertexFactory.Update(localOrientation, backgroundTextureSize);
            }
            orientation = localOrientation;

            // Draw background texture
            var texturePtr = SDPlugin.SixDegreesSDK_GetBackgroundTexture();

            if (texturePtr != IntPtr.Zero)
            {
                var obj = ObjCRuntime.Runtime.GetINativeObject<IMTLTexture>(texturePtr, false);
                Draw((IMTLTexture)obj);
            }

            if (commandQueue != null)
            {
                var commandBuffer = commandQueue.CommandBuffer();
                var currentDrawable = mtkView.CurrentDrawable;
                double CurrentTime = CAAnimation.CurrentMediaTime();
                var screenScale = UIScreen.MainScreen.Scale;
                var viewport = new CGRect(x: 0, y: 0,
                                      width: mtkView.Frame.Width * screenScale,
                                      height: mtkView.Frame.Height * screenScale);

                var renderPassDescriptor = MTLRenderPassDescriptor.CreateRenderPassDescriptor();
                renderPassDescriptor.ColorAttachments[0].Texture = currentDrawable.Texture;
                renderPassDescriptor.ColorAttachments[0].LoadAction = MTLLoadAction.Load;
                renderPassDescriptor.ColorAttachments[0].StoreAction = MTLStoreAction.Store;

                renderer.Render(CurrentTime,
                                viewport,
                                commandBuffer,
                                renderPassDescriptor);

                commandBuffer.PresentDrawable(currentDrawable);
                commandBuffer.Commit();
            }
        }
    }
}