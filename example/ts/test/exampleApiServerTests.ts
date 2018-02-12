import { createHttpClient } from '../src/exampleApi';
import { createApp } from '../src/exampleApiServer';
import { IServiceResult, IServiceError } from 'facility-core';
import { IExampleApi, IGetWidgetsRequest, IGetWidgetsResponse, ICreateWidgetRequest, ICreateWidgetResponse, IGetWidgetRequest, IGetWidgetResponse, IDeleteWidgetRequest, IDeleteWidgetResponse, IEditWidgetRequest, IEditWidgetResponse, IGetWidgetBatchRequest, IGetWidgetBatchResponse, IGetWidgetWeightRequest, IGetWidgetWeightResponse, IGetPreferenceRequest, IGetPreferenceResponse, ISetPreferenceRequest, ISetPreferenceResponse, IGetInfoRequest, IGetInfoResponse, INotRestfulRequest, INotRestfulResponse, IKitchenRequest, IKitchenResponse, IWidget, IWidgetJob, IPreference, IObsoleteData, IKitchenSink } from '../src/exampleApiTypes';
import { expect, should } from 'chai';
import * as http from 'http';
import fetch from 'node-fetch';

should();

class ExampleApi implements IExampleApi {
	getWidgets(request: IGetWidgetsRequest): Promise<IServiceResult<IGetWidgetsResponse>> {
		throw new Error('Method not implemented.');
	}

	createWidget(request: ICreateWidgetRequest): Promise<IServiceResult<ICreateWidgetResponse>> {
		throw new Error('Method not implemented.');
	}

	async getWidget(request: IGetWidgetRequest): Promise<IServiceResult<IGetWidgetResponse>> {
		const { id } = request;
		const name = id && id[0].toUpperCase() + id.substr(1);
		return {
			value: {
				widget: {
					id,
					name,
				}
			}
		};
	}

	deleteWidget(request: IDeleteWidgetRequest): Promise<IServiceResult<IDeleteWidgetResponse>> {
		throw new Error('Method not implemented.');
	}

	editWidget(request: IEditWidgetRequest): Promise<IServiceResult<IEditWidgetResponse>> {
		throw new Error('Method not implemented.');
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
		const server = app.listen(0, '127.0.0.1', () => {
			const { address, port } = server.address();

			client = createHttpClient({
				fetch,
				baseUri: `http://${address}:${port}`
			});
		});
	});

	it('get widget with ID', () => {
		return client.getWidget({
			id: 'xyzzy'
		}).then(result => {
			expect(result.value).to.be.ok;
			delete result.value!.eTag;
			expect(result.value).to.deep.equal({
				widget: {
					id: 'xyzzy',
					name: 'Xyzzy'
				}
			});
		});
	});

});
