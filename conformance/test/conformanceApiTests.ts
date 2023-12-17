import { createHttpClient } from "../src/ts/conformanceApi";
import { expect, should } from "chai";
import fetch from "node-fetch";
import conformanceTestsJson from "../ConformanceTests.json";

should();

const httpClient = createHttpClient({
	fetch: (uri, request) => {
		return fetch("http://localhost:4117/" + uri, request);
	},
});

describe("tests", () => {
	conformanceTestsJson.tests.forEach((data: any) => {
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
