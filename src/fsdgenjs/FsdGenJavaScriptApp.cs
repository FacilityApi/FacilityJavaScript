using System.Collections.Generic;
using ArgsReading;
using Facility.CodeGen.Console;
using Facility.CodeGen.JavaScript;
using Facility.Definition.CodeGen;

namespace fsdgenjs
{
	public sealed class FsdGenJavaScriptApp : CodeGeneratorApp
	{
		public static int Main(string[] args) => new FsdGenJavaScriptApp().Run(args);

		protected override IReadOnlyList<string> Description => new[]
		{
			"Generates a JavaScript client for a Facility Service Definition.",
		};

		protected override IReadOnlyList<string> ExtraUsage => new[]
		{
			"   --module <name>",
			"      The module name used by the generated JavaScript.",
			"   --typescript",
			"      Generates TypeScript.",
			"   --express",
			"      Generates Express service.",
			"   --disable-eslint",
			"      Disables ESLint via code comment.",
		};

		protected override CodeGenerator CreateGenerator() => new JavaScriptGenerator();

		protected override FileGeneratorSettings CreateSettings(ArgsReader args) =>
			new JavaScriptGeneratorSettings
			{
				ModuleName = args.ReadOption("module"),
				TypeScript = args.ReadFlag("typescript"),
				Express = args.ReadFlag("express"),
				DisableESLint = args.ReadFlag("disable-eslint"),
			};
	}
}
