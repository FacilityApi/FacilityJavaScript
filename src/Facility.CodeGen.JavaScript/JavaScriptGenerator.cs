using System.Globalization;
using Facility.Definition;
using Facility.Definition.CodeGen;
using Facility.Definition.Fsd;
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
		/// <param name="parser">The parser.</param>
		/// <param name="settings">The settings.</param>
		/// <returns>The number of updated files.</returns>
		public static int GenerateCSharp(ServiceParser parser, JavaScriptGeneratorSettings settings) =>
			FileGenerator.GenerateFiles(parser, new JavaScriptGenerator { GeneratorName = nameof(JavaScriptGenerator) }, settings);

		/// <summary>
		/// Generates JavaScript/TypeScript.
		/// </summary>
		/// <param name="settings">The settings.</param>
		/// <returns>The number of updated files.</returns>
		[Obsolete("Use the overload that takes a parser.")]
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
		/// True to omit generated HTTP code.
		/// </summary>
		public bool NoHttp { get; set; }

		/// <summary>
		/// True to generate Express service.
		/// </summary>
		public bool Express { get; set; }

		/// <summary>
		/// True to generate Fastify plugin.
		/// </summary>
		/// <remarks>
		/// When specified, only the server plugin is generated, not the client.
		/// </remarks>
		public bool Fastify { get; set; }

		/// <summary>
		/// True to disable ESLint via code comment.
		/// </summary>
		public bool DisableESLint { get; set; }

		/// <summary>
		/// Suffix to append to generated file names before the extension.
		/// </summary>
		public string? FileNameSuffix { get; set; }

		/// <summary>
		/// Generates the JavaScript/TypeScript output.
		/// </summary>
		public override CodeGenOutput GenerateOutput(ServiceInfo service)
		{
			if (Fastify)
				return GenerateFastifyPluginOutput(service);

			var httpServiceInfo = HttpServiceInfo.Create(service);

			var moduleName = ModuleName ?? service.Name;
			var capModuleName = CodeGenUtility.Capitalize(moduleName);
			var typesFileNameNoExt = CodeGenUtility.Uncapitalize(moduleName) + "Types" + (FileNameSuffix ?? "");
			var typesFileName = typesFileNameNoExt + (TypeScript ? ".ts" : ".js");
			var clientFileName = CodeGenUtility.Uncapitalize(moduleName) + (FileNameSuffix ?? "") + (TypeScript ? ".ts" : ".js");
			var serverFileName = CodeGenUtility.Uncapitalize(moduleName) + "Server" + (FileNameSuffix ?? "") + (TypeScript ? ".ts" : ".js");

			var errors = new List<ServiceDefinitionError>();
			var namedTexts = new List<CodeGenFile>();
			var typeNames = new List<string>();

			if (TypeScript)
			{
				var externImports = new List<(string Name, string Alias, string Module)>();
				var externTypes = service.ExternalDtos.Concat<ServiceMemberInfo>(service.ExternalEnums);
				foreach (var externalDtoInfo in externTypes)
				{
					var jsAttribute = externalDtoInfo.TryGetAttribute(c_jsAttributeName);
					if (jsAttribute is null)
					{
						errors.Add(new ServiceDefinitionError($"Missing required attribute '{c_jsAttributeName}'.", externalDtoInfo.Position));
						continue;
					}

					var module = jsAttribute.TryGetParameterValue(c_jsAttributeModuleParameterName);
					if (module is null)
					{
						errors.Add(new ServiceDefinitionError($"Missing required parameter '{c_jsAttributeModuleParameterName}' for attribute '{c_jsAttributeName}'.", jsAttribute.Position));
						continue;
					}

					var alias = externalDtoInfo is ServiceExternalDtoInfo
						? $"I{CodeGenUtility.Capitalize(externalDtoInfo.Name)}"
						: CodeGenUtility.Capitalize(externalDtoInfo.Name);

					var name = jsAttribute.TryGetParameterValue(c_jsAttributeNameParameterName) ?? alias;
					externImports.Add((name, alias, module));
				}

				if (errors.Count > 0)
					throw new ServiceDefinitionException(errors);

				namedTexts.Add(CreateFile(typesFileName, code =>
				{
					WriteFileHeader(code);

					var allFields = service.Dtos.SelectMany(x => x.Fields)
						.Concat(service.Methods.SelectMany(x => x.RequestFields.Concat(x.ResponseFields)))
						.Concat(service.Events.SelectMany(x => x.RequestFields.Concat(x.ResponseFields)))
						.ToList();

					code.WriteLine();
					var facilityImports = new List<string>();
					if (httpServiceInfo.Methods.Any() || httpServiceInfo.Events.Any() || allFields.Any(x => FieldUsesKind(service, x, ServiceTypeKind.Result)))
						facilityImports.Add("IServiceResult");
					if (allFields.Any(x => FieldUsesKind(service, x, ServiceTypeKind.Error)))
						facilityImports.Add("IServiceError");
					WriteImports(code, facilityImports, "facility-core");

					// Imports for extern data/enum
					foreach (var import in externImports.GroupBy(x => x.Module))
						WriteImports(code, import.Select(x => $"{x.Name}{(x.Name != x.Alias ? $" as {x.Alias}" : "")}").ToArray(), import.Key);

					typeNames.AddRange(WriteTypes(code, httpServiceInfo));
					code.WriteLine();
				}));
			}

			if (!NoHttp)
			{
				namedTexts.Add(CreateFile(clientFileName, code =>
				{
					WriteFileHeader(code);

					if (!TypeScript)
						code.WriteLine("'use strict';");

					code.WriteLine();
					var facilityImports = new List<string> { "HttpClientUtility" };
					if (TypeScript)
					{
						if (httpServiceInfo.Methods.Any() || httpServiceInfo.Events.Any())
							facilityImports.Add("IServiceResult");
						facilityImports.Add("IHttpClientOptions");
					}
					WriteImports(code, facilityImports, "facility-core");

					if (TypeScript)
					{
						WriteImports(code, typeNames, $"./{typesFileNameNoExt}");
						code.WriteLine($"export * from './{typesFileNameNoExt}';");
					}

					code.WriteLine();
					WriteJsDoc(code, $"Provides access to {capModuleName} over HTTP via fetch.");
					using (code.Block("export function createHttpClient(options" + IfTypeScript(": IHttpClientOptions") + ")" + IfTypeScript($": I{capModuleName}") + " {", "}"))
						code.WriteLine($"return new {capModuleName}HttpClient(options);");

					code.WriteLine();
					var utilityImports = new List<string> { "fetchResponse", "createResponseError", "createRequiredRequestFieldError" };
					if (httpServiceInfo.Events.Count > 0)
						utilityImports.Add("createFetchEventStream");
					code.WriteLine($"const {{ {string.Join(", ", utilityImports)} }} = HttpClientUtility;");
					if (TypeScript)
					{
						code.WriteLine("type IFetch = HttpClientUtility.IFetch;");
						code.WriteLine("type IFetchRequest = HttpClientUtility.IFetchRequest;");
					}

					// TODO: export this from facility-core?
					if (httpServiceInfo.Methods.Any(x => x.RequestHeaderFields.Any(y => service.GetFieldType(y.ServiceField)!.Kind == ServiceTypeKind.Boolean)))
					{
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
					}

					code.WriteLine();
					WriteJsDoc(code, $"Provides access to {capModuleName} over HTTP via fetch.");
					using (code.Block($"export class {capModuleName}HttpClient" + IfTypeScript($" implements I{capModuleName}") + " {", "}"))
					{
						using (code.Block("constructor({ fetch, baseUri }" + IfTypeScript(": IHttpClientOptions") + ") {", "}"))
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
							using (code.Block(IfTypeScript("public ") + $"{methodName}(request" + IfTypeScript($": I{capMethodName}Request") + ", context" + IfTypeScript("?: unknown") + ")" + IfTypeScript($": Promise<IServiceResult<I{capMethodName}Response>>") + " {", "}"))
							{
								var hasPathFields = httpMethodInfo.PathFields.Count != 0;
								var jsUriDelim = hasPathFields ? "`" : "'";
#if !NETSTANDARD2_0
								var jsUri = string.Concat(jsUriDelim, httpMethodInfo.Path.AsSpan(1), jsUriDelim);
#else
								var jsUri = jsUriDelim + httpMethodInfo.Path.Substring(1) + jsUriDelim;
#endif
								if (hasPathFields)
								{
									foreach (var httpPathField in httpMethodInfo.PathFields)
									{
										code.WriteLine($"const uriPart{CodeGenUtility.Capitalize(httpPathField.ServiceField.Name)} = request.{httpPathField.ServiceField.Name} != null && {RenderUriComponent(httpPathField.ServiceField, service)};");
										using (code.Block($"if (!uriPart{CodeGenUtility.Capitalize(httpPathField.ServiceField.Name)}) {{", "}"))
											code.WriteLine($"return Promise.resolve(createRequiredRequestFieldError('{httpPathField.ServiceField.Name}'));");
									}
									foreach (var httpPathField in httpMethodInfo.PathFields)
										jsUri = ReplaceOrdinal(jsUri, "{" + httpPathField.Name + "}", $"${{uriPart{CodeGenUtility.Capitalize(httpPathField.ServiceField.Name)}}}");
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

								if (httpMethodInfo.RequestBodyField != null)
								{
									using (code.Block($"if (!request.{httpMethodInfo.RequestBodyField.ServiceField.Name}) {{", "}"))
										code.WriteLine($"return Promise.resolve(createRequiredRequestFieldError('{httpMethodInfo.RequestBodyField.ServiceField.Name}'));");
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
											code.WriteLine("fetchRequest.headers" + IfTypeScript("!") + $"['{httpHeaderField.Name}'] = {RenderFieldValue(httpHeaderField.ServiceField, service, $"request.{httpHeaderField.ServiceField.Name}")};");
									}
								}

								code.WriteLine("return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)");
								using (code.Indent())
								using (code.Block(".then(result => {", "});"))
								{
									code.WriteLine("const status = result.response.status;");
									var responseValueType = $"I{capMethodName}Response";
									code.WriteLine("let value" + IfTypeScript($": {responseValueType} | null") + " = null;");
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
												{
													code.WriteLine($"value = {{ {responseBodyFieldName}: true }};");
												}
												else
												{
													using (code.Block("if (result.json) {", "}"))
													{
														code.WriteLine($"value = {{ {responseBodyFieldName}: result.json }}" + IfTypeScript($" as {responseValueType}") + ";");
													}
												}
											}
											else
											{
												if (validResponse.NormalFields!.Count == 0)
												{
													code.WriteLine("value = {};");
												}
												else
												{
													using (code.Block("if (result.json) {", "}"))
													{
														code.WriteLine("value = result.json" + IfTypeScript($" as {responseValueType} | null") + ";");
													}
												}
											}
										}
										code.WriteLine("}");
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
												code.WriteLine($"value.{httpHeaderField.ServiceField.Name} = {ParseFieldValue(httpHeaderField.ServiceField, service, "headerValue")};");
										}
									}

									code.WriteLine("return { value: value };");
								}
							}
						}

						foreach (var httpEventInfo in httpServiceInfo.Events)
						{
							var eventName = httpEventInfo.ServiceMethod.Name;
							var capEventName = CodeGenUtility.Capitalize(eventName);

							code.WriteLine();
							WriteJsDoc(code, httpEventInfo.ServiceMethod);
							using (code.Block(IfTypeScript("public ") + $"{eventName}(request" + IfTypeScript($": I{capEventName}Request") + ", context" + IfTypeScript("?: unknown") + ")" + IfTypeScript($": Promise<IServiceResult<AsyncIterable<IServiceResult<I{capEventName}Response>>>>") + " {", "}"))
							{
								var hasPathFields = httpEventInfo.PathFields.Count != 0;
								var jsUriDelim = hasPathFields ? "`" : "'";
#if !NETSTANDARD2_0
								var jsUri = string.Concat(jsUriDelim, httpEventInfo.Path.AsSpan(1), jsUriDelim);
#else
								var jsUri = jsUriDelim + httpEventInfo.Path.Substring(1) + jsUriDelim;
#endif
								if (hasPathFields)
								{
									foreach (var httpPathField in httpEventInfo.PathFields)
									{
										code.WriteLine($"const uriPart{CodeGenUtility.Capitalize(httpPathField.ServiceField.Name)} = request.{httpPathField.ServiceField.Name} != null && {RenderUriComponent(httpPathField.ServiceField, service)};");
										using (code.Block($"if (!uriPart{CodeGenUtility.Capitalize(httpPathField.ServiceField.Name)}) {{", "}"))
											code.WriteLine($"return Promise.resolve({{ error: createRequiredRequestFieldError('{httpPathField.ServiceField.Name}').error }}" + IfTypeScript($" as IServiceResult<AsyncIterable<IServiceResult<I{capEventName}Response>>>") + ");");
									}
									foreach (var httpPathField in httpEventInfo.PathFields)
										jsUri = ReplaceOrdinal(jsUri, "{" + httpPathField.Name + "}", $"${{uriPart{CodeGenUtility.Capitalize(httpPathField.ServiceField.Name)}}}");
								}

								var hasQueryFields = httpEventInfo.QueryFields.Count != 0;
								code.WriteLine((hasQueryFields ? "let" : "const") + $" uri = {jsUri};");
								if (hasQueryFields)
								{
									code.WriteLine("const query" + IfTypeScript(": string[]") + " = [];");
									foreach (var httpQueryField in httpEventInfo.QueryFields)
										code.WriteLine($"request.{httpQueryField.ServiceField.Name} == null || query.push('{httpQueryField.Name}=' + {RenderUriComponent(httpQueryField.ServiceField, service)});");
									using (code.Block("if (query.length) {", "}"))
										code.WriteLine("uri = uri + '?' + query.join('&');");
								}

								// Build fetch request for all HTTP methods
								using (code.Block("const fetchRequest" + IfTypeScript(": IFetchRequest") + " = {", "};"))
								{
									code.WriteLine($"method: '{httpEventInfo.Method}',");

									if (httpEventInfo.RequestBodyField == null && httpEventInfo.RequestNormalFields.Count == 0)
									{
										if (httpEventInfo.RequestHeaderFields.Count != 0)
											code.WriteLine("headers: {},");
									}
									else
									{
										code.WriteLine("headers: { 'Content-Type': 'application/json' },");

										if (httpEventInfo.RequestBodyField != null)
										{
											code.WriteLine($"body: JSON.stringify(request.{httpEventInfo.RequestBodyField.ServiceField.Name})");
										}
										else if (httpEventInfo.ServiceMethod.RequestFields.Count == httpEventInfo.RequestNormalFields.Count)
										{
											code.WriteLine("body: JSON.stringify(request)");
										}
										else
										{
											using (code.Block("body: JSON.stringify({", "})"))
											{
												for (var httpFieldIndex = 0; httpFieldIndex < httpEventInfo.RequestNormalFields.Count; httpFieldIndex++)
												{
													var httpFieldInfo = httpEventInfo.RequestNormalFields[httpFieldIndex];
													var isLastField = httpFieldIndex == httpEventInfo.RequestNormalFields.Count - 1;
													var fieldName = httpFieldInfo.ServiceField.Name;
													code.WriteLine(fieldName + ": request." + fieldName + (isLastField ? "" : ","));
												}
											}
										}
									}
								}

								if (httpEventInfo.RequestHeaderFields.Count != 0)
								{
									foreach (var httpHeaderField in httpEventInfo.RequestHeaderFields)
									{
										using (code.Block($"if (request.{httpHeaderField.ServiceField.Name} != null) {{", "}"))
											code.WriteLine("fetchRequest.headers" + IfTypeScript("!") + $"['{httpHeaderField.Name}'] = {RenderFieldValue(httpHeaderField.ServiceField, service, $"request.{httpHeaderField.ServiceField.Name}")};");
									}
								}

								code.WriteLine("return " + (TypeScript ? "HttpClientUtility." : "") + "createFetchEventStream" + IfTypeScript($"<I{capEventName}Response>") + "(this._fetch, this._baseUri + uri, fetchRequest, context);");
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
			}

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
						WriteImports(code, typeNames, $"./{typesFileNameNoExt}");
						code.WriteLine($"export * from './{typesFileNameNoExt}';");
					}

					code.WriteLine();
					WriteStandardErrorCodesVariable("standardErrorCodes", code, httpServiceInfo.ErrorSets);

					code.WriteLine();
					WriteParseBooleanFunction("parseBoolean", code);

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
								expressPath = ReplaceOrdinal(expressPath, "{" + httpPathField.Name + "}", $":{httpPathField.Name}");

							code.WriteLine();
							WriteJsDoc(code, httpMethodInfo.ServiceMethod);
							using (code.Block($"app.{expressMethod}('{expressPath}', function (req, res, next) {{", "});"))
							{
								code.WriteLine("const request" + IfTypeScript($": I{capMethodName}Request") + " = {};");

								foreach (var httpPathField in httpMethodInfo.PathFields)
									code.WriteLine($"request.{httpPathField.ServiceField.Name} = {ParseFieldValue(httpPathField.ServiceField, service, $"req.params.{httpPathField.Name}")};");

								foreach (var httpQueryField in httpMethodInfo.QueryFields)
								{
									using (code.Block($"if (typeof req.query['{httpQueryField.Name}'] === 'string') {{", "}"))
										code.WriteLine($"request.{httpQueryField.ServiceField.Name} = {ParseFieldValue(httpQueryField.ServiceField, service, $"req.query['{httpQueryField.Name}']")};");
								}

								if (httpMethodInfo.RequestBodyField != null)
								{
									code.WriteLine($"request.{httpMethodInfo.RequestBodyField.ServiceField.Name} = req.body;");
								}
								else
								{
									foreach (var field in httpMethodInfo.RequestNormalFields)
										code.WriteLine($"request.{field.ServiceField.Name} = req.body.{field.ServiceField.Name};");
								}

								foreach (var field in httpMethodInfo.RequestHeaderFields)
									code.WriteLine($"request.{field.ServiceField.Name} = req.header('{field.Name}');");

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
											foreach (var field in httpMethodInfo.ResponseHeaderFields)
											{
												using (code.Block($"if (result.value.{field.ServiceField.Name} != null) {{", "}"))
													code.WriteLine($"res.setHeader('{field.Name}', result.value.{field.ServiceField.Name});");
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
			Fastify = ourSettings.Fastify;
			DisableESLint = ourSettings.DisableESLint;
			FileNameSuffix = ourSettings.FileNameSuffix;
			NoHttp = ourSettings.NoHttp;
		}

		/// <summary>
		/// Supports writing output to a single file.
		/// </summary>
		public override bool SupportsSingleOutput => true;

		private CodeGenOutput GenerateFastifyPluginOutput(ServiceInfo service)
		{
			var httpServiceInfo = HttpServiceInfo.Create(service);
			var moduleName = ModuleName ?? service.Name;
			var capModuleName = CodeGenUtility.Capitalize(moduleName);
			var camelCaseModuleName = CodeGenUtility.ToCamelCase(moduleName);
			var pluginFileName = CodeGenUtility.Uncapitalize(moduleName) + "Plugin" + (TypeScript ? ".ts" : ".js");
			var customTypes = new HashSet<string>(service.Dtos.Select(x => x.Name).Concat(service.Enums.Select(x => x.Name)));

			var file = CreateFile(pluginFileName, code =>
			{
				WriteFileHeader(code);

				if (TypeScript)
				{
					code.WriteLine();
					code.WriteLine("import type * as fastifyTypes from 'fastify';");
					code.WriteLine("import type { IServiceResult, IServiceError } from 'facility-core';");

					code.WriteLine();
					using (code.Block($"export type {capModuleName}PluginOptions = fastifyTypes.RegisterOptions & {{", "}"))
					{
						WriteJsDoc(code, $"The `I{capModuleName}` service implementation. Can be a service instance or a factory function which is called on each request.");
						code.WriteLine($"serviceOrFactory: I{capModuleName} | ((req: fastifyTypes.FastifyRequest) => I{capModuleName});");

						code.WriteLine();
						WriteJsDoc(code, "Whether to make query string keys case insensitive. Defalts to false.");
						code.WriteLine("caseInsensitiveQueryStringKeys?: boolean;");

						code.WriteLine();
						WriteJsDoc(code, "Whether to include error details in the response. Defaults to false.");
						code.WriteLine("includeErrorDetails?: boolean;");
					}
				}
				else
				{
					code.WriteLine("'use strict';");
				}

				code.WriteLine();
				WriteJsDoc(code, "EXPERIMENTAL: The generated code for this plugin is subject to change/removal without a major version bump.");
				using (code.Block($"export const {camelCaseModuleName}Plugin" + IfTypeScript($": fastifyTypes.FastifyPluginAsync<{capModuleName}PluginOptions>") + " = async (fastify, opts) => {", "}"))
				{
					code.WriteLine("const { serviceOrFactory, caseInsensitiveQueryStringKeys, includeErrorDetails } = opts;");

					code.WriteLine();
					code.WriteLine("const getService = typeof serviceOrFactory === 'function' ? serviceOrFactory : () => serviceOrFactory;");

					code.WriteLine();
					using (code.Block($"function sendErrorResponse(res{IfTypeScript(": fastifyTypes.FastifyReply")}, error{IfTypeScript(": IServiceError")}) {{", "}"))
					{
						code.WriteLine("const statusCode = standardErrorCodes[error.code ?? ''] || 500;");
						using (code.Block("if (statusCode >= 500) {", "}"))
						{
							code.WriteLine("res.log.error(error);");
							using (code.Block("if (!includeErrorDetails) {", "}"))
							{
								code.WriteLine("error.message = 'The service experienced an unexpected internal error.';");
								code.WriteLine("delete error.details;");
								code.WriteLine("delete error.innerError;");
							}
						}
						code.WriteLine();
						code.WriteLine("res.code(statusCode);");
						code.WriteLine("res.type('application/json');");
						code.WriteLine("res.send(error);");
					}

					code.WriteLine();
					using (code.Block($"function sendResponse(res{IfTypeScript(": fastifyTypes.FastifyReply")}, code{IfTypeScript(": number")}, value{IfTypeScript(": object | string | true")}) {{", "}"))
					{
						code.WriteLine("res.code(code);");

						code.WriteLine();
						code.WriteLine("if (value === true) {");
						using (code.Indent())
						{
							code.WriteLine("res.send();");
						}
						code.WriteLine("} else {");
						using (code.Indent())
						{
							code.WriteLine("res.type('application/json');");
							code.WriteLine("res.send(value);");
						}
						code.WriteLine("}");
					}

					code.WriteLine();
					using (code.Block("for (const jsonSchema of jsonSchemas) {", "}"))
						code.WriteLine("fastify.addSchema(jsonSchema);");

					code.WriteLine();
					using (code.Block("fastify.setErrorHandler((err, req, res) => {", "});"))
					{
						using (code.Block("sendErrorResponse(res, {", "});"))
						{
							code.WriteLine("code: 'InternalError',");
							code.WriteLine("message: err.message,");
							code.WriteLine("details: { stack: err.stack?.split('\\n').filter((x) => x.length > 0) },");
						}
					}

					code.WriteLine();
					using (code.Block("if (caseInsensitiveQueryStringKeys) {", "}"))
					{
						using (code.Block("fastify.addHook('onRequest', async (req, res) => {", "});"))
						{
							code.WriteLine($"const query = req.query{IfTypeScript(" as Record<string, string>")};");
							using (code.Block("for (const key of Object.keys(query)) {", "}"))
							{
								code.WriteLine("const lowerKey = key.toLowerCase();");
								using (code.Block("if (lowerKey !== key) {", "}"))
								{
									code.WriteLine($"query[lowerKey] = query[key]{IfTypeScript(" as string")};");
									code.WriteLine("delete query[key];");
								}
							}
						}
					}

					foreach (var httpMethodInfo in httpServiceInfo.Methods)
					{
						var methodName = httpMethodInfo.ServiceMethod.Name;
						var capMethodName = CodeGenUtility.Capitalize(methodName);
						var fastifyPath = httpMethodInfo.Path;
						foreach (var httpPathField in httpMethodInfo.PathFields)
							fastifyPath = ReplaceOrdinal(fastifyPath, "{" + httpPathField.Name + "}", $":{httpPathField.Name}");

						code.WriteLine();
						using (code.Block("fastify.route({", "});"))
						{
							code.WriteLine($"url: '{fastifyPath}',");
							code.WriteLine($"method: '{httpMethodInfo.Method.ToUpperInvariant()}',");
							using (code.Block("schema: {", "},"))
							{
								using (code.Block("response: {", "},"))
								{
									foreach (var response in httpMethodInfo.ValidResponses)
									{
										var statusCode = (int) response.StatusCode;
										code.Write($"'{statusCode}': ");

										if (response.BodyField is not null)
										{
											code.WriteLine($"{GetJsonSchemaType(service.GetFieldType(response.BodyField.ServiceField)!)},");
										}
										else if (response.NormalFields?.Count > 0)
										{
											using (code.Block("{", "},"))
											{
												code.WriteLine("type: 'object',");
												using (code.Block("properties: {", "},"))
												{
													foreach (var normalField in response.NormalFields)
														code.WriteLine($"{normalField.ServiceField.Name}: {GetJsonSchemaType(service.GetFieldType(normalField.ServiceField)!)},");
												}
											}
										}
										else
										{
											code.WriteLine("{ type: 'object', additionalProperties: false },");
										}
									}

									code.WriteLine("'4xx': { $ref: '_error' },");
									code.WriteLine("'5xx': { $ref: '_error' },");
								}
							}
							using (code.Block("handler: async function (req, res) {", "}"))
							{
								code.WriteLine("const request" + IfTypeScript($": Partial<I{capMethodName}Request>") + " = {};");
								if (httpMethodInfo.PathFields.Count != 0)
								{
									code.WriteLine();
									code.WriteLine($"const params = req.params{IfTypeScript(" as Record<string, string>")};");
									foreach (var pathParam in httpMethodInfo.PathFields)
									{
										code.WriteLine($"if (typeof params['{pathParam.Name}'] === 'string') request.{pathParam.ServiceField.Name} = {ParseFieldValue(pathParam.ServiceField, service, $"params['{pathParam.Name}']")};");
									}
								}

								if (httpMethodInfo.QueryFields.Count != 0)
								{
									code.WriteLine();
									code.WriteLine($"const query = req.query{IfTypeScript(" as Record<string, string>")};");
									foreach (var queryParam in httpMethodInfo.QueryFields)
									{
										code.WriteLine($"if (typeof query['{queryParam.Name}'] === 'string') request.{queryParam.ServiceField.Name} = {ParseFieldValue(queryParam.ServiceField, service, $"query['{queryParam.Name}']")};");
									}
								}

								if (httpMethodInfo.RequestHeaderFields.Count != 0)
								{
									code.WriteLine();
									code.WriteLine($"const headers = req.headers{IfTypeScript(" as Record<string, string>")};");
									foreach (var header in httpMethodInfo.RequestHeaderFields)
									{
										string lowerHeaderName = header.Name.ToLowerInvariant();
										code.WriteLine($"if (typeof headers['{lowerHeaderName}'] === 'string') request.{header.ServiceField.Name} = {ParseFieldValue(header.ServiceField, service, $"headers['{lowerHeaderName}']")};");
									}
								}

								if (httpMethodInfo.RequestBodyField != null)
								{
									code.WriteLine();
									code.WriteLine($"request.{httpMethodInfo.RequestBodyField.ServiceField.Name} = req.body{IfTypeScript(" as never")};");
								}
								else if (httpMethodInfo.RequestNormalFields.Count != 0)
								{
									code.WriteLine();
									code.WriteLine($"const body = req.body{IfTypeScript(" as Record<string, never>")};");
									foreach (var field in httpMethodInfo.RequestNormalFields)
										code.WriteLine($"request.{field.ServiceField.Name} = body.{field.ServiceField.Name};");
								}

								code.WriteLine();
								code.WriteLine($"const result = await getService(req).{methodName}(request{IfTypeScript($" as I{capMethodName}Request")});");

								code.WriteLine();
								code.WriteLine("if (result.value != null && result.error == null) {");
								using (code.Indent())
								{
									if (httpMethodInfo.ResponseHeaderFields.Count != 0)
									{
										code.WriteLineSkipOnce();
										foreach (var field in httpMethodInfo.ResponseHeaderFields)
											code.WriteLine($"if (result.value.{field.ServiceField.Name} != null) res.header('{field.Name}', result.value.{field.ServiceField.Name});");
									}

									var responsesWithoutBodyField = httpMethodInfo.ValidResponses.Where(x => x.BodyField is null).ToList();
									var responsesWithBodyField = httpMethodInfo.ValidResponses.Where(x => x.BodyField is not null).ToList();

									if (responsesWithBodyField.Count > 0)
									{
										code.WriteLineSkipOnce();
										bool elseIf = false;
										foreach (var response in responsesWithBodyField)
										{
											var name = response.BodyField!.ServiceField.Name;
											var statusCode = (int) response.StatusCode;

											code.WriteLine($"{(elseIf ? "} else " : "")}if (result.value.{name}) {{");
											using (code.Indent())
												code.WriteLine($"sendResponse(res, {statusCode}, result.value.{name});");

											elseIf = true;
										}
										code.WriteLine("} else {");
										using (code.Indent())
										{
											if (responsesWithoutBodyField.Count == 1)
												code.WriteLine($"sendResponse(res, {(int) responsesWithoutBodyField[0].StatusCode}, result.value);");
											else
												code.WriteLine($"throw new Error('Value must have exactly one set from: {string.Join(", ", responsesWithBodyField.Select(x => x.BodyField!.ServiceField.Name))}');");
										}
										code.WriteLine("}");
									}
									else if (responsesWithoutBodyField.Count == 1)
									{
										code.WriteLineSkipOnce();
										code.WriteLine($"sendResponse(res, {(int) responsesWithoutBodyField[0].StatusCode}, result.value);");
									}

									if (responsesWithoutBodyField.Count > 1)
										throw new InvalidOperationException("Multiple responses without a body field are not supported.");
								}
								code.WriteLine("} else if (result.error != null) {");
								using (code.Indent())
								{
									code.WriteLine("sendErrorResponse(res, result.error);");
								}
								code.WriteLine("} else {");
								using (code.Indent())
								{
									code.WriteLine("throw new Error('Result must have exactly one set from: value, error');");
								}
								code.WriteLine("}");
							}
						}
					}
				}

				WriteJsonSchemaDtos(code, service);

				code.WriteLine();
				WriteStandardErrorCodesVariable("standardErrorCodes", code, httpServiceInfo.ErrorSets);

				code.WriteLine();
				WriteParseBooleanFunction("parseBoolean", code);

				if (TypeScript)
					WriteTypes(code, httpServiceInfo);
			});

			return new CodeGenOutput(file);

			string GetJsonSchemaType(ServiceTypeInfo serviceType) => serviceType.Kind switch
			{
				ServiceTypeKind.String or ServiceTypeKind.Bytes or ServiceTypeKind.DateTime or ServiceTypeKind.ExternalEnum => "{ type: 'string' }",
				ServiceTypeKind.Boolean => "{ type: 'boolean' }",
				ServiceTypeKind.Float or ServiceTypeKind.Double or ServiceTypeKind.Decimal => "{ type: 'number' }",
				ServiceTypeKind.Int32 or ServiceTypeKind.Int64 => "{ type: 'integer' }",
				ServiceTypeKind.Object or ServiceTypeKind.ExternalDto => "{ type: 'object', additionalProperties: true }",
				ServiceTypeKind.Error => "{ $ref: '_error' }",
				ServiceTypeKind.Dto => $"{{ $ref: '{serviceType.Dto!.Name}' }}",
				ServiceTypeKind.Enum => $"{{ $ref: '{serviceType.Enum!.Name}' }}",
				ServiceTypeKind.Result => $"{{ type: 'object', properties: {{ value: {GetJsonSchemaType(serviceType.ValueType!)}, error: {{ $ref: '_error' }} }} }}",
				ServiceTypeKind.Array => $"{{ type: 'array', items: {GetJsonSchemaType(serviceType.ValueType!)} }}",
				ServiceTypeKind.Map => $"{{ type: 'object', additionalProperties: {GetJsonSchemaType(serviceType.ValueType!)} }}",
				ServiceTypeKind.Nullable => $"{{ oneOf: [ {GetJsonSchemaType(serviceType.ValueType!)}, {{ type: 'null' }} ] }}",
				_ => throw new NotSupportedException($"Unknown field type '{serviceType.Kind}'"),
			};

			void WriteJsonSchemaDtos(CodeWriter code, ServiceInfo service)
			{
				code.WriteLine();
				using (code.Block("const jsonSchemas = [", $"]{IfTypeScript(" as const")};"))
				{
					using (code.Block("{", $"}}{IfTypeScript(" as const")},"))
					{
						code.WriteLine("$id: '_error',");
						code.WriteLine("type: 'object',");
						using (code.Block("properties: {", "}"))
						{
							code.WriteLine("code: { type: 'string' },");
							code.WriteLine("message: { type: 'string' },");
							code.WriteLine("details: { type: 'object', additionalProperties: true },");
							code.WriteLine("innerError: { $ref: '_error' },");
						}
					}

					foreach (var dto in service.Dtos)
					{
						using (code.Block("{", $"}}{IfTypeScript(" as const")},"))
						{
							code.WriteLine($"$id: '{dto.Name}',");
							code.WriteLine("type: 'object',");
							using (code.Block("properties: {", "}"))
							{
								foreach (var field in dto.Fields)
									code.WriteLine($"{field.Name}: {GetJsonSchemaType(service.GetFieldType(field)!)},");
							}
						}
					}

					foreach (var enumInfo in service.Enums)
					{
						using (code.Block("{", $"}}{IfTypeScript(" as const")},"))
						{
							code.WriteLine($"$id: '{enumInfo.Name}',");
							code.WriteLine("type: 'string',");
							code.Write("enum: [ ");

							var shouldWriteComma = false;
							foreach (var enumValue in enumInfo.Values)
							{
								if (shouldWriteComma)
									code.Write(", ");
								else
									shouldWriteComma = true;

								code.Write($"'{enumValue.Name}'");
							}

							code.WriteLine(" ],");
						}
					}
				}
			}
		}

		private void WriteFileHeader(CodeWriter code)
		{
			code.WriteLine($"// DO NOT EDIT: generated by {GeneratorName}");
			if (DisableESLint)
				code.WriteLine("/* eslint-disable */");
		}

		private static void WriteDto(CodeWriter code, ServiceDtoInfo dtoInfo, ServiceInfo service)
		{
			code.WriteLine();
			WriteJsDoc(code, dtoInfo);
			using (code.Block($"export interface I{CodeGenUtility.Capitalize(dtoInfo.Name)} {{", "}"))
			{
				foreach (var fieldInfo in dtoInfo.Fields)
				{
					code.WriteLineSkipOnce();
					WriteJsDoc(code, fieldInfo);
					code.WriteLine($"{fieldInfo.Name}{(fieldInfo.IsRequired ? "" : "?")}: {RenderFieldType(service.GetFieldType(fieldInfo)!)};");
				}
			}
		}

		private string IfTypeScript(string value) => TypeScript ? value : "";

		private static string RenderFieldType(ServiceTypeInfo fieldType)
		{
			switch (fieldType.Kind)
			{
				case ServiceTypeKind.String:
				case ServiceTypeKind.Bytes:
				case ServiceTypeKind.DateTime:
					return "string";
				case ServiceTypeKind.Enum:
					return fieldType.Enum!.Name;
				case ServiceTypeKind.ExternalEnum:
					return fieldType.ExternalEnum!.Name;
				case ServiceTypeKind.Boolean:
					return "boolean";
				case ServiceTypeKind.Float:
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
				case ServiceTypeKind.ExternalDto:
					return $"I{CodeGenUtility.Capitalize(fieldType.ExternalDto!.Name)}";
				case ServiceTypeKind.Result:
					return $"IServiceResult<{RenderFieldType(fieldType.ValueType!)}>";
				case ServiceTypeKind.Array:
					return $"{RenderFieldType(fieldType.ValueType!)}[]";
				case ServiceTypeKind.Map:
					return $"{{ [name: string]: {RenderFieldType(fieldType.ValueType!)} }}";
				case ServiceTypeKind.Nullable:
					return $"({RenderFieldType(fieldType.ValueType!)} | null)";
				default:
					throw new NotSupportedException("Unknown field type " + fieldType.Kind);
			}
		}

		private static string RenderUriComponent(ServiceFieldInfo field, ServiceInfo service)
		{
			var fieldTypeKind = service.GetFieldType(field)!.Kind;
			var fieldName = field.Name;

			switch (fieldTypeKind)
			{
				case ServiceTypeKind.Enum:
				case ServiceTypeKind.ExternalEnum:
					return $"request.{fieldName}";
				case ServiceTypeKind.String:
				case ServiceTypeKind.Bytes:
				case ServiceTypeKind.DateTime:
					return $"encodeURIComponent(request.{fieldName})";
				case ServiceTypeKind.Boolean:
				case ServiceTypeKind.Int32:
				case ServiceTypeKind.Int64:
				case ServiceTypeKind.Decimal:
					return $"request.{fieldName}.toString()";
				case ServiceTypeKind.Float:
				case ServiceTypeKind.Double:
					return $"encodeURIComponent(request.{fieldName}.toString())";
				case ServiceTypeKind.Dto:
				case ServiceTypeKind.ExternalDto:
				case ServiceTypeKind.Error:
				case ServiceTypeKind.Object:
					throw new NotSupportedException("Field type not supported on path/query: " + fieldTypeKind);
				default:
					throw new NotSupportedException("Unknown field type " + fieldTypeKind);
			}
		}

		private string ParseFieldValue(ServiceFieldInfo field, ServiceInfo service, string value)
		{
			var fieldTypeKind = service.GetFieldType(field)!.Kind;

			switch (fieldTypeKind)
			{
				case ServiceTypeKind.Enum:
				case ServiceTypeKind.ExternalEnum:
					return $"{value}{IfTypeScript($" as {field.TypeName}")}";
				case ServiceTypeKind.String:
				case ServiceTypeKind.Bytes:
				case ServiceTypeKind.DateTime:
					return value;
				case ServiceTypeKind.Boolean:
					return $"parseBoolean({value})";
				case ServiceTypeKind.Int32:
				case ServiceTypeKind.Int64:
					return $"parseInt({value}, 10)";
				case ServiceTypeKind.Decimal:
				case ServiceTypeKind.Float:
				case ServiceTypeKind.Double:
					return $"parseFloat({value})";
				case ServiceTypeKind.Dto:
				case ServiceTypeKind.ExternalDto:
				case ServiceTypeKind.Error:
				case ServiceTypeKind.Object:
					throw new NotSupportedException("Field type not supported on path/query/header: " + fieldTypeKind);
				default:
					throw new NotSupportedException("Unknown field type " + fieldTypeKind);
			}
		}

		private static string RenderFieldValue(ServiceFieldInfo field, ServiceInfo service, string value)
		{
			var fieldTypeKind = service.GetFieldType(field)!.Kind;

			switch (fieldTypeKind)
			{
				case ServiceTypeKind.Enum:
				case ServiceTypeKind.ExternalEnum:
				case ServiceTypeKind.String:
				case ServiceTypeKind.Bytes:
				case ServiceTypeKind.DateTime:
					return value;
				case ServiceTypeKind.Boolean:
				case ServiceTypeKind.Int32:
				case ServiceTypeKind.Int64:
				case ServiceTypeKind.Decimal:
				case ServiceTypeKind.Float:
				case ServiceTypeKind.Double:
					return $"{value}.toString()";
				case ServiceTypeKind.Dto:
				case ServiceTypeKind.ExternalDto:
				case ServiceTypeKind.Error:
				case ServiceTypeKind.Object:
					throw new NotSupportedException("Field type not supported on path/query: " + fieldTypeKind);
				default:
					throw new NotSupportedException("Unknown field type " + fieldTypeKind);
			}
		}

		private static void WriteJsDoc(CodeWriter code, ServiceElementWithAttributesInfo element) =>
			WriteJsDoc(code, (element as IServiceHasSummary)?.Summary, isObsolete: element.IsObsolete, obsoleteMessage: element.ObsoleteMessage);

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

		private void WriteStandardErrorCodesVariable(string name, CodeWriter code, IEnumerable<HttpErrorSetInfo>? errorSets)
		{
			// TODO: export this from facility-core
			using (code.Block($"const {name}" + IfTypeScript(": { [code: string]: number }") + " = {", "};"))
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

				if (errorSets is not null)
				{
					foreach (var errorSetInfo in errorSets)
					{
						foreach (var error in errorSetInfo.Errors)
						{
							code.WriteLine($"'{error.ServiceError.Name}': {(int) error.StatusCode},");
						}
					}
				}
			}
		}

		private void WriteParseBooleanFunction(string name, CodeWriter code)
		{
			// TODO: export this from facility-core
			using (code.Block($"function {name}(value" + IfTypeScript(": string | undefined") + ") {", "}"))
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
		}

		private List<string> WriteTypes(CodeWriter code, HttpServiceInfo httpServiceInfo)
		{
			var typeNames = new List<string>();
			var service = httpServiceInfo.Service;
			code.WriteLine();
			WriteJsDoc(code, service);

			var capModuleName = CodeGenUtility.Capitalize(ModuleName ?? service.Name);
			typeNames.Add($"I{capModuleName}");
			using (code.Block($"export interface I{capModuleName} {{", "}"))
			{
				foreach (var httpMethodInfo in httpServiceInfo.Methods)
				{
					var methodName = httpMethodInfo.ServiceMethod.Name;
					var capMethodName = CodeGenUtility.Capitalize(methodName);
					code.WriteLineSkipOnce();
					WriteJsDoc(code, httpMethodInfo.ServiceMethod);
					code.WriteLine($"{methodName}(request: I{capMethodName}Request, context?: unknown): Promise<IServiceResult<I{capMethodName}Response>>;");
				}

				foreach (var httpEventInfo in httpServiceInfo.Events)
				{
					var eventName = httpEventInfo.ServiceMethod.Name;
					var capEventName = CodeGenUtility.Capitalize(eventName);
					code.WriteLineSkipOnce();
					WriteJsDoc(code, httpEventInfo.ServiceMethod);
					code.WriteLine($"{eventName}(request: I{capEventName}Request, context?: unknown): Promise<IServiceResult<AsyncIterable<IServiceResult<I{capEventName}Response>>>>;");
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

			foreach (var eventInfo in service.Events)
			{
				var requestDtoName = $"{CodeGenUtility.Capitalize(eventInfo.Name)}Request";
				typeNames.Add($"I{requestDtoName}");
				WriteDto(code, new ServiceDtoInfo(
					name: requestDtoName,
					fields: eventInfo.RequestFields,
					summary: $"Request for {CodeGenUtility.Capitalize(eventInfo.Name)}."), service);

				var responseDtoName = $"{CodeGenUtility.Capitalize(eventInfo.Name)}Response";
				typeNames.Add($"I{responseDtoName}");
				WriteDto(code, new ServiceDtoInfo(
					name: responseDtoName,
					fields: eventInfo.ResponseFields,
					summary: $"Response for {CodeGenUtility.Capitalize(eventInfo.Name)}."), service);
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
						code.WriteLine($"{value.Name} = '{value.Name}',");
					}
				}
			}

			foreach (var errorSetInfo in service.ErrorSets)
			{
				typeNames.Add(errorSetInfo.Name);
				code.WriteLine();
				WriteJsDoc(code, errorSetInfo);
				using (code.Block($"export enum {errorSetInfo.Name} {{", "}"))
				{
					foreach (var error in errorSetInfo.Errors)
					{
						code.WriteLineSkipOnce();
						WriteJsDoc(code, error);
						code.WriteLine($"{error.Name} = '{error.Name}',");
					}
				}
			}

			return typeNames;
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

#if !NETSTANDARD2_0
		private static string ReplaceOrdinal(string value, string oldValue, string newValue) => value.Replace(oldValue, newValue, StringComparison.Ordinal);
#else
		private static string ReplaceOrdinal(string value, string oldValue, string newValue) => value.Replace(oldValue, newValue);
#endif

		private const string c_jsAttributeName = "js";
		private const string c_jsAttributeNameParameterName = "name";
		private const string c_jsAttributeModuleParameterName = "module";
	}
}
