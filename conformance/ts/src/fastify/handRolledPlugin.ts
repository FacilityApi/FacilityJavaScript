import { FastifyPluginAsync } from "fastify";
import type { IConformanceApi, IGetApiInfoRequest, IGetWidgetsRequest, IGetWidgetRequest, IGetWidgetsResponse, ICreateWidgetRequest, IWidget, IDeleteWidgetRequest, IGetWidgetBatchRequest, IMirrorFieldsRequest, ICheckQueryRequest, Answer, ICheckPathRequest, IMirrorHeadersRequest, IMixedRequest, IRequiredRequest } from "../conformanceApi";

const standardErrorCodes: { [code: string]: number } = {
	NotModified: 304,
	InvalidRequest: 400,
	NotAuthenticated: 401,
	NotAuthorized: 403,
	NotFound: 404,
	Conflict: 409,
	RequestTooLarge: 413,
	TooManyRequests: 429,
	InternalError: 500,
	ServiceUnavailable: 503,
	NotAdmin: 403,
};

function parseBoolean(value: string | undefined) {
	if (typeof value === 'string') {
		const lowerValue = value.toLowerCase();
		if (lowerValue === 'true') {
			return true;
		}
		if (lowerValue === 'false') {
			return false;
		}
	}
	return undefined;
}

export type ConformanceApiPluginOptions = {
	api: IConformanceApi;
}

