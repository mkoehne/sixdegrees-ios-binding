// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace Sample.iOS
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton loadButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton meshButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton saveButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        Sample.iOS.SixDegreesMetalView sixDegreesView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel statusLabel { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (loadButton != null) {
                loadButton.Dispose ();
                loadButton = null;
            }

            if (meshButton != null) {
                meshButton.Dispose ();
                meshButton = null;
            }

            if (saveButton != null) {
                saveButton.Dispose ();
                saveButton = null;
            }

            if (sixDegreesView != null) {
                sixDegreesView.Dispose ();
                sixDegreesView = null;
            }

            if (statusLabel != null) {
                statusLabel.Dispose ();
                statusLabel = null;
            }
        }
    }
}