/** A service result. */
export interface IServiceResultBase {
	/** The error. */
	error?: IServiceError;
}

/** A service result. */
export interface IServiceResult<T> extends IServiceResultBase {
	/** The value. */
	value?: T;
}

/** An error. */
export interface IServiceError {
	/** The error code. */
	code?: string;
	/** The error message. (For developers, not end users.) */
	message?: string;
	/** Advanced error details. */
	details?: Record<string, unknown>;
	/** The inner error. */
	innerError?: IServiceError;
}

/** Options for an HTTP client. */
export interface IHttpClientOptions {
	/** The fetch object. */
	fetch: HttpClientUtility.IFetch;
	/** The base URI of the service. */
	baseUri?: string;
}

/** Helpers for HTTP clients. */
// eslint-disable-next-line @typescript-eslint/no-namespace
export namespace HttpClientUtility {
	/** The fetch function. */
	export interface IFetch {
		(uri: string, request: IFetchRequest): Promise<IFetchResponse>;
	}

	/** The minimal fetch request. */
	export interface IFetchRequest {
		method?: string;
		headers?: Record<string, string>;
		body?: string;
	}

	/** The minimal fetch response. */
	export interface IFetchResponse {
		status: number;
		headers: {
			get(name: string): string | null;
		};
		json(): Promise<unknown>;
	}

	/** A fetch response with any fetched content. */
	export interface IFetchedResponseWithContent {
		/** The fetch response. */
		response: IFetchResponse;
		/** The fetched JSON, if any. */
		json?: unknown;
	}

	const standardErrorCodes: { [index: number]: string } = {
		'304': 'NotModified',
		'400': 'InvalidRequest',
		'401': 'NotAuthenticated',
		'403': 'NotAuthorized',
		'404': 'NotFound',
		'409': 'Conflict',
		'413': 'RequestTooLarge',
		'429': 'TooManyRequests',
		'500': 'InternalError',
		'503': 'ServiceUnavailable',
	};

	const jsonContentType = 'application/json';

	/** Fetch JSON using the specified fetch, URI, and request. */
	export function fetchResponse(
		fetch: IFetch,
		uri: string,
		request: IFetchRequest
	): Promise<IFetchedResponseWithContent> {
		return fetch(uri, request).then((response) => {
			if (!response.headers || !response.status || typeof response.json !== 'function') {
				throw new TypeError('fetch must resolve Promise with { status, headers, json() }.');
			}
			const contentType = response.headers.get('content-type');
			if (!contentType) {
				return Promise.resolve({ response: response, json: {} });
			}
			if (contentType.toLowerCase().substr(0, jsonContentType.length) === jsonContentType) {
				const jsonPromise = response.json();
				if (!jsonPromise || typeof jsonPromise.then !== 'function') {
					throw new TypeError('json() of fetch response must return a Promise.');
				}
				return jsonPromise.then((json) => ({
					response: response,
					json: json,
				}));
			}
			return Promise.resolve({ response: response });
		});
	}

	/** Creates an error result for the specified response. */
	export function createResponseError(status: number, json?: unknown): IServiceResultBase {
		if (isServiceError(json)) {
			return { error: json };
		}
		const isClientError = status >= 400 && status <= 499;
		const isServerError = status >= 500 && status <= 599;
		const errorCode = standardErrorCodes[status] || (isClientError ? 'InvalidRequest' : 'InvalidResponse');
		const message = isServerError
			? 'HTTP server error'
			: isClientError
			? 'HTTP client error'
			: 'Unexpected HTTP status code';
		return { error: { code: errorCode, message: `${message}: ${status}` } };
	}

	/** Creates an error result for a required request field. */
	export function createRequiredRequestFieldError(name: string): IServiceResultBase {
		return {
			error: {
				code: 'InvalidRequest',
				message: `The request field '${name}' is required.`,
			},
		};
	}
}

function isServiceError(json: unknown): json is IServiceError {
	return typeof json === 'object' && json != null && typeof (json as Record<string, unknown>).code === 'string';
}
