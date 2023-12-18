import { ConformanceApiService } from "../../ts/conformanceApiService.js";
import conformanceTestsJson from "../../../ConformanceTests.json";
import { conformanceApiPlugin } from "./conformanceApiPlugin.js";

const app = async (fastify, opts) => {
  fastify.register(conformanceApiPlugin, {
    api: new ConformanceApiService(conformanceTestsJson.tests),
    caseInsenstiveQueryStringKeys: true,
  });
};

export default app;
export const options = {
  caseSensitive: false,
};
