var target = Argument("target", "Lib:Test");
var configuration = Argument("configuration", "Release");


// DEFINITIONS
// ////////////////////////////////////////////////////////////////////

var sourceDir = Directory("./sources");

var testProject = sourceDir + File("Capsule.Test/Capsule.Test.csproj");
var libProject = sourceDir + File("Capsule/Capsule.csproj");
var generatorProject = sourceDir + File("CapsuleGen/CapsuleGen.csproj");

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
    .Does(() =>
{
    var version = Argument<string>("lib-version");

    CleanDirectory(releaseDir);

    var packSettings = new DotNetPackSettings {
        Configuration = configuration,
        OutputDirectory = releaseDir,
        IncludeSource = true,
        IncludeSymbols = true,
        SymbolPackageFormat = "snupkg",
        MSBuildSettings = new DotNetMSBuildSettings().SetVersion(version) };

    DotNetPack(libProject, packSettings);
    DotNetPack(generatorProject, packSettings);
});


// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
