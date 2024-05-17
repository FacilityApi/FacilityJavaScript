using System.Reflection;
using Facility.Definition;
using Facility.Definition.CodeGen;
using Facility.Definition.Fsd;
using FluentAssertions;
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
			Assert.That(stream, Is.Not.Null);
			using (var reader = new StreamReader(stream))
				service = parser.ParseDefinition(new ServiceDefinitionText(Path.GetFileName(fileName), reader.ReadToEnd()));

			var generator = new JavaScriptGenerator
			{
				GeneratorName = "JavaScriptGeneratorTests",
				TypeScript = typeScript,
			};
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void GenerateExampleApiTypeScript_IncludesEnums()
		{
			ServiceInfo service;
			const string fileName = "Facility.CodeGen.JavaScript.UnitTests.ExampleApi.fsd";
			var parser = new FsdParser();
			var stream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream(fileName)!;
			Assert.That(stream, Is.Not.Null);
			using (var reader = new StreamReader(stream))
				service = parser.ParseDefinition(new ServiceDefinitionText(Path.GetFileName(fileName), reader.ReadToEnd()));

			var generator = new JavaScriptGenerator
			{
				GeneratorName = "JavaScriptGeneratorTests",
				TypeScript = true,
				NewLine = "\n",
			};
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "exampleApiTypes.ts");
			const string expectedEnums = """
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
				}
				""";
			Assert.That(typesFile.Text, Contains.Substring(expectedEnums));

			const string expectedEnumUsage = "widgetField?: WidgetField;";

			Assert.That(typesFile.Text, Contains.Substring(expectedEnumUsage));
		}

		[Test]
		public void GenerateExampleApiTypeScript_DoesntRequireJsonWhenNoResponseBodyExpected()
		{
			ServiceInfo service;
			const string fileName = "Facility.CodeGen.JavaScript.UnitTests.ExampleApi.fsd";
			var parser = new FsdParser();
			var stream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream(fileName)!;
			Assert.That(stream, Is.Not.Null);
			using (var reader = new StreamReader(stream))
				service = parser.ParseDefinition(new ServiceDefinitionText(Path.GetFileName(fileName), reader.ReadToEnd()));

			var generator = new JavaScriptGenerator
			{
				GeneratorName = "JavaScriptGeneratorTests",
				TypeScript = true,
				NewLine = "\n",
			};
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var apiFile = result.Files.Single(f => f.Name == "exampleApi.ts");

			// `deleteWidget` does not expect response body
			const string expectedDeleteWidgetLines = """
								let value: IDeleteWidgetResponse | null = null;
								if (status === 204) {
									value = {};
								}
				""";
			Assert.That(apiFile.Text, Contains.Substring(expectedDeleteWidgetLines));

			// `createWidget` does expect response body
			const string expectedCreateWidgetLines = """
								let value: ICreateWidgetResponse | null = null;
								if (status === 201) {
									if (result.json) {
										value = { widget: result.json } as ICreateWidgetResponse;
									}
								}
				""";
			Assert.That(apiFile.Text, Contains.Substring(expectedCreateWidgetLines));
		}

		[Test]
		public void GenerateExampleApiTypeScript_ExternDataWithNameAndModuel()
		{
			const string definition = "[csharp] service TestApi { [js(name: \"SomeExternalDto\", module: \"extern-dto-module\")] extern data Thing; data Test { thing: Thing; } }";
			var parser = new FsdParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "testApiTypes.ts");
			Assert.That(typesFile.Text, Does.Contain("import { SomeExternalDto as IThing } from 'extern-dto-module';"));
			Assert.That(typesFile.Text, Does.Contain("thing?: IThing;"));
		}

		[Test]
		public void GenerateExampleApiTypeScript_ExternDataWithoutJsAttribute()
		{
			ThrowsServiceDefinitionException(
				"[csharp] service TestApi { extern data MissingAttribute; }",
				"TestApi.fsd(1,35): Missing required attribute 'js'.");
		}

		[Test]
		public void GenerateExampleApiTypeScript_ExternDataWithoutModule()
		{
			ThrowsServiceDefinitionException(
				"[csharp] service TestApi { [js(name: \"SomeData\")] extern data MissingModule; }",
				"TestApi.fsd(1,29): Missing required parameter 'module' for attribute 'js'.");
		}

		[Test]
		public void GenerateExampleApiTypeScript_ExternDataWithoutName()
		{
			const string definition = "[csharp] service TestApi { [js(module: \"extern-dto-module\")] extern data Thing; data Test { thing: Thing; } }";
			var parser = new FsdParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "testApiTypes.ts");
			Assert.That(typesFile.Text, Does.Contain("import { IThing } from 'extern-dto-module';"));
			Assert.That(typesFile.Text, Does.Contain("thing?: IThing;"));
		}

		[Test]
		public void GenerateExampleApiTypeScript_ExternDataNameSameAsAlias()
		{
			const string definition = "[csharp] service TestApi { [js(name: \"IThing\", module: \"extern-dto-module\")] extern data Thing; data Test { thing: Thing; } }";
			var parser = new FsdParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "testApiTypes.ts");
			Assert.That(typesFile.Text, Does.Contain("import { IThing } from 'extern-dto-module';"));
			Assert.That(typesFile.Text, Does.Contain("thing?: IThing;"));
		}

		[Test]
		public void GenerateExampleApiTypeScript_ExternEnumWithNameAndModule()
		{
			const string definition = "[csharp] service TestApi { [js(name: \"SomeExternalEnum\", module: \"extern-enum-module\")] extern enum Thing; data Test { thing: Thing; } }";
			var parser = new FsdParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "testApiTypes.ts");
			Assert.That(typesFile.Text, Does.Contain("import { SomeExternalEnum as Thing } from 'extern-enum-module';"));
			Assert.That(typesFile.Text, Does.Contain("thing?: Thing;"));
		}

		[Test]
		public void GenerateExampleApiTypeScript_ExternEnumWithoutJsAttribute()
		{
			ThrowsServiceDefinitionException(
				"[csharp] service TestApi { extern enum MissingAttribute; }",
				"TestApi.fsd(1,35): Missing required attribute 'js'.");
		}

		[Test]
		public void GenerateExampleApiTypeScript_ExternEnumWithoutModule()
		{
			ThrowsServiceDefinitionException(
				"[csharp] service TestApi { [js(name: \"SomeEnum\")] extern enum MissingModule; }",
				"TestApi.fsd(1,29): Missing required parameter 'module' for attribute 'js'.");
		}

		[Test]
		public void GenerateExampleApiTypeScript_ExternEnumWithoutName()
		{
			const string definition = "[csharp] service TestApi { [js(module: \"extern-enum-module\")] extern enum Thing; data Test { thing: Thing; } }";
			var parser = new FsdParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "testApiTypes.ts");
			Assert.That(typesFile.Text, Does.Contain("import { Thing } from 'extern-enum-module';"));
			Assert.That(typesFile.Text, Does.Contain("thing?: Thing;"));
		}

		[Test]
		public void GenerateExampleApiTypeScript_ExternEnumNameIsSameAsAlias()
		{
			const string definition = "[csharp] service TestApi { [js(name: \"Thing\", module: \"extern-enum-module\")] extern enum Thing; data Test { thing: Thing; } }";
			var parser = new FsdParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "testApiTypes.ts");
			Assert.That(typesFile.Text, Does.Contain("import { Thing } from 'extern-enum-module';"));
			Assert.That(typesFile.Text, Does.Contain("thing?: Thing;"));
		}

		[Test]
		public void GenerateExampleApiTypeScript_IncludesErrorSets()
		{
			ServiceInfo service;
			const string fileName = "Facility.CodeGen.JavaScript.UnitTests.ExampleApi.fsd";
			var parser = new FsdParser();
			var stream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream(fileName)!;
			Assert.That(stream, Is.Not.Null);
			using (var reader = new StreamReader(stream))
				service = parser.ParseDefinition(new ServiceDefinitionText(Path.GetFileName(fileName), reader.ReadToEnd()));

			var generator = new JavaScriptGenerator
			{
				GeneratorName = "JavaScriptGeneratorTests",
				TypeScript = true,
				NewLine = "\n",
			};
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "exampleApiTypes.ts");
			const string expectedErrorSet = """
				/** Custom errors. */
				export enum ExampleApiErrors {
					/** The user is not an administrator. */
					NotAdmin = 'NotAdmin',
				}
				""";
			Assert.That(typesFile.Text, Contains.Substring(expectedErrorSet));
		}

		[TestCase("", true)]
		[TestCase("", false)]
		[TestCase("suffix", true)]
		[TestCase("suffix", false)]
		[TestCase(".g", true)]
		[TestCase(".g", false)]
		public void GenerateWithCustomFileNameSuffix(string suffix, bool isTypeScript)
		{
			const string definition = "service TestApi { }";
			var parser = new FsdParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = isTypeScript, Express = true, FileNameSuffix = suffix };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			Assert.That(result.Files, Has.Count.EqualTo(isTypeScript ? 3 : 2));
			var fullSuffix = suffix + (isTypeScript ? ".ts" : ".js");
			Assert.That(result.Files, Has.One.Matches<CodeGenFile>(f => f.Name == $"testApi{fullSuffix}"));
			Assert.That(result.Files, Has.One.Matches<CodeGenFile>(f => f.Name == $"testApi{fullSuffix}"));
			Assert.That(result.Files, Has.One.Matches<CodeGenFile>(f => f.Name == $"testApiServer{fullSuffix}"));

			if (isTypeScript)
				Assert.That(result.Files.SingleOrDefault(f => f.Name == $"testApiTypes{fullSuffix}"), Is.Not.Null);
		}

		private void ThrowsServiceDefinitionException(string definition, string message)
		{
			var parser = new FsdParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true };
			Action action = () => generator.GenerateOutput(service);
			action.Should().Throw<ServiceDefinitionException>().WithMessage(message);
		}
	}
}
