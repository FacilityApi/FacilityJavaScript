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
			var parser = CreateParser();
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
			var parser = CreateParser();
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
			var parser = CreateParser();
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
			var parser = CreateParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "testApiTypes.ts");
			Assert.That(typesFile.Text, Does.Contain("import { SomeExternalDto as IThing } from 'extern-dto-module';"));
			Assert.That(typesFile.Text, Does.Contain("thing?: IThing;"));
		}

		[Test]
		public void GenerateExampleApiTypeScript_OmitHttpClient()
		{
			ServiceInfo service;
			const string fileName = "Facility.CodeGen.JavaScript.UnitTests.ExampleApi.fsd";
			var parser = CreateParser();
			var stream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream(fileName)!;
			Assert.That(stream, Is.Not.Null);
			using (var reader = new StreamReader(stream))
				service = parser.ParseDefinition(new ServiceDefinitionText(Path.GetFileName(fileName), reader.ReadToEnd()));

			var generator = new JavaScriptGenerator
			{
				GeneratorName = "JavaScriptGeneratorTests",
				TypeScript = true,
				NoHttp = true,
				NewLine = "\n",
			};
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var files = result.Files.Select(x => x.Name).ToList();
			Assert.That(files, Does.Not.Contain("exampleApi.ts"));
			Assert.That(files, Does.Contain("exampleApiTypes.ts"));
		}

		[Test]
		public void GenerateExampleApiTypeScript_OmitHttpClientWithExpress()
		{
			ServiceInfo service;
			const string fileName = "Facility.CodeGen.JavaScript.UnitTests.ExampleApi.fsd";
			var parser = CreateParser();
			var stream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream(fileName)!;
			Assert.That(stream, Is.Not.Null);
			using (var reader = new StreamReader(stream))
				service = parser.ParseDefinition(new ServiceDefinitionText(Path.GetFileName(fileName), reader.ReadToEnd()));

			var generator = new JavaScriptGenerator
			{
				GeneratorName = "JavaScriptGeneratorTests",
				TypeScript = true,
				NoHttp = true,
				Express = true,
				NewLine = "\n",
			};
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var expressServerContents = result.Files.Single(x => x.Name == "exampleApiServer.ts");
			Assert.That(expressServerContents.Text, Does.Not.Contain("from 'exampleApi';"));
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
			var parser = CreateParser();
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
			var parser = CreateParser();
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
			var parser = CreateParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "testApiTypes.ts");
			Assert.That(typesFile.Text, Does.Contain("import { SomeExternalEnum as Thing } from 'extern-enum-module';"));
			Assert.That(typesFile.Text, Does.Contain("thing?: Thing;"));
		}

		[Test]
		public void GenerateExampleApiTypeScript_ExternEnumAsUriParam()
		{
			const string definition = "[csharp] service TestApi { [js(name: \"SomeExternalEnum\", module: \"extern-enum-module\")] extern enum Thing; [http(method: GET, path: \"/myMethod\")] method myMethod { e: Thing; }: {} }";
			var parser = CreateParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true, Express = true };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "testApiTypes.ts");
			Assert.That(typesFile.Text, Does.Contain("export interface IMyMethodRequest"));
			Assert.That(typesFile.Text, Does.Contain("e?: Thing;"));

			var serverFile = result.Files.Single(f => f.Name == "testApiServer.ts");
			Assert.That(serverFile.Text, Does.Contain("request.e = req.query['e'] as Thing;"));
		}

		[Test]
		public void GenerateExampleApiTypeScript_ExternEnumAsHeader()
		{
			const string definition = "[csharp] service TestApi { [js(name: \"SomeExternalEnum\", module: \"extern-enum-module\")] extern enum Thing; [http(method: GET, path: \"/myMethod\")] method myMethod { [http(from: header, name: \"Thing-Header\")] e: Thing; }: {} }";
			var parser = CreateParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true, Express = true };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "testApiTypes.ts");
			Assert.That(typesFile.Text, Does.Contain("export interface IMyMethodRequest"));
			Assert.That(typesFile.Text, Does.Contain("e?: Thing;"));

			var serverFile = result.Files.Single(f => f.Name == "testApiServer.ts");
			Assert.That(serverFile.Text, Does.Contain("request.e = req.header('Thing-Header');"));
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
			var parser = CreateParser();
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
			var parser = CreateParser();
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
			var parser = CreateParser();
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

		[Test]
		public void GenerateExampleApiTypeScript_DataPropertiesOptional()
		{
			const string definition = "service TestApi { data Widget { id: string; name: string; price: decimal; } }";
			var parser = CreateParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "testApiTypes.ts");
			Assert.That(typesFile.Text, Does.Contain("export interface IWidget {"));
			Assert.That(typesFile.Text, Does.Contain("id?: string;"));
			Assert.That(typesFile.Text, Does.Contain("name?: string;"));
			Assert.That(typesFile.Text, Does.Contain("price?: number;"));
			Assert.That(typesFile.Text, Does.Contain("}"));
		}

		[Test]
		public void GenerateExampleApiTypeScript_DataPropertiesRequired()
		{
			const string definition = "service TestApi { data Widget { [required] id: string; name: string!; price: decimal; } }";
			var parser = CreateParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var typesFile = result.Files.Single(f => f.Name == "testApiTypes.ts");
			Assert.That(typesFile.Text, Does.Contain("export interface IWidget {"));
			Assert.That(typesFile.Text, Does.Contain("id: string;"));
			Assert.That(typesFile.Text, Does.Contain("name: string;"));
			Assert.That(typesFile.Text, Does.Contain("price?: number;"));
			Assert.That(typesFile.Text, Does.Contain("}"));
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
			var parser = CreateParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = isTypeScript, Express = true, FileNameSuffix = suffix };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			Assert.That(result.Files, Has.Count.EqualTo(isTypeScript ? 3 : 2));
			var fullSuffix = suffix + (isTypeScript ? ".ts" : ".js");
			Assert.That(result.Files, Has.One.Matches<CodeGenFile>(f => f.Name == $"testApi{fullSuffix}"));
			Assert.That(result.Files, Has.One.Matches<CodeGenFile>(f => f.Name == $"testApiServer{fullSuffix}"));

			if (isTypeScript)
				Assert.That(result.Files.SingleOrDefault(f => f.Name == $"testApiTypes{fullSuffix}"), Is.Not.Null);
		}

		[Test]
		public void GenerateWithCustomFileNameSuffix_TypeScriptFileNameReferencesCorrect()
		{
			const string definition = "service TestApi { }";
			var parser = CreateParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true, Express = true, FileNameSuffix = ".g" };
			var result = generator.GenerateOutput(service);
			Assert.That(result, Is.Not.Null);

			var clientFile = result.Files.Single(x => x.Name == "testApi.g.ts");
			Assert.That(clientFile.Text, Does.Contain("export * from './testApiTypes.g';"));

			var serverFile = result.Files.Single(x => x.Name == "testApiServer.g.ts");
			Assert.That(serverFile.Text, Does.Contain("export * from './testApiTypes.g';"));
		}

		private void ThrowsServiceDefinitionException(string definition, string message)
		{
			var parser = CreateParser();
			var service = parser.ParseDefinition(new ServiceDefinitionText("TestApi.fsd", definition));
			var generator = new JavaScriptGenerator { GeneratorName = "JavaScriptGeneratorTests", TypeScript = true };
			Action action = () => generator.GenerateOutput(service);
			action.Should().Throw<ServiceDefinitionException>().WithMessage(message);
		}

		private static FsdParser CreateParser() => new FsdParser(new FsdParserSettings { SupportsEvents = true });
	}
}
