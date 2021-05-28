using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facility.Definition;
using Facility.Definition.CodeGen;
using Facility.Definition.Http;

namespace Facility.CodeGen.JavaScript
{
	/// <summary>
	/// Generates JavaScript and TypeScript.
	/// </summary>
	public sealed class JavaScriptGenerator : CodeGenerator
	{
		/// <summary>
		/// Generates JavaScript/TypeScript.
		/// </summary>
		/// <param name="settings">The settings.</param>
		/// <returns>The number of updated files.</returns>
		public static int GenerateJavaScript(JavaScriptGeneratorSettings settings) =>
			FileGenerator.GenerateFiles(new JavaScriptGenerator { GeneratorName = nameof(JavaScriptGenerator) }, settings);

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

		/// <summary>
		/// Generates the JavaScript/TypeScript output.
		/// </summary>
		public override CodeGenOutput GenerateOutput(ServiceInfo service)
		{
			var httpServiceInfo = HttpServiceInfo.Create(service);

			var moduleName = ModuleName ?? service.Name;
			var capModuleName = CodeGenUtility.Capitalize(moduleName);
			var typesFileName = Uncapitalize(moduleName) + "Types" + (TypeScript ? ".ts" : ".js");
			var clientFileName = Uncapitalize(moduleName) + (TypeScript ? ".ts" : ".js");
			var serverFileName = Uncapitalize(moduleName) + "Server" + (TypeScript ? ".ts" : ".js");

			var namedTexts = new List<CodeGenFile>();
			var typeNames = new List<string>();
			if (TypeScript)
			{
				namedTexts.Add(CreateFile(typesFileName, code =>
				{
					WriteFileHeader(code);

					var allFields = service.Dtos.SelectMany(x => x.Fields).Concat(service.Methods.SelectMany(x => x.RequestFields.Concat(x.ResponseFields))).ToList();

					code.WriteLine();
					var facilityImports = new List<string>();
					if (httpServiceInfo.Methods.Any() || allFields.Any(x => FieldUsesKind(service, x, ServiceTypeKind.Result)))
						facilityImports.Add("IServiceResult");
					if (allFields.Any(x => FieldUsesKind(service, x, ServiceTypeKind.Error)))
						facilityImports.Add("IServiceError");
					WriteImports(code, facilityImports, "facility-core");

					code.WriteLine();
					WriteJsDoc(code, service);
					typeNames.Add($"I{capModuleName}");
					using (code.Block($"export interface I{capModuleName} {{", "}"))
					{
						foreach (var httpMethodInfo in httpServiceInfo.Methods)
						{
							var methodName = httpMethodInfo.ServiceMethod.Name;
							var capMethodName = CodeGenUtility.Capitalize(methodName);
							code.WriteLineSkipOnce();
							WriteJsDoc(code, httpMethodInfo.ServiceMethod);
							code.WriteLine($"{methodName}(request: I{capMethodName}Request): Promise<IServiceResult<I{capMethodName}Response>>;");
						}
					}

					foreach (var methodInfo in service.Methods)
					{
						var requestDtoName = $"{CodeGenUtility.Capitalize(methodInfo.Name)}Request";
						typeNames.Add($"I{requestDtoName}");
						WriteDto(code, new ServiceDtoInfo(
							name: requestDtoName,
							fields: methodInfo.RequestFields,
							summary: $"Request for {CodeGenUtility.Capitalize(methodInfo.Name)}."), service);

						var responseDtoName = $"{CodeGenUtility.Capitalize(methodInfo.Name)}Response";
						typeNames.Add($"I{responseDtoName}");
						WriteDto(code, new ServiceDtoInfo(
							name: responseDtoName,
							fields: methodInfo.ResponseFields,
							summary: $"Response for {CodeGenUtility.Capitalize(methodInfo.Name)}."), service);
					}

					foreach (var dtoInfo in service.Dtos)
					{
						typeNames.Add($"I{dtoInfo.Name}");
						WriteDto(code, dtoInfo, service);
					}

					foreach (var enumInfo in service.Enums)
					{
						typeNames.Add(enumInfo.Name);
						code.WriteLine();
						WriteJsDoc(code, enumInfo);
						using (code.Block($"export enum {enumInfo.Name} {{", "}"))
						{
							foreach (var value in enumInfo.Values)
							{
								code.WriteLineSkipOnce();
								WriteJsDoc(code, value);
								code.WriteLine($@"{value.Name} = ""{value.Name}"",");
							}
						}
					}

					code.WriteLine();
				}));
			}

			namedTexts.Add(CreateFile(clientFileName, code =>
			{
				WriteFileHeader(code);

				if (!TypeScript)
					code.WriteLine("'use strict';");

				code.WriteLine();
				var facilityImports = new List<string> { "HttpClientUtility" };
				if (TypeScript)
				{
					if (httpServiceInfo.Methods.Any())
						facilityImports.Add("IServiceResult");
					facilityImports.Add("IHttpClientOptions");
				}
				WriteImports(code, facilityImports, "facility-core");

				if (TypeScript)
				{
					WriteImports(code, typeNames, $"./{Uncapitalize(moduleName)}Types");
					code.WriteLine($"export * from './{Uncapitalize(moduleName)}Types';");
				}

				code.WriteLine();
				WriteJsDoc(code, $"Provides access to {capModuleName} over HTTP via fetch.");
				using (code.Block("export function createHttpClient({ fetch, baseUri }" + IfTypeScript(": IHttpClientOptions") + ")" + IfTypeScript($": I{capModuleName}") + " {", "}"))
					code.WriteLine($"return new {capModuleName}HttpClient(fetch, baseUri);");

				code.WriteLine();
				code.WriteLine("const { fetchResponse, createResponseError, createRequiredRequestFieldError } = HttpClientUtility;");
				if (TypeScript)
				{
					code.WriteLine("type IFetch = HttpClientUtility.IFetch;");
					code.WriteLine("type IFetchRequest = HttpClientUtility.IFetchRequest;");
				}

				code.WriteLine();
				using (code.Block($"class {capModuleName}HttpClient" + IfTypeScript($" implements I{capModuleName}") + " {", "}"))
				{
					using (code.Block("constructor(fetch" + IfTypeScript(": IFetch") + ", baseUri" + IfTypeScript("?: string") + ") {", "}"))
					{
						using (code.Block("if (typeof fetch !== 'function') {", "}"))
							code.WriteLine("throw new TypeError('fetch must be a function.');");
						using (code.Block("if (typeof baseUri === 'undefined') {", "}"))
							code.WriteLine($"baseUri = '{httpServiceInfo.Url ?? ""}';");
						using (code.Block(@"if (/[^\/]$/.test(baseUri)) {", "}"))
							code.WriteLine("baseUri += '/';");

						code.WriteLine("this._fetch = fetch;");
						code.WriteLine("this._baseUri = baseUri;");
					}

					foreach (var httpMethodInfo in httpServiceInfo.Methods)
					{
						var methodName = httpMethodInfo.ServiceMethod.Name;
						var capMethodName = CodeGenUtility.Capitalize(methodName);

						code.WriteLine();
						WriteJsDoc(code, httpMethodInfo.ServiceMethod);
						using (code.Block(IfTypeScript("public ") + $"{methodName}(request" + IfTypeScript($": I{capMethodName}Request") + ")" + IfTypeScript($": Promise<IServiceResult<I{capMethodName}Response>>") + " {", "}"))
						{
							var hasPathFields = httpMethodInfo.PathFields.Count != 0;
							var jsUriDelim = hasPathFields ? "`" : "'";
							var jsUri = jsUriDelim + httpMethodInfo.Path.Substring(1) + jsUriDelim;
							if (hasPathFields)
							{
								foreach (var httpPathField in httpMethodInfo.PathFields)
								{
									code.WriteLine($"const uriPart{CodeGenUtility.Capitalize(httpPathField.ServiceField.Name)} = request.{httpPathField.ServiceField.Name} != null && {RenderUriComponent(httpPathField.ServiceField, service)};");
									using (code.Block($"if (!uriPart{CodeGenUtility.Capitalize(httpPathField.ServiceField.Name)}) {{", "}"))
										code.WriteLine($"return Promise.resolve(createRequiredRequestFieldError('{httpPathField.ServiceField.Name}'));");
								}
								foreach (var httpPathField in httpMethodInfo.PathFields)
									jsUri = jsUri.Replace("{" + httpPathField.Name + "}", $"${{uriPart{CodeGenUtility.Capitalize(httpPathField.ServiceField.Name)}}}");
							}

							var hasQueryFields = httpMethodInfo.QueryFields.Count != 0;
							code.WriteLine((hasQueryFields ? "let" : "const") + $" uri = {jsUri};");
							if (hasQueryFields)
							{
								code.WriteLine("const query" + IfTypeScript(": string[]") + " = [];");
								foreach (var httpQueryField in httpMethodInfo.QueryFields)
									code.WriteLine($"request.{httpQueryField.ServiceField.Name} == null || query.push('{httpQueryField.Name}=' + {RenderUriComponent(httpQueryField.ServiceField, service)});");
								using (code.Block("if (query.length) {", "}"))
									code.WriteLine("uri = uri + '?' + query.join('&');");
							}

							using (code.Block("const fetchRequest" + IfTypeScript(": IFetchRequest") + " = {", "};"))
							{
								if (httpMethodInfo.RequestBodyField == null && httpMethodInfo.RequestNormalFields.Count == 0)
								{
									code.WriteLine($"method: '{httpMethodInfo.Method}',");
									if (httpMethodInfo.RequestHeaderFields.Count != 0)
										code.WriteLine("headers: {},");
								}
								else
								{
									code.WriteLine($"method: '{httpMethodInfo.Method}',");
									code.WriteLine("headers: { 'Content-Type': 'application/json' },");

									if (httpMethodInfo.RequestBodyField != null)
									{
										code.WriteLine($"body: JSON.stringify(request.{httpMethodInfo.RequestBodyField.ServiceField.Name})");
									}
									else if (httpMethodInfo.ServiceMethod.RequestFields.Count == httpMethodInfo.RequestNormalFields.Count)
									{
										code.WriteLine("body: JSON.stringify(request)");
									}
									else
									{
										using (code.Block("body: JSON.stringify({", "})"))
										{
											for (var httpFieldIndex = 0; httpFieldIndex < httpMethodInfo.RequestNormalFields.Count; httpFieldIndex++)
											{
												var httpFieldInfo = httpMethodInfo.RequestNormalFields[httpFieldIndex];
												var isLastField = httpFieldIndex == httpMethodInfo.RequestNormalFields.Count - 1;
												var fieldName = httpFieldInfo.ServiceField.Name;
												code.WriteLine(fieldName + ": request." + fieldName + (isLastField ? "" : ","));
											}
										}
									}
								}
							}

							if (httpMethodInfo.RequestHeaderFields.Count != 0)
							{
								foreach (var httpHeaderField in httpMethodInfo.RequestHeaderFields)
								{
									using (code.Block($"if (request.{httpHeaderField.ServiceField.Name} != null) {{", "}"))
										code.WriteLine($"fetchRequest.headers['{httpHeaderField.Name}'] = request.{httpHeaderField.ServiceField.Name};");
								}
							}

							code.WriteLine("return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)");
							using (code.Indent())
							using (code.Block(".then(result => {", "});"))
							{
								code.WriteLine("const status = result.response.status;");
								code.WriteLine("let value" + IfTypeScript($": I{capMethodName}Response | null") + " = null;");
								using (code.Block("if (result.json) {", "}"))
								{
									var validResponses = httpMethodInfo.ValidResponses;
									var elsePrefix = "";
									foreach (var validResponse in validResponses)
									{
										var statusCodeAsString = ((int) validResponse.StatusCode).ToString(CultureInfo.InvariantCulture);
										code.WriteLine($"{elsePrefix}if (status === {statusCodeAsString}) {{");
										elsePrefix = "else ";

										using (code.Indent())
										{
											var bodyField = validResponse.BodyField;
											if (bodyField != null)
											{
												var responseBodyFieldName = bodyField.ServiceField.Name;

												var bodyFieldType = service.GetFieldType(bodyField.ServiceField)!;
												if (bodyFieldType.Kind == ServiceTypeKind.Boolean)
													code.WriteLine($"value = {{ {responseBodyFieldName}: true }};");
												else
													code.WriteLine($"value = {{ {responseBodyFieldName}: result.json }};");
											}
											else
											{
												if (validResponse.NormalFields!.Count == 0)
													code.WriteLine("value = {};");
												else
													code.WriteLine("value = result.json;");
											}
										}
										code.WriteLine("}");
									}
								}

								using (code.Block("if (!value) {", "}"))
									code.WriteLine("return createResponseError(status, result.json)" + IfTypeScript($" as IServiceResult<I{capMethodName}Response>") + ";");

								if (httpMethodInfo.ResponseHeaderFields.Count != 0)
								{
									code.WriteLine("let headerValue" + IfTypeScript(": string | null | undefined") + ";");
									foreach (var httpHeaderField in httpMethodInfo.ResponseHeaderFields)
									{
										code.WriteLine($"headerValue = result.response.headers.get('{httpHeaderField.Name}');");
										using (code.Block("if (headerValue != null) {", "}"))
											code.WriteLine($"value.{httpHeaderField.ServiceField.Name} = headerValue;");
									}
								}

								code.WriteLine("return { value: value };");
							}
						}
					}

					if (TypeScript)
					{
						code.WriteLine();
						code.WriteLine("private _fetch: IFetch;");
						code.WriteLine("private _baseUri: string;");
					}
				}
			}));

			if (Express)
			{
				namedTexts.Add(CreateFile(serverFileName, code =>
				{
					WriteFileHeader(code);

					if (!TypeScript)
						code.WriteLine("'use strict';");

					code.WriteLine();
					code.WriteLine("import * as bodyParser from 'body-parser';");
					code.WriteLine("import * as express from 'express';");
					var facilityImports = new List<string>();
					if (TypeScript)
						facilityImports.Add("IServiceResult");
					WriteImports(code, facilityImports, "facility-core");
					if (TypeScript)
					{
						WriteImports(code, typeNames, $"./{Uncapitalize(moduleName)}Types");
						code.WriteLine($"export * from './{Uncapitalize(moduleName)}Types';");
					}

					// TODO: export this from facility-core
					code.WriteLine();
					using (code.Block("const standardErrorCodes" + IfTypeScript(": { [code: string]: number }") + " = {", "};"))
					{
						code.WriteLine("'NotModified': 304,");
						code.WriteLine("'InvalidRequest': 400,");
						code.WriteLine("'NotAuthenticated': 401,");
						code.WriteLine("'NotAuthorized': 403,");
						code.WriteLine("'NotFound': 404,");
						code.WriteLine("'Conflict': 409,");
						code.WriteLine("'RequestTooLarge': 413,");
						code.WriteLine("'TooManyRequests': 429,");
						code.WriteLine("'InternalError': 500,");
						code.WriteLine("'ServiceUnavailable': 503,");
					}

					code.WriteLine();
					using (code.Block("function parseBoolean(value" + IfTypeScript(": string | undefined") + ") {", "}"))
					{
						using (code.Block("if (typeof value === 'string') {", "}"))
						{
							code.WriteLine("const lowerValue = value.toLowerCase();");
							using (code.Block("if (lowerValue === 'true') {", "}"))
								code.WriteLine("return true;");
							using (code.Block("if (lowerValue === 'false') {", "}"))
								code.WriteLine("return false;");
						}
						code.WriteLine("return undefined;");
					}

					code.WriteLine();
					using (code.Block("export function createApp(service" + IfTypeScript($": I{capModuleName}") + ")" + IfTypeScript(": express.Application") + " {", "}"))
					{
						code.WriteLine("const app = express();");
						code.WriteLine("app.use(bodyParser.json());");
						code.WriteLine("app.use(bodyParser.urlencoded({ extended: true }));");

						foreach (var httpMethodInfo in httpServiceInfo.Methods)
						{
							var methodName = httpMethodInfo.ServiceMethod.Name;
							var capMethodName = CodeGenUtility.Capitalize(methodName);
							var expressMethod = httpMethodInfo.Method.ToLowerInvariant();
							var expressPath = httpMethodInfo.Path;
							foreach (var httpPathField in httpMethodInfo.PathFields)
								expressPath = expressPath.Replace("{" + httpPathField.Name + "}", $":{httpPathField.Name}");

							code.WriteLine();
							WriteJsDoc(code, httpMethodInfo.ServiceMethod);
							using (code.Block($"app.{expressMethod}('{expressPath}', function (req, res, next) {{", "});"))
							{
								code.WriteLine("const request" + IfTypeScript($": I{capMethodName}Request") + " = {};");

								foreach (var httpPathField in httpMethodInfo.PathFields)
									code.WriteLine($"request.{httpPathField.ServiceField.Name} = {RenderJsConversion(httpPathField.ServiceField, service, $"req.params.{httpPathField.Name}")};");

								foreach (var httpQueryField in httpMethodInfo.QueryFields)
								{
									using (code.Block($"if (req.query['{httpQueryField.Name}'] != null) {{", "}"))
										code.WriteLine($"request.{httpQueryField.ServiceField.Name} = {RenderJsConversion(httpQueryField.ServiceField, service, $"req.query['{httpQueryField.Name}']")};");
								}

								if (httpMethodInfo.RequestBodyField != null)
								{
									code.WriteLine($"request.{httpMethodInfo.RequestBodyField.ServiceField.Name} = req.body;");
								}
								else if (httpMethodInfo.RequestNormalFields != null)
								{
									foreach (var field in httpMethodInfo.RequestNormalFields)
										code.WriteLine($"request.{field.ServiceField.Name} = req.body.{field.ServiceField.Name};");
								}

								if (httpMethodInfo.RequestHeaderFields != null)
								{
									foreach (var field in httpMethodInfo.RequestHeaderFields)
										code.WriteLine($"request.{field.ServiceField.Name} = req.header('{field.Name}');");
								}

								code.WriteLine();
								code.WriteLine($"return service.{methodName}(request)");

								using (code.Indent())
								{
									using (code.Block(".then(result => {", "})"))
									{
										using (code.Block("if (result.error) {", "}"))
										{
											code.WriteLine("const status = result.error.code && standardErrorCodes[result.error.code] || 500;");
											code.WriteLine("res.status(status).send(result.error);");
											code.WriteLine("return;");
										}
										using (code.Block("if (result.value) {", "}"))
										{
											if (httpMethodInfo.ResponseHeaderFields != null)
											{
												foreach (var field in httpMethodInfo.ResponseHeaderFields)
												{
													using (code.Block($"if (result.value.{field.ServiceField.Name} != null) {{", "}"))
														code.WriteLine($"res.setHeader('{field.Name}', result.value.{field.ServiceField.Name});");
												}
											}

											foreach (var validResponse in httpMethodInfo.ValidResponses.Where(x => x.NormalFields == null || x.NormalFields.Count == 0))
											{
												var bodyField = validResponse.BodyField;
												if (bodyField != null)
												{
													var responseBodyFieldName = bodyField.ServiceField.Name;

													using (code.Block($"if (result.value.{responseBodyFieldName}) {{", "}"))
													{
														var bodyFieldType = service.GetFieldType(bodyField.ServiceField)!;
														if (bodyFieldType.Kind == ServiceTypeKind.Boolean)
														{
															code.WriteLine($"res.sendStatus({(int) validResponse.StatusCode});");
															code.WriteLine("return;");
														}
														else
														{
															code.WriteLine($"res.status({(int) validResponse.StatusCode}).send(result.value.{responseBodyFieldName});");
															code.WriteLine("return;");
														}
													}
												}
												else
												{
													if (validResponse.NormalFields!.Count == 0)
													{
														code.WriteLine($"res.sendStatus({(int) validResponse.StatusCode});");
														code.WriteLine("return;");
													}
												}
											}

											foreach (var validResponse in httpMethodInfo.ValidResponses.Where(x => x.NormalFields != null && x.NormalFields.Count != 0))
											{
												code.WriteLine($"res.status({(int) validResponse.StatusCode}).send({{");
												using (code.Indent())
												{
													foreach (var field in validResponse.NormalFields!)
													{
														code.WriteLine($"{field.ServiceField.Name}: result.value.{field.ServiceField.Name},");
													}
												}
												code.WriteLine("});");
												code.WriteLine("return;");
											}
										}

										code.WriteLine("throw new Error('Result must have an error or value.');");
									}
									code.WriteLine(".catch(next);");
								}
							}
						}

						code.WriteLine();
						code.WriteLine("return app;");
					}
				}));
			}

			return new CodeGenOutput(namedTexts, new List<CodeGenPattern>());
		}

