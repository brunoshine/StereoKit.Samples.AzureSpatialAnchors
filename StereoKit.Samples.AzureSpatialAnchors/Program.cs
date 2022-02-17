using StereoKit;
using System;

namespace StereoKit.Samples.AzureSpatialAnchors
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize StereoKit
            SKSettings settings = new SKSettings
            {
                appName = "StereoKit.Samples.AzureSpatialAnchors",
                assetsFolder = "Assets",

                /* Mixed Reality preferences */
                blendPreference = DisplayBlend.AnyTransparent,
                displayPreference = DisplayMode.MixedReality,

                /* What Log details we want to get on the Output Window */
                logFilter = LogLevel.Diagnostic
            };

            // First things first...
            if (!SK.Initialize(settings))
                Environment.Exit(1);

            // Just display some introduction to this sample
            SK.AddStepper<IntroStepper>();

            ASADemoScene scene = new ASADemoScene();
            scene.Initialize();

            // Core application loop
            SK.Run(() => {
                scene.Update();
            }, () => scene.Shutdown());

            SK.Shutdown();
        }
    }
}
