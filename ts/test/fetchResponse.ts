import { HttpClientUtility } from '../src/facilityCore';
import { expect, should } from 'chai';

should();

const createResponseError = HttpClientUtility.createResponseError;
const fetchResponse = HttpClientUtility.fetchResponse;

describe('fetchResponse', () => {

	it('should return invalid response when parsing json fails for success status', async () => {
		const result = await fetchResponse(
			() => Promise.resolve({
				status: 200,
				headers: {
					get(name: string) {
						return name === 'content-type' ? 'application/json' : null;
					},
				},
				json() {
					return Promise.reject(new SyntaxError('Unexpected end of JSON input'));
				},
			}),
			'http://example.com',
			{},
		);

		expect(createResponseError(result.response.status, result.json))
			.to.deep.equal({ error: { code: 'InvalidResponse', message: 'Unexpected HTTP status code: 200' } });
	});

	it('should preserve error status when parsing json fails for failure status', async () => {
		const result = await fetchResponse(
			() => Promise.resolve({
				status: 404,
				headers: {
					get(name: string) {
						return name === 'content-type' ? 'application/json' : null;
					},
				},
				json() {
					return Promise.reject(new SyntaxError('Unexpected end of JSON input'));
				},
			}),
			'http://example.com',
			{},
		);

		expect(createResponseError(result.response.status, result.json))
			.to.deep.equal({ error: { code: 'NotFound', message: 'HTTP client error: 404' } });
	});

	it('should return parsed json when parsing succeeds', async () => {
		const result = await fetchResponse(
			() => Promise.resolve({
				status: 200,
				headers: {
					get(name: string) {
						return name === 'content-type' ? 'application/json' : null;
					},
				},
				json() {
					return Promise.resolve({ value: 'ok' });
				},
			}),
			'http://example.com',
			{},
		);

		expect(result.json).to.deep.equal({ value: 'ok' });
	});

});
