import { FastifyPluginAsync } from "fastify";
import { ConformanceApiService } from "../conformanceApiService.js";
import conformanceTestsJson from "../../../ConformanceTests.json";
import { conformanceApiPlugin } from "./conformanceApiPlugin.js";

export type AppOptions = {};
const options: AppOptions = {};

const app: FastifyPluginAsync<AppOptions> = async (
	fastify,
	opts
): Promise<void> => {
	fastify.register(conformanceApiPlugin, {
		api: new ConformanceApiService(conformanceTestsJson.tests),
		caseInsenstiveQueryStringKeys: true,
	});
};

export default app;
export { app, options };