		/// <summary>
		/// Applies generator-specific settings.
		/// </summary>
		public override void ApplySettings(FileGeneratorSettings settings)
		{
			var ourSettings = (JavaScriptGeneratorSettings) settings;
			ModuleName = ourSettings.ModuleName;
			TypeScript = ourSettings.TypeScript;
			Express = ourSettings.Express;
			DisableESLint = ourSettings.DisableESLint;
		}

		/// <summary>
		/// Supports writing output to a single file.
		/// </summary>
		public override bool SupportsSingleOutput => true;

		private void WriteFileHeader(CodeWriter code)
		{
			code.WriteLine($"// DO NOT EDIT: generated by {GeneratorName}");
			if (DisableESLint)
				code.WriteLine("/* eslint-disable */");
		}

		private void WriteDto(CodeWriter code, ServiceDtoInfo dtoInfo, ServiceInfo service)
		{
			code.WriteLine();
			WriteJsDoc(code, dtoInfo);
			using (code.Block($"export interface I{CodeGenUtility.Capitalize(dtoInfo.Name)} {{", "}"))
			{
				foreach (var fieldInfo in dtoInfo.Fields)
				{
					code.WriteLineSkipOnce();
					WriteJsDoc(code, fieldInfo);
					code.WriteLine($"{fieldInfo.Name}?: {RenderFieldType(service.GetFieldType(fieldInfo)!)};");
				}
			}
		}

