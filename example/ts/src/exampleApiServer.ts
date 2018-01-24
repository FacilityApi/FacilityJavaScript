// DO NOT EDIT: generated by fsdgenjs

import * as bodyParser from 'body-parser';
import * as express from 'express';
import { IServiceResult, IServiceError } from 'facility-core';
import { IExampleApi, IGetWidgetsRequest, IGetWidgetsResponse, ICreateWidgetRequest, ICreateWidgetResponse, IGetWidgetRequest, IGetWidgetResponse, IDeleteWidgetRequest, IDeleteWidgetResponse, IEditWidgetRequest, IEditWidgetResponse, IGetWidgetBatchRequest, IGetWidgetBatchResponse, IGetWidgetWeightRequest, IGetWidgetWeightResponse, IGetPreferenceRequest, IGetPreferenceResponse, ISetPreferenceRequest, ISetPreferenceResponse, IGetInfoRequest, IGetInfoResponse, INotRestfulRequest, INotRestfulResponse, IKitchenRequest, IKitchenResponse, IWidget, IWidgetJob, IPreference, IObsoleteData, IKitchenSink } from './exampleApiTypes';
export * from './exampleApiTypes';

const standardErrorCodes: { [code: string]: number } = {
	'notModified': 304,
	'invalidRequest': 400,
	'notAuthenticated': 401,
	'notAuthorized': 403,
	'notFound': 404,
	'conflict': 409,
	'requestTooLarge': 413,
	'tooManyRequests': 429,
	'internalError': 500,
	'serviceUnavailable': 503,
};

export function createApp(service: IExampleApi) {
	const app = express();
	app.use(bodyParser.json());
	app.use(bodyParser.urlencoded({ extended: true }));

	/** Gets widgets. */
	app.get('/widgets', function (req, res, next) {
		const request: IGetWidgetsRequest = {};
		if (req.query['q'] != null) {
			request.query = req.query['q'];
		}
		if (req.query['limit'] != null) {
			request.limit = parseInt(req.query['limit'], 10);
		}
		if (req.query['sort'] != null) {
			request.sort = req.query['sort'];
		}
		if (req.query['desc'] != null) {
			request.desc = req.query['desc'] === 'true';
		}
		if (req.query['maxWeight'] != null) {
			request.maxWeight = parseFloat(req.query['maxWeight']);
		}
		if (req.query['minPrice'] != null) {
			request.minPrice = parseFloat(req.query['minPrice']);
		}

		return service.getWidgets(request)
			.then(result => {
				if (result.error) {
					const status = standardErrorCodes[result.error.code] || 500;
					res.status(status).send(result.error.details);
				} else {
					res.send(result.value);
				}
			})
			.catch(next);
	});

	/** Creates a new widget. */
	app.post('/widgets', function (req, res, next) {
		const request: ICreateWidgetRequest = {};

		return service.createWidget(request)
			.then(result => {
				if (result.error) {
					const status = standardErrorCodes[result.error.code] || 500;
					res.status(status).send(result.error.details);
				} else {
					res.send(result.value);
				}
			})
			.catch(next);
	});

	/** Gets the specified widget. */
	app.get('/widgets/:id', function (req, res, next) {
		const request: IGetWidgetRequest = {};
		request.id = req.params.id;

		return service.getWidget(request)
			.then(result => {
				if (result.error) {
					const status = standardErrorCodes[result.error.code] || 500;
					res.status(status).send(result.error.details);
				} else {
					res.send(result.value);
				}
			})
			.catch(next);
	});

	/** Deletes the specified widget. */
	app.delete('/widgets/:id', function (req, res, next) {
		const request: IDeleteWidgetRequest = {};
		request.id = req.params.id;

		return service.deleteWidget(request)
			.then(result => {
				if (result.error) {
					const status = standardErrorCodes[result.error.code] || 500;
					res.status(status).send(result.error.details);
				} else {
					res.send(result.value);
				}
			})
			.catch(next);
	});

	/** Edits widget. */
	app.post('/widgets/:id', function (req, res, next) {
		const request: IEditWidgetRequest = {};
		request.id = req.params.id;

		return service.editWidget(request)
			.then(result => {
				if (result.error) {
					const status = standardErrorCodes[result.error.code] || 500;
					res.status(status).send(result.error.details);
				} else {
					res.send(result.value);
				}
			})
			.catch(next);
	});

	/** Gets the specified widgets. */
	app.post('/widgets/get', function (req, res, next) {
		const request: IGetWidgetBatchRequest = {};

		return service.getWidgetBatch(request)
			.then(result => {
				if (result.error) {
					const status = standardErrorCodes[result.error.code] || 500;
					res.status(status).send(result.error.details);
				} else {
					res.send(result.value);
				}
			})
			.catch(next);
	});

	/**
	 * Gets the widget weight.
	 * @deprecated
	 */
	app.get('/widgets/:id/weight', function (req, res, next) {
		const request: IGetWidgetWeightRequest = {};
		request.id = req.params.id;

		return service.getWidgetWeight(request)
			.then(result => {
				if (result.error) {
					const status = standardErrorCodes[result.error.code] || 500;
					res.status(status).send(result.error.details);
				} else {
					res.send(result.value);
				}
			})
			.catch(next);
	});

	/** Gets a widget preference. */
	app.get('/prefs/:key', function (req, res, next) {
		const request: IGetPreferenceRequest = {};
		request.key = req.params.key;

		return service.getPreference(request)
			.then(result => {
				if (result.error) {
					const status = standardErrorCodes[result.error.code] || 500;
					res.status(status).send(result.error.details);
				} else {
					res.send(result.value);
				}
			})
			.catch(next);
	});

	/** Sets a widget preference. */
	app.put('/prefs/:key', function (req, res, next) {
		const request: ISetPreferenceRequest = {};
		request.key = req.params.key;

		return service.setPreference(request)
			.then(result => {
				if (result.error) {
					const status = standardErrorCodes[result.error.code] || 500;
					res.status(status).send(result.error.details);
				} else {
					res.send(result.value);
				}
			})
			.catch(next);
	});

	/** Gets service info. */
	app.get('/', function (req, res, next) {
		const request: IGetInfoRequest = {};

		return service.getInfo(request)
			.then(result => {
				if (result.error) {
					const status = standardErrorCodes[result.error.code] || 500;
					res.status(status).send(result.error.details);
				} else {
					res.send(result.value);
				}
			})
			.catch(next);
	});

	/** Demonstrates the default HTTP behavior. */
	app.post('/notRestful', function (req, res, next) {
		const request: INotRestfulRequest = {};

		return service.notRestful(request)
			.then(result => {
				if (result.error) {
					const status = standardErrorCodes[result.error.code] || 500;
					res.status(status).send(result.error.details);
				} else {
					res.send(result.value);
				}
			})
			.catch(next);
	});

	app.post('/kitchen', function (req, res, next) {
		const request: IKitchenRequest = {};

		return service.kitchen(request)
			.then(result => {
				if (result.error) {
					const status = standardErrorCodes[result.error.code] || 500;
					res.status(status).send(result.error.details);
				} else {
					res.send(result.value);
				}
			})
			.catch(next);
	});

	return app;
}
