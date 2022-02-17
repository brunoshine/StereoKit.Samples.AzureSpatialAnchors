using StereoKit.Framework;

namespace StereoKit.Samples.AzureSpatialAnchors
{
    class IntroStepper : IStepper
    {
        Matrix descPose = Matrix.TR(-0.5f, 0, -0.5f, Quat.LookDir(1, 0, 1));
        string description = "StereoKit can use Azure Spatial Anchors to place persisting objects in the real world and share these persistance between sessions and other devices.";
        Matrix titlePose = Matrix.TRS(V.XYZ(-0.5f, 0.05f, -0.5f), Quat.LookDir(1, 0, 1), 2);
        string title = "Azure Spatial Anchors";


        public bool Enabled => true;

        public bool Initialize()
        {
            return true;
        }

        public void Step()
        {
            Text.Add(title, titlePose);
            Text.Add(description, descPose, V.XY(0.5f, 0), TextFit.Wrap, TextAlign.TopCenter, TextAlign.TopLeft);
        }

        public void Shutdown()
        {
        }
    }
}
