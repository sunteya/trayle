#addin nuget:?package=Cake.VersionReader

var target = Argument<string>("target", "Default");

var name = "trayle";
var buildType = Argument<string>("buildType", "Release");

var sln = string.Format("./{0}.sln", name);

Task("NuGetRestore").Does(() =>
{
  NuGetRestore(sln);
});

Task("Build").IsDependentOn("NuGetRestore").Does(() =>
{
  MSBuild(sln, new MSBuildSettings{
    Verbosity = Verbosity.Minimal,
    Configuration = buildType,
  });
});

Task("Package").IsDependentOn("Build").Does(() =>
{
  var build = "./build";
  CleanDirectory(build);

  var bin = string.Format("./bin/{0}/{1}.exe", buildType, name);
  CopyFileToDirectory(bin, build);
  var sample = string.Format("./{0}.yml.sample", name);
  CopyFileToDirectory(sample, build);

	var version = GetVersionNumber(bin);
  CreateDirectory("./dest");
  var package = string.Format("./dest/{0}-{1}.zip", name, version);
  if (FileExists(package))
  {
    DeleteFile(package);
  }
  Zip(build, package, build + "/*");
});

RunTarget(target);
