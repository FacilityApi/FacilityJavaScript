import { HttpClientUtility } from '../src/facilityCore';
import { expect, should } from 'chai';

should();

const createResponseError = HttpClientUtility.createResponseError;

describe('createResponseError', () => {

	it('should return json with code', () => {
		createResponseError(500, { 'code': 'heyNow' })
			.should.deep.equal({ 'error': { 'code': 'heyNow' } });
	});

	it('should return json with code on any status', () => {
		createResponseError(200, { 'code': 'heyNow' })
			.should.deep.equal({ 'error': { 'code': 'heyNow' } });
	});

	it('should return custom error with no code', () => {
		createResponseError(500, { 'message': 'heyNow' })
			.should.deep.equal({ 'error': { 'code': 'InternalError', 'message': 'HTTP server error: 500' } });
	});

	it('should return custom error with no JSON', () => {
		createResponseError(500)
			.should.deep.equal({ 'error': { 'code': 'InternalError', 'message': 'HTTP server error: 500' } });
	});

	it('should return invalid response with weird server error', () => {
		createResponseError(599)
			.should.deep.equal({ 'error': { 'code': 'InvalidResponse', 'message': 'HTTP server error: 599' } });
	});

	it('should return invalid request with weird client error', () => {
		createResponseError(499)
			.should.deep.equal({ 'error': { 'code': 'InvalidRequest', 'message': 'HTTP client error: 499' } });
	});

	it('should return invalid response with success', () => {
		createResponseError(200)
			.should.deep.equal({ 'error': { 'code': 'InvalidResponse', 'message': 'Unexpected HTTP status code: 200' } });
	});

});
