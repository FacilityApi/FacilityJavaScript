using System.IO;
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
			generator.GenerateOutput(service);
		}
	}
}
