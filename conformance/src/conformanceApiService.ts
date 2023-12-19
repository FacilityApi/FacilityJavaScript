import { IServiceError, IServiceResult } from "facility-core";
import { IConformanceApi, IGetApiInfoRequest, IGetApiInfoResponse, IGetWidgetsRequest, IGetWidgetsResponse, ICreateWidgetRequest, ICreateWidgetResponse, IGetWidgetRequest, IGetWidgetResponse, IDeleteWidgetRequest, IDeleteWidgetResponse, IGetWidgetBatchRequest, IGetWidgetBatchResponse, IMirrorFieldsRequest, IMirrorFieldsResponse, ICheckQueryRequest, ICheckQueryResponse, ICheckPathRequest, ICheckPathResponse, IMirrorHeadersRequest, IMirrorHeadersResponse, IMixedRequest, IMixedResponse, IRequiredRequest, IRequiredResponse, IMirrorBytesRequest, IMirrorBytesResponse, IMirrorTextRequest, IMirrorTextResponse, IBodyTypesRequest, IBodyTypesResponse } from "./conformanceApiTypes";

export type ConformanceApiTest = {
	test: string;
	method: string;
	request: unknown;
	response?: unknown;
	error?: IServiceError;
};

export class ConformanceApiService implements IConformanceApi {
	constructor(tests: ConformanceApiTest[]) {
		this._tests = tests;
	}

	getApiInfo(request: IGetApiInfoRequest, context?: unknown): Promise<IServiceResult<IGetApiInfoResponse>> {
		return this.execute("getApiInfo", request);
	}

	getWidgets(request: IGetWidgetsRequest, context?: unknown): Promise<IServiceResult<IGetWidgetsResponse>> {
		return this.execute("getWidgets", request);
	}

	createWidget(request: ICreateWidgetRequest, context?: unknown): Promise<IServiceResult<ICreateWidgetResponse>> {
		return this.execute("createWidget", request);
	}

	getWidget(request: IGetWidgetRequest, context?: unknown): Promise<IServiceResult<IGetWidgetResponse>> {
		return this.execute("getWidget", request);
	}

	deleteWidget(request: IDeleteWidgetRequest, context?: unknown): Promise<IServiceResult<IDeleteWidgetResponse>> {
		return this.execute("deleteWidget", request);
	}

	getWidgetBatch(request: IGetWidgetBatchRequest, context?: unknown): Promise<IServiceResult<IGetWidgetBatchResponse>> {
		return this.execute("getWidgetBatch", request);
	}

	mirrorFields(request: IMirrorFieldsRequest, context?: unknown): Promise<IServiceResult<IMirrorFieldsResponse>> {
		return this.execute("mirrorFields", request);
	}

	checkQuery(request: ICheckQueryRequest, context?: unknown): Promise<IServiceResult<ICheckQueryResponse>> {
		return this.execute("checkQuery", request);
	}

	checkPath(request: ICheckPathRequest, context?: unknown): Promise<IServiceResult<ICheckPathResponse>> {
		return this.execute("checkPath", request);
	}

	mirrorHeaders(request: IMirrorHeadersRequest, context?: unknown): Promise<IServiceResult<IMirrorHeadersResponse>> {
		return this.execute("mirrorHeaders", request);
	}

	mixed(request: IMixedRequest, context?: unknown): Promise<IServiceResult<IMixedResponse>> {
		return this.execute("mixed", request);
	}

	required(request: IRequiredRequest, context?: unknown): Promise<IServiceResult<IRequiredResponse>> {
		return this.execute("required", request);
	}

	mirrorBytes(request: IMirrorBytesRequest, context?: unknown): Promise<IServiceResult<IMirrorBytesResponse>> {
		return this.execute("mirrorBytes", request);
	}

	mirrorText(request: IMirrorTextRequest, context?: unknown): Promise<IServiceResult<IMirrorTextResponse>> {
		return this.execute("mirrorText", request);
	}

	bodyTypes(request: IBodyTypesRequest, context?: unknown): Promise<IServiceResult<IBodyTypesResponse>> {
		return this.execute("bodyTypes", request);
	}

	private async execute<TRequest, TResponse>(methodName: string, request: TRequest): Promise<IServiceResult<TResponse>> {
		const testsWithMethodName = this._tests.filter((x) => x.method === methodName);

		if (testsWithMethodName.length === 0) {
			return this.failure({
				code: "InvalidRequest",
				message: `No tests found for method ${methodName}.`,
			});
		}

		const testsWithMatchingRequest = testsWithMethodName.filter((x) => {
			return JSON.stringify(x.request) === JSON.stringify(request);
		});

		if (testsWithMatchingRequest.length !== 1) {
			return this.failure({
				code: "InvalidRequest",
				message: `${testsWithMatchingRequest.length} of ${testsWithMethodName.length} tests for method ${methodName} matched request:`,
			});
		}

		var testInfo = testsWithMatchingRequest[0];
		return testInfo.error ? this.failure(testInfo.error) : this.success(testInfo.response as TResponse);
	}

	private success<T>(result: T): IServiceResult<T> { return { value: result }; }

	private failure<T>(error: IServiceError): IServiceResult<T> { return { error}; }

	private readonly _tests: readonly ConformanceApiTest[];

}
