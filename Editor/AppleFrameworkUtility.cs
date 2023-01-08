#if UNITY_EDITOR_OSX
using System.IO;
using UnityEditor;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace Apple.Core
{
    public static class AppleFrameworkUtility
    {
        /// <summary>
        /// Wrapper around pbxProject.AddFrameworkToProject. This helper
        /// automatically finds the correct targetGuid based on settings.
        /// </summary>
        /// <param name="framework"></param>
        /// <param name="weak"></param>
        /// <param name="buildTarget"></param>
        /// <param name="pbxProject"></param>
        public static void AddFrameworkToProject(string framework, bool weak, BuildTarget buildTarget, PBXProject pbxProject)
        {

            if (buildTarget == BuildTarget.StandaloneOSX
                && !AppleBuild.IsXcodeGeneratedMac())
                return;

            var projectTargetName = buildTarget == BuildTarget.StandaloneOSX ? Application.productName : "Unity-iPhone";
            var targetGuid = buildTarget == BuildTarget.StandaloneOSX ? pbxProject.TargetGuidByName(projectTargetName) : pbxProject.GetUnityMainTargetGuid();

            pbxProject.AddFrameworkToProject(targetGuid, framework, weak);
        }

        /// <summary>
        /// Wrapper around PbxProjectExtensions.AddFileToEmbeddedFrameworks. This
        /// helper automatically finds the correct paths and targetGuids.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="buildTarget"></param>
        /// <param name="pathToBuiltProject"></param>
        /// <param name="pbxProject"></param>
        public static void CopyAndEmbed(string source, BuildTarget buildTarget, string pathToBuiltProject, PBXProject pbxProject)
        {
            var frameworkName = Path.GetFileName(source);
            var parentDirectory = string.Empty;
            var relativeTargetCopyName = string.Empty;

            switch(buildTarget)
            {
                case BuildTarget.iOS:
                case BuildTarget.tvOS:
                    parentDirectory = pathToBuiltProject;
                    relativeTargetCopyName = "Frameworks";
                    break;
                case BuildTarget.StandaloneOSX:
                    if (AppleBuild.IsXcodeGeneratedMac())
                    {
                        parentDirectory = Path.GetDirectoryName(pathToBuiltProject);
                        relativeTargetCopyName = $"{Application.productName}/Frameworks";
                    }
                    else
                    {
                        parentDirectory = pathToBuiltProject;
                        relativeTargetCopyName = "Contents/PlugIns";
                    }
                    break;
            }

            // Copy the actual framework over, delete existing & meta files...
            var copyBinaryPath = $"{parentDirectory}/{relativeTargetCopyName}/{Path.GetFileName(source)}";
            Debug.Log($"CopyAndEmbed putting source file {source} at {copyBinaryPath}");
            Copy(source, copyBinaryPath);

            if (pbxProject != null)
            {
                // Add as embedded...
                var projectTargetName = buildTarget == BuildTarget.StandaloneOSX ? Application.productName : "Unity-iPhone";
                var targetGuid = buildTarget == BuildTarget.StandaloneOSX ? pbxProject.TargetGuidByName(projectTargetName) : pbxProject.GetUnityMainTargetGuid();
                var fileGuid = pbxProject.AddFile(Path.GetFullPath(copyBinaryPath), $"Frameworks/{frameworkName}", PBXSourceTree.Sdk);
                Debug.Log($"CopyAndEmbed embedding {frameworkName} into target {projectTargetName}");
                PBXProjectExtensions.AddFileToEmbedFrameworks(pbxProject, targetGuid, fileGuid);
            }
            else
            {
                Debug.Log($"CopyAndEmbed no pbxproject file. Not embedding {frameworkName}");
            }
        }

        /// <summary>
        /// Copies the path from source to desitnation and removes any .meta files from the destination
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void Copy(string source, string destination)
        {
            Debug.Log($"AppleFrameworkUtility: Copying {source} as {destination}...");

            // Clean up any existing unity plugins or old from previous build...
            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }

            // Copy raw from source...
            FileUtil.CopyFileOrDirectory(source, destination);


            // Recursively cleanup meta files...
            RecursiveCleanupMetaFiles(new DirectoryInfo(destination));
        }

        /// <summary>
        /// Private recursive method to remove .meta files from a directory and all of it's sub directories.
        /// </summary>
        private static void RecursiveCleanupMetaFiles(DirectoryInfo directory)
        {
            var directories = directory.GetDirectories();
            var files = directory.GetFiles();

            foreach (var file in files)
            {
                // File is a Unity meta file, clean it up...
                if (file.Extension == ".meta")
                {
                    Debug.Log($"AppleFrameworkUtility: Cleaning up meta file ({file.FullName})");
                    FileUtil.DeleteFileOrDirectory(file.FullName);
                }
            }

            // Recurse...
            foreach (var subdirectory in directories)
            {
                RecursiveCleanupMetaFiles(subdirectory);
            }
        }
    }
}
#endif