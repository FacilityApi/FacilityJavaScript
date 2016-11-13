import { createHttpClient } from '../exampleApi';
import { expect, should } from 'chai';

should();

function createFetchResponse(status: number, body?: any) {
	const headers = {
		get: (name: string) => {
			if (typeof body === 'object' && name.toLowerCase() === 'content-type') {
				return 'application/json';
			} else if (typeof body === 'string' && name.toLowerCase() === 'content-type') {
				return 'text/html';
			}
			return undefined;
		}
	};
	const json = () => Promise.resolve(body);
	return Promise.resolve({ status, headers, json });
}

describe('createHttpClient', () => {

	it('get widgets returns 200', () => {
		return createHttpClient({
			fetch: (uri, request) => {
				uri.should.equal('http://local.example.com/v1/widgets');
				request.method.should.equal('GET');
				expect(request.headers).to.not.exist;
				expect(request.body).to.not.exist;
				return createFetchResponse(200, {});
			}
		}).getWidgets({}).then(result => {
			result.value.should.deep.equal({});
		});
	});

});
