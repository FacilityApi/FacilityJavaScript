import { createHttpClient } from '../src/conformanceApi';
import { expect, should } from 'chai';
import fetch from 'node-fetch';
import * as fs from 'fs';
import * as path from 'path';

should();

const httpClient = createHttpClient({
	fetch: (uri, request) => {
		return fetch('http://localhost:4117/' + uri, request);
	}
});

const testData = JSON.parse(
	fs.readFileSync(path.resolve(__dirname, '../../conformanceTests.json'), 'utf8'));

describe('tests', () => {

	testData.tests.forEach((data: any) => {
		it(data.test, () => {
			return ((httpClient as any)[data.method](data.request))
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