		private string IfTypeScript(string value)
		{
			return TypeScript ? value : "";
		}

		private string RenderFieldType(ServiceTypeInfo fieldType)
		{
			switch (fieldType.Kind)
			{
			case ServiceTypeKind.String:
			case ServiceTypeKind.Bytes:
				return "string";
			case ServiceTypeKind.Enum:
				return fieldType.Enum!.Name;
			case ServiceTypeKind.Boolean:
				return "boolean";
			case ServiceTypeKind.Double:
			case ServiceTypeKind.Int32:
			case ServiceTypeKind.Int64:
			case ServiceTypeKind.Decimal:
				return "number";
			case ServiceTypeKind.Object:
				return "{ [name: string]: any }";
			case ServiceTypeKind.Error:
				return "IServiceError";
			case ServiceTypeKind.Dto:
				return $"I{CodeGenUtility.Capitalize(fieldType.Dto!.Name)}";
			case ServiceTypeKind.Result:
				return $"IServiceResult<{RenderFieldType(fieldType.ValueType!)}>";
			case ServiceTypeKind.Array:
				return $"{RenderFieldType(fieldType.ValueType!)}[]";
			case ServiceTypeKind.Map:
				return $"{{ [name: string]: {RenderFieldType(fieldType.ValueType!)} }}";
			default:
				throw new NotSupportedException("Unknown field type " + fieldType.Kind);
			}
		}

