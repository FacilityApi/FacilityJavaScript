import fs from "fs";
import path from "path";
import { FastifyPluginAsync } from "fastify";
import { ConformanceApiService } from "../conformanceApiService.js";
import { handRolledPlugin } from "./handRolledPlugin.js";
import conformanceTestsJson from '../../../ConformanceTests.json';

export type AppOptions = {};
const options: AppOptions = {};

const app: FastifyPluginAsync<AppOptions> = async (fastify, opts): Promise<void> => {
	fastify.register(handRolledPlugin, { api: new ConformanceApiService(conformanceTestsJson.tests) });
};

export default app;
export { app, options };
