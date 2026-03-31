using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class iOSPostProcessBuild
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string buildPath)
    {
        if (target != BuildTarget.iOS) return;

        string pbxprojPath = PBXProject.GetPBXProjectPath(buildPath);
        PBXProject project = new PBXProject();
        project.ReadFromString(File.ReadAllText(pbxprojPath));

        string appTarget = project.GetUnityMainTargetGuid();
        string frameworkTarget = project.GetUnityFrameworkTargetGuid();

        string[] compatLibs = new string[]
        {
            "$(DEVELOPER_DIR)/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift/$(PLATFORM_NAME)/libswiftCompatibility51.a",
            "$(DEVELOPER_DIR)/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift/$(PLATFORM_NAME)/libswiftCompatibility56.a",
            "$(DEVELOPER_DIR)/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift/$(PLATFORM_NAME)/libswiftCompatibilityConcurrency.a",
        };

        foreach (string guid in new[] { appTarget, frameworkTarget })
        {
            project.AddBuildProperty(guid, "OTHER_LDFLAGS", "-L/usr/lib/swift");
            project.AddBuildProperty(guid, "OTHER_LDFLAGS", "-Xlinker");
            project.AddBuildProperty(guid, "OTHER_LDFLAGS", "-rpath");
            project.AddBuildProperty(guid, "OTHER_LDFLAGS", "-Xlinker");
            project.AddBuildProperty(guid, "OTHER_LDFLAGS", "/usr/lib/swift");

            foreach (string lib in compatLibs)
            {
                project.AddBuildProperty(guid, "OTHER_LDFLAGS", "-Xlinker");
                project.AddBuildProperty(guid, "OTHER_LDFLAGS", "-force_load");
                project.AddBuildProperty(guid, "OTHER_LDFLAGS", "-Xlinker");
                project.AddBuildProperty(guid, "OTHER_LDFLAGS", lib);
            }

            project.SetBuildProperty(guid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            project.SetBuildProperty(guid, "SWIFT_VERSION", "5.0");
        }

        File.WriteAllText(pbxprojPath, project.WriteToString());
    }
}
