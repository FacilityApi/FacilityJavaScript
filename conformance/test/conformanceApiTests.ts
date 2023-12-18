import { createHttpClient } from "../src/ts/conformanceApi";
import { expect, should } from "chai";
import fetch from "node-fetch";
import conformanceTestsJson from "../ConformanceTests.json";
import { isDeepStrictEqual } from "util";

const tests = conformanceTestsJson.tests;

validateTests();
should();

const httpClient = createHttpClient({
	fetch: (uri, request) => {
		return fetch("http://localhost:4117/" + uri, request);
	},
});

describe("tests", () => {
	tests.forEach((data: any) => {
		it(data.test, async () => {
			return (httpClient as any)
				[data.method](data.request)
				.then((result: any) => {
					expect({
						error: result.error ?? undefined,
						value: result.value ?? undefined,
					}).to.be.deep.equal({
						error: data.error ?? undefined,
						value: data.response ?? undefined,
					});
				});
		});
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

