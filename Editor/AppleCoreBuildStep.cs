using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR_OSX
using UnityEditor.iOS.Xcode;
#endif

namespace Apple.Core
{
    public class AppleCoreBuildStep : AppleBuildStep
    {
        public override string DisplayName => "AppleCore";
        public override string DisplayIcon => null;

        const string _iosFrameworkGuid = "ad7a5c94b284a43cbbbb1760b765c6fb";
        const string _macOSFrameworkGuid = "4c5c3de9930a9455d91b231827b0566b";
        const string _tvOSFrameworkGuid = "585c4f355b0ac4ae1ac8506b9f6065f3";

#if UNITY_EDITOR_OSX
        public override void OnProcessFrameworks(AppleBuildProfile appleBuildProfile, BuildTarget buildTarget, string pathToBuiltTarget, PBXProject pBXProject)
        {
            var frameworkGuid = string.Empty;

            switch(buildTarget)
            {
                case BuildTarget.iOS:
                    frameworkGuid = _iosFrameworkGuid;
                    break;
                case BuildTarget.StandaloneOSX:
                    frameworkGuid = _macOSFrameworkGuid;
                    break;
                case BuildTarget.tvOS:
                    frameworkGuid = _tvOSFrameworkGuid;
                    break;
            }

            // Prepare paths...
            var localBinaryPath = AssetDatabase.GUIDToAssetPath(frameworkGuid);

            // Delete and copy...
            AppleFrameworkUtility.CopyAndEmbed(localBinaryPath, buildTarget, pathToBuiltTarget, pBXProject);
        }
#endif
    }
}
