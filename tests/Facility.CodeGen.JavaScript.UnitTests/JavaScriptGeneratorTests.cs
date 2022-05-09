using System.IO;
using System.Linq;
using System.Reflection;
using Facility.Definition;
using Facility.Definition.CodeGen;
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
			var stream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream(fileName)!;
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
		public void GenerateExampleApiTypeScript_DoesntExpectResponseBodyForBooleanFields()
		{
			var result = GenExampleApi();
			var codeFile = result.Files.Single(f => f.Name == "exampleApi.ts");

			const string expectedCheckWithoutBody = @"
				else if (status === 304) {
					value = { notModified: true };
				}";

			const string expectedCheckWithBody = @"
				else if (status === 202 && result.json) {
					value = { job: result.json } as IEditWidgetResponse;
				}";

			Assert.That(codeFile.Text, Contains.Substring(expectedCheckWithoutBody));
			Assert.That(codeFile.Text, Contains.Substring(expectedCheckWithBody));
		}

		[Test]
		public void GenerateExampleApiTypeScript_DoesntExpectResponseBodyForNoContentResponses()
		{
			var result = GenExampleApi();
			var codeFile = result.Files.Single(f => f.Name == "exampleApi.ts");

			const string unexpectedNoContentCheck = @"if (status === 204 && result.json) {";
			const string expectedNoContentCheck = @"if (status === 204) {";

			Assert.That(codeFile.Text, Does.Not.Contains(unexpectedNoContentCheck));
			Assert.That(codeFile.Text, Contains.Substring(expectedNoContentCheck));
		}

		[Test]
		public void GenerateExampleApiTypeScript_IncludesEnums()
		{
			var result = GenExampleApi();

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

		private CodeGenOutput GenExampleApi()
		{
			ServiceInfo service;
			const string fileName = "Facility.CodeGen.JavaScript.UnitTests.ExampleApi.fsd";
			var parser = new FsdParser();
			var stream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream(fileName)!;
			Assert.IsNotNull(stream);
			using (var reader = new StreamReader(stream))
				service = parser.ParseDefinition(new ServiceDefinitionText(Path.GetFileName(fileName), reader.ReadToEnd()));

			var generator = new JavaScriptGenerator
			{
				GeneratorName = "JavaScriptGeneratorTests",
				TypeScript = true,
				NewLine = "\n",
			};
			return generator.GenerateOutput(service);
		}
	}
}
