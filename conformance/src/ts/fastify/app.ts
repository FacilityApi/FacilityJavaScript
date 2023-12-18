import { FastifyPluginAsync, FastifyServerOptions } from "fastify";
import { ConformanceApiService } from "../conformanceApiService.js";
import conformanceTestsJson from "../../../ConformanceTests.json";
import { conformanceApiPlugin } from "./conformanceApiPlugin.js";

const app: FastifyPluginAsync<FastifyServerOptions> = async (
	fastify,
	opts
): Promise<void> => {
	fastify.register(conformanceApiPlugin, {
		api: new ConformanceApiService(conformanceTestsJson.tests),
		caseInsenstiveQueryStringKeys: true,
		includeErrorDetails: true,
	});
};

export default app;
export const options: FastifyServerOptions = {
	caseSensitive: false,
};