		private string RenderUriComponent(ServiceFieldInfo field, ServiceInfo service)
		{
			var fieldTypeKind = service.GetFieldType(field)!.Kind;
			var fieldName = field.Name;

			switch (fieldTypeKind)
			{
			case ServiceTypeKind.Enum:
				return $"request.{fieldName}";
			case ServiceTypeKind.String:
			case ServiceTypeKind.Bytes:
				return $"encodeURIComponent(request.{fieldName})";
			case ServiceTypeKind.Boolean:
			case ServiceTypeKind.Int32:
			case ServiceTypeKind.Int64:
			case ServiceTypeKind.Decimal:
				return $"request.{fieldName}.toString()";
			case ServiceTypeKind.Double:
				return $"encodeURIComponent(request.{fieldName}.toString())";
			case ServiceTypeKind.Dto:
			case ServiceTypeKind.Error:
			case ServiceTypeKind.Object:
				throw new NotSupportedException("Field type not supported on path/query: " + fieldTypeKind);
			default:
				throw new NotSupportedException("Unknown field type " + fieldTypeKind);
			}
		}

		private string RenderJsConversion(ServiceFieldInfo field, ServiceInfo service, string value)
		{
			var fieldTypeKind = service.GetFieldType(field)!.Kind;

			switch (fieldTypeKind)
			{
			case ServiceTypeKind.Enum:
			case ServiceTypeKind.String:
			case ServiceTypeKind.Bytes:
				return value;
			case ServiceTypeKind.Boolean:
				return $"parseBoolean({value})";
			case ServiceTypeKind.Int32:
			case ServiceTypeKind.Int64:
				return $"parseInt({value}, 10)";
			case ServiceTypeKind.Decimal:
			case ServiceTypeKind.Double:
				return $"parseFloat({value})";
			case ServiceTypeKind.Dto:
			case ServiceTypeKind.Error:
			case ServiceTypeKind.Object:
				throw new NotSupportedException("Field type not supported on path/query: " + fieldTypeKind);
			default:
				throw new NotSupportedException("Unknown field type " + fieldTypeKind);
			}
		}

