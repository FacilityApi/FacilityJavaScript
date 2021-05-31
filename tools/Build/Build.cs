using System;
using System.IO;
using System.Linq;
using Faithlife.Build;
using static Faithlife.Build.AppRunner;
using static Faithlife.Build.BuildUtility;
using static Faithlife.Build.DotNetRunner;

return BuildRunner.Execute(args, build =>
{
	var codegen = "fsdgenjs";

	var gitLogin = new GitLoginInfo("FacilityApiBot", Environment.GetEnvironmentVariable("BUILD_BOT_PASSWORD") ?? "");

	var dotNetBuildSettings = new DotNetBuildSettings
	{
		NuGetApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY"),
		DocsSettings = new DotNetDocsSettings
		{
			GitLogin = gitLogin,
			GitAuthor = new GitAuthorInfo("FacilityApiBot", "facilityapi@gmail.com"),
			SourceCodeUrl = "https://github.com/FacilityApi/FacilityJavaScript/tree/master/src",
			ProjectHasDocs = name => !name.StartsWith("fsdgen", StringComparison.Ordinal),
		},
		PackageSettings = new DotNetPackageSettings
		{
			GitLogin = gitLogin,
			PushTagOnPublish = x => $"nuget.{x.Version}",
		},
	};

	build.AddDotNetTargets(dotNetBuildSettings);

	build.Target("codegen")
		.DependsOn("build")
		.Describe("Generates code from the FSD")
		.Does(() => CodeGen(verify: false));

	build.Target("verify-codegen")
		.DependsOn("build")
		.Describe("Ensures the generated code is up-to-date")
		.Does(() => CodeGen(verify: true));

	build.Target("test")
		.DependsOn("verify-codegen");

	void CodeGen(bool verify)
	{
		var configuration = dotNetBuildSettings.GetConfiguration();
		var toolPath = FindFiles($"src/{codegen}/bin/{configuration}/net5.0/{codegen}.dll").FirstOrDefault() ?? throw new BuildException($"Missing {codegen}.dll.");

		var verifyOption = verify ? "--verify" : null;

		RunDotNet(toolPath, "example/ExampleApi.fsd", "example/js/", "--indent", "2", "--express", "--disable-eslint", "--newline", "lf", verifyOption);
		RunDotNet(toolPath, "example/ExampleApi.fsd", "example/ts/src/", "--typescript", "--express", "--disable-eslint", "--newline", "lf", verifyOption);
	}

	build.Target("build-npm")
		.Describe("Builds the npm package.")
		.Does(() =>
		{
			RunNpmFrom("./ts", "install");
			RunNpmFrom("./ts", "run", "build");
		});

	build.Target("test-npm")
		.DependsOn("build-npm")
		.Describe("Tests the npm package.")
		.Does(() =>
		{
			RunNpmFrom("./ts", "run", "test");

			RunNpmFrom("./example/js", "install");
			RunNpmFrom("./example/js", "run", "test");

			RunNpmFrom("./example/ts", "install");
			RunNpmFrom("./example/ts", "run", "test");
		});

	build.Target("publish-npm")
		.DependsOn("test-npm")
		.Describe("Publishes the npm package.")
		.Does(() =>
		{
			var token = Environment.GetEnvironmentVariable("NPM_ACCESS_TOKEN");
			if (token is null)
				throw new BuildException("Missing NPM_ACCESS_TOKEN.");
			File.WriteAllText("./ts/.npmrc", $"//registry.npmjs.org/:_authToken={token}");

			RunNpmFrom("./ts", "publish");
		});

	void RunNpmFrom(string directory, params string[] args) =>
		RunApp("npm",
			new AppRunnerSettings
			{
				Arguments = args,
				WorkingDirectory = directory,
				UseCmdOnWindows = true,
				IsExitCodeSuccess = x => args[0] == "publish" ? x is 0 or 1 : x is 0,
			});
});
