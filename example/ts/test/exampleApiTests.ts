import { createHttpClient } from '../src/exampleApi';
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
			return null;
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
				expect(request.method).to.equal('GET');
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
			expect(result.value).to.deep.equal({
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
			expect(result.error).to.deep.equal({
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
			expect(result.error).to.deep.equal({
				code: 'invalidRequest',
				message: 'The request field \'id\' is required.'
			})
		});
	});

});
