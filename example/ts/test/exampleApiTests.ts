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

	it('get widget with ID', () => {
		return createHttpClient({
			fetch: (uri, request) => {
				uri.should.equal('http://local.example.com/v1/widgets/xyzzy');
				request.method.should.equal('GET');
				expect(request.headers).to.not.exist;
				expect(request.body).to.not.exist;
				return createFetchResponse(200, {
					id: 'xyzzy',
					name: 'Xyzzy'
				});
			}
		}).getWidget({
			id: 'xyzzy'
		}).then(result => {
			result.value.should.deep.equal({
				widget: {
					id: 'xyzzy',
					name: 'Xyzzy'
				}
			});
		});
	});

	it('get widget with missing ID', () => {
		return createHttpClient({
			fetch: (uri, request) => {
				throw new Error();
			}
		}).getWidget({}).then(result => {
			result.error.should.deep.equal({
				code: 'invalidRequest',
				message: 'The request field \'id\' is required.'
			})
		});
	});

	it('get widget with blank ID', () => {
		return createHttpClient({
			fetch: (uri, request) => {
				throw new Error();
			}
		}).getWidget({
			id: ''
		}).then(result => {
			result.error.should.deep.equal({
				code: 'invalidRequest',
				message: 'The request field \'id\' is required.'
			})
		});
	});

});
