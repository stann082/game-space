using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;

// ReSharper disable UnusedMember.Local
// ReSharper disable AllUnderscoreLocalParameterName
class BuildProject : NukeBuild
{

    public static int Main() => Execute<BuildProject>(x => x.Build);

    #region Parameters

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = Configuration.Debug;

    [Parameter("Verbosity level of the build output - Default is 'Minimal'")]
    readonly DotNetVerbosity DotNetVerbosityLevel = DotNetVerbosity.minimal;

    [Parameter("Paths to the test project(s) to run. You can provide multiple paths as separate arguments.")]
    readonly AbsolutePath[] TestProjectPaths;

    #endregion

    #region Targets

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetClean(s => s
                .SetConfiguration(Configuration)
                .SetVerbosity(DotNetVerbosityLevel)
                .SetProject(RootDirectory / "GameSpace.sln"));
        });

    Target Build => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetConfiguration(Configuration)
                .SetProjectFile(RootDirectory / "GameSpace.sln")
                .SetVerbosity(DotNetVerbosityLevel)
                .EnableNoRestore());
        });

    Target DeepClean => _ => _
        .Executes(() =>
        {
            var buildDirectory = RootDirectory / "build";
            Log.Information("Cleaning directory {BuildDirectory}", buildDirectory);
            buildDirectory.CreateOrCleanDirectory();

            var publishDirectory = RootDirectory / "pub";
            Log.Information("Cleaning directory {PublishDirectory}", publishDirectory);
            publishDirectory.CreateOrCleanDirectory();
        });

    Target Deploy => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var targetDirectory = Path.Combine(appDataPath, "utils");
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            var targetFile = Path.Combine(targetDirectory, "gamespace.exe");
            if (File.Exists(targetFile))
            {
                File.Delete(targetFile);
            }

            var sourceFile = RootDirectory / "pub" / "gamespace.exe";
            File.Copy(sourceFile, targetFile);
            Log.Information("Deployed gamespace.exe to {TargetDirectory}", targetDirectory);
        });

    Target Publish => _ => _
        .DependsOn(Build)
        .Executes(() =>
        {
            DotNetTasks.DotNetPublish(s => s
                .SetProject(RootDirectory / "GameSpace.sln")
                .SetConfiguration(Configuration)
                .SetVerbosity(DotNetVerbosityLevel)
                .SetOutput(RootDirectory / "pub"));
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetVerbosity(DotNetVerbosityLevel)
                .SetProjectFile(RootDirectory / "GameSpace.sln"));
        });

    Target Test => _ => _
        .DependsOn(Build)
        .Executes(() =>
        {
            foreach (var project in GetTestProjects())
            {
                DotNetTasks.DotNetTest(s => s
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration)
                    .SetVerbosity(DotNetVerbosityLevel)
                    .EnableNoBuild());
            }
        });

    Target TestAsync => _ => _
        .DependsOn(Build)
        .Executes(() =>
        {
            GetTestProjects().AsParallel().ForAll(project =>
            {
                DotNetTasks.DotNetTest(s => s
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration)
                    .SetVerbosity(DotNetVerbosityLevel)
                    .EnableNoBuild());
            });
        });

    #endregion

    #region Helper Methods

    AbsolutePath[] GetTestProjects() =>
        (TestProjectPaths ?? []).Length > 0
            ? TestProjectPaths
            :
            [
                RootDirectory / "TestProject1" / "TestProject1.csproj",
                RootDirectory / "TestProject2" / "TestProject2.csproj",
                RootDirectory / "TestProject3" / "TestProject3.csproj"
            ];

    #endregion

}
