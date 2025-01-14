import { FastifyPluginAsync, FastifyServerOptions } from "fastify";
import { ConformanceApiService } from "../conformanceApiService.js";
import conformanceTestsJson from "../../ConformanceTests.json";
import { conformanceApiPlugin, ConformanceApiPluginOptions } from "./conformanceApiPlugin.js";
import { jsConformanceApiPlugin } from "./jsConformanceApiPlugin.js";

const app: FastifyPluginAsync<FastifyServerOptions> = async (fastify): Promise<void> => {
	const conformanceApiPluginOptions: ConformanceApiPluginOptions = {
		serviceOrFactory: () =>
			new ConformanceApiService(conformanceTestsJson.tests),
		caseInsenstiveQueryStringKeys: true,
		includeErrorDetails: true,
	};

	fastify.register(conformanceApiPlugin, conformanceApiPluginOptions);
	fastify.register(jsConformanceApiPlugin, {...conformanceApiPluginOptions, prefix: "/js" });
};

export default app;
export const options: FastifyServerOptions = {
	caseSensitive: false,
};
