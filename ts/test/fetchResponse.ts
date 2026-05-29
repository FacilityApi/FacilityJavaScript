import { HttpClientUtility } from '../src/facilityCore';
import { expect } from 'chai';

const createResponseError = HttpClientUtility.createResponseError;
const fetchResponse = HttpClientUtility.fetchResponse;

describe('fetchResponse', () => {

	it('should return invalid response when parsing JSON fails for success status', async () => {
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
			.to.deep.equal({ error: { code: 'InvalidResponse', message: 'HTTP content is invalid: ' } });
	});

	it('should preserve error status when parsing JSON fails for failure status', async () => {
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

	it('should return parsed JSON when parsing succeeds', async () => {
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
