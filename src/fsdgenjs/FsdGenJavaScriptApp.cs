using System.Collections.Generic;
using ArgsReading;
using Facility.Console;
using Facility.JavaScript;
using Facility.Definition.CodeGen;

namespace fsdgenjs
{
	public sealed class FsdGenJavaScriptApp : CodeGeneratorApp
	{
		public static int Main(string[] args)
		{
			return new FsdGenJavaScriptApp().Run(args);
		}

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
		};

		protected override CodeGenerator CreateGenerator(ArgsReader args)
		{
			return new JavaScriptGenerator
			{
				ModuleName = args.ReadOption("module"),
				TypeScript = args.ReadFlag("typescript"),
			};
		}

		protected override bool SupportsSingleOutput => true;
	}
}
