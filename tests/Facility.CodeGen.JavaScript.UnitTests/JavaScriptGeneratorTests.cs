using System.IO;
using System.Linq;
using System.Reflection;
using Facility.Definition;
using Facility.Definition.Fsd;
using NUnit.Framework;

namespace Facility.CodeGen.JavaScript.UnitTests
{
	public sealed class JavaScriptGeneratorTests
	{
		[TestCase(false)]
		[TestCase(true)]
		public void GenerateExampleApiSuccess(bool typeScript)
		{
			ServiceInfo service;
			const string fileName = "Facility.CodeGen.JavaScript.UnitTests.ExampleApi.fsd";
			var parser = new FsdParser();
			var stream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream(fileName);
			Assert.IsNotNull(stream);
			using (var reader = new StreamReader(stream))
				service = parser.ParseDefinition(new ServiceDefinitionText(Path.GetFileName(fileName), reader.ReadToEnd()));

			var generator = new JavaScriptGenerator
			{
				GeneratorName = "JavaScriptGeneratorTests",
				TypeScript = typeScript,
			};
			var result = generator.GenerateOutput(service);
			Assert.IsNotNull(result);
		}

		[Test]
		public void GenerateExampleApiTypeScript_IncludesEnums()
		{
			ServiceInfo service;
			const string fileName = "Facility.CodeGen.JavaScript.UnitTests.ExampleApi.fsd";
			var parser = new FsdParser();
			var stream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream(fileName);
			Assert.IsNotNull(stream);
			using (var reader = new StreamReader(stream))
				service = parser.ParseDefinition(new ServiceDefinitionText(Path.GetFileName(fileName), reader.ReadToEnd()));

			var generator = new JavaScriptGenerator
			{
				GeneratorName = "JavaScriptGeneratorTests",
				TypeScript = true,
				NewLine = "\n"
			};
			var result = generator.GenerateOutput(service);
			Assert.IsNotNull(result);

			var typesFile = result.Files.Single(f => f.Name == "exampleApiTypes.ts");
			const string expectedEnums = @"
/** Identifies a widget field. */
export enum WidgetField {
	/** The 'id' field. */
	id = 'id',

	/** The 'name' field. */
	name = 'name',

	/**
	 * The 'weight' field.
	 * @deprecated
	 */
	weight = 'weight',
}

/**
 * An obsolete enum.
 * @deprecated
 */
export enum ObsoleteEnum {
	unused = 'unused',
}";
			Assert.That(typesFile.Text, Contains.Substring(expectedEnums));

			const string expectedEnumUsage = @"widgetField?: WidgetField;";

			Assert.That(typesFile.Text, Contains.Substring(expectedEnumUsage));
		}
	}
}
