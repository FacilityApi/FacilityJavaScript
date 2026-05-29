import { HttpClientUtility } from '../src/facilityCore';
import { expect, should } from 'chai';

should();

const fetchResponse = HttpClientUtility.fetchResponse;

describe('fetchResponse', () => {

	it('should handle json content-type with content-length 0', async () => {
		const mockFetch: HttpClientUtility.IFetch = () => Promise.resolve({
			status: 202,
			headers: {
				get(name: string) {
					if (name === 'content-type') return 'application/json';
					if (name === 'content-length') return '0';
					return null;
				},
			},
			json() {
				return Promise.reject(new SyntaxError('Unexpected end of JSON input'));
			},
		});

		const result = await fetchResponse(mockFetch, 'http://example.com', {});
		expect(result.response.status).to.equal(202);
		expect(result.json).to.deep.equal({});
	});

	it('should parse json when content-type is json and body is present', async () => {
		const mockFetch: HttpClientUtility.IFetch = () => Promise.resolve({
			status: 200,
			headers: {
				get(name: string) {
					if (name === 'content-type') return 'application/json';
					if (name === 'content-length') return '15';
					return null;
				},
			},
			json() {
				return Promise.resolve({ value: 'ok' });
			},
		});

		const result = await fetchResponse(mockFetch, 'http://example.com', {});
		expect(result.response.status).to.equal(200);
		expect(result.json).to.deep.equal({ value: 'ok' });
	});

	it('should return empty json when no content-type header', async () => {
		const mockFetch: HttpClientUtility.IFetch = () => Promise.resolve({
			status: 204,
			headers: {
				get() { return null; },
			},
			json() {
				return Promise.reject(new Error('should not be called'));
			},
		});

		const result = await fetchResponse(mockFetch, 'http://example.com', {});
		expect(result.response.status).to.equal(204);
		expect(result.json).to.deep.equal({});
	});

});
