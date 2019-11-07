using Foundation;
using ARKit;

namespace SixDegrees.iOS
{
    [BaseType(typeof(NSObject))]
    [Protocol]
    interface SDK
    {
        [Static]
        [Export("SixDegreesSDK_InitializeWithConfig:")]
        bool InitializeWithConfig(ARWorldTrackingConfiguration configuration);
    }
}