		private static void WriteJsDoc(CodeWriter code, ServiceElementWithAttributesInfo element)
		{
			WriteJsDoc(code, (element as IServiceHasSummary)?.Summary, isObsolete: element.IsObsolete, obsoleteMessage: element.ObsoleteMessage);
		}

		private static void WriteJsDoc(CodeWriter code, string? summary, bool isObsolete = false, string? obsoleteMessage = null)
		{
			var lines = new List<string>(capacity: 2);
			if (summary != null && summary.Trim().Length != 0)
				lines.Add(summary.Trim());
			if (isObsolete)
				lines.Add("@deprecated" + (string.IsNullOrWhiteSpace(obsoleteMessage) ? "" : $" {obsoleteMessage}"));
			WriteJsDoc(code, lines);
		}

		private static void WriteJsDoc(CodeWriter code, IReadOnlyList<string> lines)
		{
			if (lines.Count == 1)
			{
				code.WriteLine($"/** {lines[0]} */");
			}
			else if (lines.Count != 0)
			{
				code.WriteLine("/**");
				foreach (var line in lines)
					code.WriteLine($" * {line}");
				code.WriteLine(" */");
			}
		}

		private static void WriteImports(CodeWriter code, IReadOnlyList<string> imports, string from)
		{
			if (imports.Count != 0)
				code.WriteLine($"import {{ {string.Join(", ", imports)} }} from '{from}';");
		}

		private static string Uncapitalize(string value)
		{
			return value.Length == 0 ? "" : value.Substring(0, 1).ToLowerInvariant() + value.Substring(1);
		}

		private static bool FieldUsesKind(ServiceInfo service, ServiceFieldInfo field, ServiceTypeKind kind)
		{
			var type = service.GetFieldType(field)!;
			if (type.Kind == kind)
				return true;

			var valueType = type;
			while (true)
			{
				if (valueType.ValueType == null)
					break;
				valueType = valueType.ValueType;
				if (valueType.Kind == kind)
					return true;
			}

			return false;
		}
	}
}
