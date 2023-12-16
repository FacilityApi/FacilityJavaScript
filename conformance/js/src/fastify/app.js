import { ConformanceApiService } from "../../../ts/src/conformanceApiService.js";
import conformanceTestsJson from "../../../ConformanceTests.json";
import { conformanceApiPlugin } from "./conformanceApiPlugin.js";

const options = {};

const app = async (fastify, opts) => {
  fastify.register(conformanceApiPlugin, {
    api: new ConformanceApiService(conformanceTestsJson.tests),
  });
};

export default app;
export { app, options };
