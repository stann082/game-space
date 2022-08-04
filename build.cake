var target = Argument("target", "deploy");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("clean")
    .Does(() =>
{
    CleanDirectory("./build/");
    CleanDirectory("./pub/");
});

Task("build")
    .Does(() =>
{
    CleanDirectory("./build/");
    DotNetBuild("./GameSpace.sln", new DotNetBuildSettings
    {
        Configuration = configuration,
    });
});

Task("publish")
    .Does(() =>
{
    CleanDirectory("./pub");
    DotNetPublish("./GameSpace.sln", new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = "./pub/"
    });
});

Task("deploy")
    .IsDependentOn("publish")
    .Does(() =>
{
    CleanDirectory("./pub");
    DotNetPublish("./GameSpace.sln", new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = "./pub/"
    });
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);