var target = Argument("target", "Lib:Test");
var configuration = Argument("configuration", "Release");


// DEFINITIONS
// ////////////////////////////////////////////////////////////////////

var sourceDir = Directory("./sources");

var conveniencePackageProject = sourceDir + File("Capsule/Capsule.csproj");
var testProject = sourceDir + File("Capsule.Test/Capsule.Test.csproj");
var coreLibProject = sourceDir + File("Capsule.Core/Capsule.Core.csproj");
var extensionsLibProject = sourceDir + File("Capsule.Extensions.DependencyInjection/Capsule.Extensions.DependencyInjection.csproj");
var testSupportProject = sourceDir + File("Capsule.Testing/Capsule.Testing.csproj");
var generatorProject = sourceDir + File("Capsule.Generator/Capsule.Generator.csproj");

var releaseDir = Directory("./release/lib");


// TASKS
//////////////////////////////////////////////////////////////////////

Task("Lib:Test")
    .Does(() =>
{
    DotNetTest(testProject, new DotNetTestSettings {
        Configuration = configuration,
        Filter = "FullyQualifiedName~AutomatedTests",
        Loggers = new [] { "junit;LogFilePath=test-results.xml;MethodFormat=Class;FailureBodyFormat=Verbose" }
    });
});

Task("Lib:Build")
    .IsDependentOn("Lib:Test")
    .Does(() =>
{
    var version = Argument<string>("lib-version");

    CleanDirectory(releaseDir);

    var libSettings = new DotNetPackSettings {
        Configuration = configuration,
        OutputDirectory = releaseDir,
        IncludeSource = true,
        IncludeSymbols = true,
        SymbolPackageFormat = "snupkg",
        MSBuildSettings = new DotNetMSBuildSettings().SetVersion(version) };

    DotNetPack(conveniencePackageProject, libSettings);
    DotNetPack(coreLibProject, libSettings);
    DotNetPack(extensionsLibProject, libSettings);
    DotNetPack(testSupportProject, libSettings);
        
    DotNetPack(generatorProject, new DotNetPackSettings {
        Configuration = configuration,
        OutputDirectory = releaseDir,
        IncludeSource = false,
        IncludeSymbols = false,
        SymbolPackageFormat = null,
        MSBuildSettings = new DotNetMSBuildSettings().SetVersion(version) });
});


// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
