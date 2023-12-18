import { createHttpClient } from "../src/ts/conformanceApi";
import { expect, should } from "chai";
import nodeFetch from "node-fetch";
import conformanceTestsJson from "../ConformanceTests.json";
import { isDeepStrictEqual } from "util";
import { HttpClientUtility } from "facility-core";

const tests = conformanceTestsJson.tests;

validateTests();
should();

const fetch: HttpClientUtility.IFetch = (uri, request) => {
	return nodeFetch("http://localhost:4117/" + uri, request);
};

const httpClient = createHttpClient({ fetch });

tests.forEach((data) => {
	if (data.test !== "getApiInfo") return;
	it(data.test, async () => {
		if (data.httpRequest) {
			const result = await fetch(
				data.httpRequest.path.replace(/^\//, ""),
				{ method: data.httpRequest.method });
			if (result.status >= 300) {
				throw new Error(
					`Raw http request failed with status code ${
						result.status
					}, ${JSON.stringify(await result.json())}`
				);
			}
		} else {
			const result = await(httpClient as any)[data.method](data.request);
			expect({
				error: result.error ?? undefined,
				value: result.value ?? undefined,
			}).to.be.deep.equal({
				error: data.error ?? undefined,
				value: data.response ?? undefined,
			});
		}
	});
});

function validateTests() {
	tests.forEach((data) => {
		if (!data.test) {
			throw new Error(`Test is missing 'test'`);
		}
		if (!data.method) {
			throw new Error(`'${data.test}' is missing 'method'`);
		}
		if (data.httpRequest && !data.httpRequest.method) {
			throw new Error(`Test '${data.test}' is missintg 'httpRequest.method'`);
		}
		if (data.httpRequest && !data.httpRequest.path) {
			throw new Error(`Test '${data.test}' is missintg 'httpRequest.path'`);
		}
		if (tests.filter((x) => x.test === data.test).length !== 1) {
			throw new Error(`Multiple tests found with name '${data.test}'`);
		}
		if (tests.filter((x) => x.method === data.method && isDeepStrictEqual(x.request, data.request)).length !== 1) {
			throw new Error(`Multiple tests found for with method '${data.method}' and request '${JSON.stringify(data.request)}'`);
		}
	});
}

