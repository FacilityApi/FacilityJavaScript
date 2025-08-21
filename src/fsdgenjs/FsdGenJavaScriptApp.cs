using ArgsReading;
using Facility.CodeGen.Console;
using Facility.CodeGen.JavaScript;
using Facility.Definition.CodeGen;
using Facility.Definition.Fsd;

namespace fsdgenjs
{
	public sealed class FsdGenJavaScriptApp : CodeGeneratorApp
	{
		public static int Main(string[] args) => new FsdGenJavaScriptApp().Run(args);

		protected override IReadOnlyList<string> Description =>
		[
			"Generates a JavaScript client for a Facility Service Definition.",
		];

		protected override IReadOnlyList<string> ExtraUsage =>
		[
			"   --module <name>",
			"      The module name used by the generated JavaScript.",
			"   --typescript",
			"      Generates TypeScript.",
			"   --express",
			"      Generates Express service.",
			"   --fastify",
			"      Generates a Fastify plugin. When specified, only the server plugin is generated, not the client. EXPERIMENTAL: This option is subject to change/removal without a major version bump.",
			"   --disable-eslint",
			"      Disables ESLint via code comment.",
			"   --no-http",
			"      Omits generated HTTP client code.",
			"   --file-name-suffix",
			"      Suffix to append to generated file names before the file extension.",
		];

		protected override ServiceParser CreateParser() => new FsdParser(new FsdParserSettings { SupportsEvents = true });

		protected override CodeGenerator CreateGenerator() => new JavaScriptGenerator();

		protected override FileGeneratorSettings CreateSettings(ArgsReader args) =>
			new JavaScriptGeneratorSettings
			{
				ModuleName = args.ReadOption("module"),
				TypeScript = args.ReadFlag("typescript"),
				NoHttp = args.ReadFlag("no-http"),
				Express = args.ReadFlag("express"),
				Fastify = args.ReadFlag("fastify"),
				DisableESLint = args.ReadFlag("disable-eslint"),
				FileNameSuffix = args.ReadOption("file-name-suffix"),
			};
	}
}