export const handRolledPlugin: FastifyPluginAsync<ConformanceApiPluginOptions> = async (fastify, opts) => {
	const api = opts.api;
	fastify.route({
		url: "/",
		method: "GET",
		handler: async function (req, reply) {
			const request: IGetApiInfoRequest = {};

			const result = await api.getApiInfo(request);

			if (result.error) {
				const status = result.error.code && standardErrorCodes[result.error.code];
				reply.status(status || 500).send(result.error);
				return;
			}

			if (result.value) {
				reply.send(result.value);
			}
		},
	});

	fastify.route({
		url: "/widgets",
		method: "GET",
		handler: async (req, reply) => {
			const request: IGetWidgetsRequest = {};

			const query = req.query as Record<string, string>;
			if (typeof query["q"] === "string") {
				request.query = query["q"];
			}

			const result = await api.getWidgets(request);

			if (result.error) {
				const status = result.error.code && standardErrorCodes[result.error.code];
				reply.status(status || 500).send(result.error);
				return;
			}

			if (result.value) {
				reply.status(200).send({
					widgets: result.value.widgets,
				} satisfies IGetWidgetsResponse);
			}

			throw new Error("Result must have an error or value.");
		},
	});

	fastify.route({
		url: "/widgets",
		method: "POST",
		handler: async (req, reply) => {
			const request: ICreateWidgetRequest = {};
			request.widget = req.body as IWidget;

			const result = await api.createWidget(request);
			if (result.error) {
				const status = result.error.code && standardErrorCodes[result.error.code];
				reply.status(status || 500).send(result.error);
				return;
			}

			if (result.value) {
				if (result.value.url != null) {
					reply.header("Location", result.value.url);
				}

				if (result.value.eTag != null) {
					reply.header("eTag", result.value.eTag);
				}

				if (result.value.widget) {
					reply.status(201).send(result.value.widget);
				}

				return;
			}

			throw new Error("Result must have an error or value.");
		},
	});

	fastify.route({
		url: "/widgets/:id",
		method: "GET",
		handler: async (req, reply) => {
			const request: IGetWidgetRequest = {};

			const params = req.params as Record<string, string>;
			if (typeof params.id === "string") {
				request.id = parseInt(params.id);
			}
			request.ifNotETag = req.headers["if-none-match"] as string;

			const result = await api.getWidget(request);

			if (result.error) {
				const status = result.error.code && standardErrorCodes[result.error.code];
				reply.status(status || 500).send(result.error);
				return;
			}

			if (result.value) {
				if (result.value.eTag != null) {
					reply.header("eTag", result.value.eTag);
				}

				if (result.value.widget) {
					reply.status(200).send(result.value.widget);
					return;
				}

				if (result.value.notModified) {
					reply.status(304);
					return;
				}
			}

			throw new Error("Result must have an error or value.");
		},
	});

	// create the delete route here
	fastify.route({
		url: "/widgets/:id",
		method: "DELETE",
		handler: async (req, reply) => {
			const request: IDeleteWidgetRequest = {};

			const params = req.params as Record<string, string>;
			if (typeof params.id === "string") {
				request.id = parseInt(params.id);
			}
			request.ifETag = req.headers['if-match'] as string;

			const result = await api.deleteWidget(request);

			if (result.error) {
				const status = result.error.code && standardErrorCodes[result.error.code];
				reply.status(status || 500).send(result.error);
				return;
			}

			if (result.value) {
				if (result.value.notFound) {
					reply.status(404);
					return;
				}

				if (result.value.conflict) {
					reply.status(409);
					return;
				}

				reply.status(204).send({});
				return;
			}

			throw new Error("Result must have an error or value.");
		},
	});

	fastify.route({
		url: "/widgets/get",
		method: "POST",
		handler: async (req, reply) => {
			const request: IGetWidgetBatchRequest = {};
			request.ids = req.body as number[];

			const result = await api.getWidgetBatch(request);
			if (result.error) {
				const status = result.error.code && standardErrorCodes[result.error.code];
				reply.status(status || 500).send(result.error);
				return;
			}

			if (result.value) {
				if (result.value.results) {
					reply.status(200).send(result.value.results);
					return;
				}
			}

			throw new Error("Result must have an error or value.");
		},
	});

	fastify.route({
		url: "/mirrorFields",
		method: "POST",
		handler: async (req, reply) => {
			const request: IMirrorFieldsRequest = {};
			request.field = (req.body as IMirrorFieldsRequest).field;
			request.matrix = (req.body as IMirrorFieldsRequest).matrix;

			const result = await api.mirrorFields(request);

			if (result.error) {
				const status = result.error.code && standardErrorCodes[result.error.code];
				reply.status(status || 500).send(result.error);
				return;
			}

			if (result.value) {
				reply.status(200).send(result.value);
				return;
			}

			throw new Error("Result must have an error or value.");
		},
	});

	fastify.route({
		url: "/checkQuery",
		method: "GET",
		handler: async (req, reply) => {
			const request: ICheckQueryRequest = {};
			const query = req.query as Record<string, string>;

			if (typeof query["string"] === "string") {
				request.string = query["string"];
			}
			if (typeof query["boolean"] === "string") {
				request.boolean = parseBoolean(query["boolean"]);
			}
			if (typeof query["double"] === "string") {
				request.double = parseFloat(query["double"]);
			}
			if (typeof query["int32"] === "string") {
				request.int32 = parseInt(query["int32"]);
			}
			if (typeof query["int64"] === "string") {
				request.int64 = parseInt(query["int64"]);
			}
			if (typeof query["decimal"] === "string") {
				request.decimal = parseFloat(query["decimal"]);
			}
			if (typeof query["enum"] === "string") {
				request.enum = query["enum"] as Answer;
			}
			if (typeof query["datetime"] === "string") {
				request.datetime = query["datetime"];
			}

			const result = await api.checkQuery(request);

			if (result.error) {
				const status = result.error.code && standardErrorCodes[result.error.code];
				reply.status(status || 500).send(result.error);
				return;
			}

			if (result.value) {
				reply.status(200).send(result.value);
			}

			throw new Error("Result must have an error or value.");
		},
	});

	fastify.route({
		url: "/checkPath/:string/:boolean/:double/:int32/:int64/:decimal/:enum/:datetime",
		method: "GET",
		handler: async (req, reply) => {
			const request: ICheckPathRequest = {};
			const params = req.params as Record<string, string>;

			if (typeof params["string"] === "string") {
				request.string = params["string"];
			}
			if (typeof params["boolean"] === "string") {
				request.boolean = parseBoolean(params["boolean"]);
			}
			if (typeof params["double"] === "string") {
				request.double = parseFloat(params["double"]);
			}
			if (typeof params["int32"] === "string") {
				request.int32 = parseInt(params["int32"]);
			}
			if (typeof params["int64"] === "string") {
				request.int64 = parseInt(params["int64"]);
			}
			if (typeof params["decimal"] === "string") {
				request.decimal = parseFloat(params["decimal"]);
			}
			if (typeof params["enum"] === "string") {
				request.enum = params["enum"] as Answer;
			}
			if (typeof params["datetime"] === "string") {
				request.datetime = params["datetime"];
			}

			const result = await api.checkPath(request);

			if (result.error) {
				const status = result.error.code && standardErrorCodes[result.error.code];
				reply.status(status || 500).send(result.error);
				return;
			}

			if (result.value) {
				reply.status(200).send(result.value);
			}

			throw new Error("Result must have an error or value.");
		}
	});

	fastify.route({
		url: "/mirrorHeaders",
		method: "GET",
		handler: async (req, reply) => {
			const request: IMirrorHeadersRequest = {};
			const headers = req.headers as Record<string, string>;

			if (typeof headers["string"] === "string") {
				request.string = headers["string"];
			}
			if (typeof headers["boolean"] === "string") {
				request.boolean = parseBoolean(headers["boolean"]);
			}
			if (typeof headers["double"] === "string") {
				request.double = parseFloat(headers["double"]);
			}
			if (typeof headers["int32"] === "string") {
				request.int32 = parseInt(headers["int32"]);
			}
			if (typeof headers["int64"] === "string") {
				request.int64 = parseInt(headers["int64"]);
			}
			if (typeof headers["decimal"] === "string") {
				request.decimal = parseFloat(headers["decimal"]);
			}
			if (typeof headers["enum"] === "string") {
				request.enum = headers["enum"] as Answer;
			}
			if (typeof headers["datetime"] === "string") {
				request.datetime = headers["datetime"];
			}

			const result = await api.mirrorHeaders(request);

			if (result.error) {
				const status = result.error.code && standardErrorCodes[result.error.code];
				reply.status(status || 500).send(result.error);
				return;
			}
			if (result.value) {
				if (result.value.string != null) {
					reply.header("string", result.value.string);
				}
				if (result.value.boolean != null) {
					reply.header("boolean", result.value.boolean.toString());
				}
				if (result.value.double != null) {
					reply.header("double", result.value.double.toString());
				}
				if (result.value.int32 != null) {
					reply.header("int32", result.value.int32.toString());
				}
				if (result.value.int64 != null) {
					reply.header("int64", result.value.int64.toString());
				}
				if (result.value.decimal != null) {
					reply.header("decimal", result.value.decimal.toString());
				}
				if (result.value.enum != null) {
					reply.header("enum", result.value.enum);
				}
				if (result.value.datetime != null) {
					reply.header("datetime", result.value.datetime);
				}

				reply.status(200);
				return;
			}

			throw new Error("Result must have an error or value.");
		}
	});

	// implement the mixed route here.
	// pull path from params, query from query, header from headers, and normal from body
	fastify.route({
		url: "/mixed/:path",
		method: "POST",
		handler: async (req, reply) => {
			const request: IMixedRequest = {};
			const params = req.params as Record<string, string>;
			const query = req.query as Record<string, string>;
			const headers = req.headers as Record<string, string>;
			const body = req.body as Record<string, string>;

			if (typeof params["path"] === "string") {
				request.path = params["path"];
			}
			if (typeof query["query"] === "string") {
				request.query = query["query"];
			}
			if (typeof headers["header"] === "string") {
				request.header = headers["header"];
			}
			if (typeof body["normal"] === "string") {
				request.normal = body["normal"];
			}

			const result = await api.mixed(request);

			if (result.error) {
				const status = result.error.code && standardErrorCodes[result.error.code];
				reply.status(status || 500).send(result.error);
			}

			if (result.value) {
				if (result.value.header != null) {
					reply.header("header", result.value.header);
				}

				if (result.value.body != null) {
					reply.status(202).send(result.value.body);
					return;
				}

				if (result.value.empty) {
					reply.status(204);
					return;
				}


				reply.status(200).send(result.value);
				return;
			}

			throw new Error("Result must have an error or value.");
		}
	});

	fastify.route({
		url: "/required",
		method: "POST",
		handler: async (req, reply) => {
			const request: IRequiredRequest = {};
			const body = req.body as Record<string, string>;
			const query = req.query as Record<string, string>;

			if (typeof query["query"] === "string") {
				request.query = query["query"];
			}
			if (typeof body["normal"] === "string") {
				request.normal = body["normal"];
			}
			if (typeof body["widget"]) {
				request.widget = body["widget"] as never;
			}
			if (typeof body["widgets"]) {
				request.widgets = body["widgets"] as never;
			}
			if (typeof body["widgetMatrix"]) {
				request.widgetMatrix = body["widgetMatrix"] as never;
			}
			if (typeof body["widgetResult"]) {
				request.widgetResult = body["widgetResult"] as never;
			}
			if (typeof body["widgetResults"]) {
				request.widgetResults = body["widgetResults"] as never;
			}
			if (typeof body["widgetMap"]) {
				request.widgetMap = body["widgetMap"] as never;
			}
			if (typeof body["hasWidget"]) {
				request.hasWidget = body["hasWidget"] as never;
			}
			if (typeof body["point"]) {
				request.point = body["point"] as never;
			}

			const result = await api.required(request);

			if (result.error) {
				const status = result.error.code && standardErrorCodes[result.error.code];
				reply.status(status || 500).send(result.error);
			}

			if (result.value) {
				reply.status(200).send(result.value);
				return;
			}

			throw new Error("Result must have an error or value.");
		},
	});

	fastify.route({
		url: "/mirrorBytes",
		method: "POST",
		handler: async (req, reply) => {
			reply.status(500).send({
				code: "NotImplemented",
				message: "Javascript HttpClientUtility doesn't support non-json responses currently.",
			});
		}
	});

	fastify.route({
		url: "/mirrorText",
		method: "POST",
		handler: async (req, reply) => {
			reply.status(500).send({
				code: "NotImplemented",
				message: "Javascript HttpClientUtility doesn't support non-json responses currently.",
			});
		}
	});

	fastify.route({
		url: "/bodyTypes",
		method: "POST",
		handler: async (req, reply) => {
			reply.status(500).send({
				code: "NotImplemented",
				message: "Need to support FSD attributes like `[http(from: body, type: \"text/x-input\")]`",
			});
		}
	});
}
