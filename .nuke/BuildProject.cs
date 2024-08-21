using System;
using System.IO;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;

class BuildProject : NukeBuild
{

    public static int Main() => Execute<BuildProject>(x => x.Build);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    Target Clean => c => c
        .Before(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetClean(s => s
                .SetProject(RootDirectory / "GameSpace.sln")
                .SetVerbosity(DotNetVerbosity.quiet));
            
            var buildDirectory = RootDirectory / "build";
            Log.Information("Cleaning directory {BuildDirectory}", buildDirectory);
            buildDirectory.CreateOrCleanDirectory();
            
            var publishDirectory = RootDirectory / "pub";
            Log.Information("Cleaning directory {PublishDirectory}", publishDirectory);
            publishDirectory.CreateOrCleanDirectory();
        });

    Target Restore => r => r
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(RootDirectory / "GameSpace.sln")
                .SetVerbosity(DotNetVerbosity.quiet));
        });

    Target Build => b => b
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "GameSpace.sln")
                .SetConfiguration(Configuration)
                .SetVerbosity(DotNetVerbosity.quiet)
                .EnableNoRestore());
        });

    Target Publish => p => p
        .DependsOn(Build)
        .Executes(() =>
        {
            DotNetTasks.DotNetPublish(s => s
                .SetProject(RootDirectory / "GameSpace.sln")
                .SetConfiguration(Configuration.Release)
                .SetOutput(RootDirectory / "pub"));
        });
    
    Target Deploy => d => d
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

}
