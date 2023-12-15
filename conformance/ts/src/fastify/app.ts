import { FastifyPluginAsync } from "fastify";

export type AppOptions = {};
const options: AppOptions = {};

const app: FastifyPluginAsync<AppOptions> = async (fastify, opts): Promise<void> => {
	fastify.get('/', () => {
		return { hello: 'world' };
	});
};

export default app;
export { app, options };
