import { createHttpClient } from '../src/exampleApi';
import { createApp } from '../src/exampleApiServer';
import { IServiceResult, IServiceError } from 'facility-core';
import { IExampleApi, IGetWidgetsRequest, IGetWidgetsResponse, ICreateWidgetRequest, ICreateWidgetResponse, IGetWidgetRequest, IGetWidgetResponse, IDeleteWidgetRequest, IDeleteWidgetResponse, IEditWidgetRequest, IEditWidgetResponse, IGetWidgetBatchRequest, IGetWidgetBatchResponse, IGetWidgetWeightRequest, IGetWidgetWeightResponse, IGetPreferenceRequest, IGetPreferenceResponse, ISetPreferenceRequest, ISetPreferenceResponse, IGetInfoRequest, IGetInfoResponse, INotRestfulRequest, INotRestfulResponse, IKitchenRequest, IKitchenResponse, IWidget, IWidgetJob, IPreference, IObsoleteData, IKitchenSink } from '../src/exampleApiTypes';
import * as crypto from 'crypto';
import { expect, should } from 'chai';
import * as http from 'http';
import fetch from 'node-fetch';

should();

function md5(data: string | Buffer): string {
	return crypto.createHash('md5')
		.update(data)
		.digest('base64');
}

class ExampleApi implements IExampleApi {
	getWidgets(request: IGetWidgetsRequest): Promise<IServiceResult<IGetWidgetsResponse>> {
		throw new Error('Method not implemented.');
	}

	createWidget(request: ICreateWidgetRequest): Promise<IServiceResult<ICreateWidgetResponse>> {
		throw new Error('Method not implemented.');
	}

	async getWidget(request: IGetWidgetRequest): Promise<IServiceResult<IGetWidgetResponse>> {
		const { id, ifNoneMatch } = request;
		if (!id) {
			throw new Error('Invalid request.');
		}

		const eTag = `"${md5(id)}"`;
		if (ifNoneMatch === eTag) {
			return {
				value: {
					notModified: true,
				},
			};
		}

		const name = id[0].toUpperCase() + id.substr(1);
		return {
			value: {
				widget: {
					id,
					name,
				},
				eTag,
			}
		};
	}

	deleteWidget(request: IDeleteWidgetRequest): Promise<IServiceResult<IDeleteWidgetResponse>> {
		throw new Error('Method not implemented.');
	}

	async editWidget(request: IEditWidgetRequest): Promise<IServiceResult<IEditWidgetResponse>> {
		const { id, ops, weight } = request;
		if (!id) {
			throw new Error('Invalid request.');
		}

		if (weight == undefined || weight < 0) {
			return {
				error: {
					code: 'invalidRequest',
				},
			};
		}

		if (ops && ops.length) {
			return {
				value: {
					job: {
						id: Math.floor(Math.random() * 1000).toString(),
					},
				},
			};
		}

		return {
			value: {
				widget: {
					id,
				},
			}
		};
	}

	getWidgetBatch(request: IGetWidgetBatchRequest): Promise<IServiceResult<IGetWidgetBatchResponse>> {
		throw new Error('Method not implemented.');
	}

	getWidgetWeight(request: IGetWidgetWeightRequest): Promise<IServiceResult<IGetWidgetWeightResponse>> {
		throw new Error('Method not implemented.');
	}

	getPreference(request: IGetPreferenceRequest): Promise<IServiceResult<IGetPreferenceResponse>> {
		throw new Error('Method not implemented.');
	}

	setPreference(request: ISetPreferenceRequest): Promise<IServiceResult<ISetPreferenceResponse>> {
		throw new Error('Method not implemented.');
	}

	getInfo(request: IGetInfoRequest): Promise<IServiceResult<IGetInfoResponse>> {
		throw new Error('Method not implemented.');
	}

	notRestful(request: INotRestfulRequest): Promise<IServiceResult<INotRestfulResponse>> {
		throw new Error('Method not implemented.');
	}

	kitchen(request: IKitchenRequest): Promise<IServiceResult<IKitchenResponse>> {
		throw new Error('Method not implemented.');
	}
}

describe('createApp', () => {

	const app = createApp(new ExampleApi());
	let server: http.Server;
	let client: IExampleApi;

	before(() => {
		server = app.listen(0, '127.0.0.1', () => {
			const { address, port } = server.address();

			client = createHttpClient({
				fetch,
				baseUri: `http://${address}:${port}`
			});
		});
	});

	after(() => {
		server.close();
	})

	it('get widget with ID', async () => {
		const result = await client.getWidget({
			id: 'xyzzy'
		});

		expect(result.value).to.be.ok;
		delete result.value!.eTag;
		expect(result.value).to.deep.equal({
			widget: {
				id: 'xyzzy',
				name: 'Xyzzy'
			}
		});
	});

	it('get widget with ID with etag', async () => {
		const id = 'xyzzy';

		const result1 = await client.getWidget({
			id
		});
		expect(result1.value).to.be.ok;
		const eTag = result1.value!.eTag;

		const result2 = await client.getWidget({
			id,
			ifNoneMatch: eTag,
		});
		expect(result2.value).to.be.ok;
		expect(result2.value!.notModified).to.be.true;
		expect(result2.value!.widget).to.be.undefined;

		const result3 = await client.getWidget({
			id,
			ifNoneMatch: 'asdf',
		});
		expect(result3.value).to.be.ok;
		expect(result3.value).to.deep.equal(result1.value);
	});

	it('edit widget', async () => {
		const result = await client.editWidget({
			id: 'xyzzy',
			weight: 1,
		});

		expect(result.value).to.be.ok;
		expect(result.value!.widget).to.be.ok;
		expect(result.value!.widget!.id).to.equal('xyzzy');
	});

	it('edit widget async', async () => {
		const result = await client.editWidget({
			id: 'xyzzy',
			ops: [{
				foo: 'bar',
			}],
			weight: 1,
		});

		expect(result.value).to.be.ok;
		expect(result.value!.job).to.be.ok;
	});

	it('edit widget bad request', async () => {
		const result = await client.editWidget({
			id: 'xyzzy',
			weight: -1,
		});

		expect(result.error).to.be.ok;
		expect(result.error!.code).to.equal('invalidRequest');
	});
});
