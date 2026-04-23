import { FastifyPluginAsync, FastifyServerOptions } from "fastify";
import { ConformanceApiService } from "../conformanceApiService.js";
import conformanceTestsJson from "../../ConformanceTests.json";
import { conformanceApiPlugin, ConformanceApiPluginOptions } from "./conformanceApiPlugin.js";
import { jsConformanceApiPlugin } from "./jsConformanceApiPlugin.js";

const app: FastifyPluginAsync<FastifyServerOptions> = async (fastify): Promise<void> => {
	const conformanceApiPluginOptions: ConformanceApiPluginOptions = {
		serviceOrFactory: () =>
			new ConformanceApiService(conformanceTestsJson.tests),
		caseInsensitiveQueryStringKeys: true,
		includeErrorDetails: true,
		routeOptions: {
			'*': {
				bodyLimit: 10 * 1024, // 10 KB
			},
			'createWidget': {
				bodyLimit: 100 * 1024, // 100 KB
			}
		}
	};

	fastify.register(conformanceApiPlugin, conformanceApiPluginOptions);
	fastify.register(jsConformanceApiPlugin, {...conformanceApiPluginOptions, prefix: "/js" });
};

export default app;
export const options: FastifyServerOptions = {
	caseSensitive: false,
};
