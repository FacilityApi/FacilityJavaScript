using Facility.Definition.CodeGen;

namespace Facility.CodeGen.JavaScript
{
	/// <summary>
	/// Settings for generating JavaScript and TypeScript.
	/// </summary>
	public sealed class JavaScriptGeneratorSettings : FileGeneratorSettings
	{
		/// <summary>
		/// The name of the module (optional).
		/// </summary>
		public string? ModuleName { get; set; }

		/// <summary>
		/// True to generate TypeScript.
		/// </summary>
		public bool TypeScript { get; set; }

		/// <summary>
		/// True to generate Express service.
		/// </summary>
		public bool Express { get; set; }

		/// <summary>
		/// True to disable ESLint via code comment.
		/// </summary>
		public bool DisableESLint { get; set; }
	}
}
