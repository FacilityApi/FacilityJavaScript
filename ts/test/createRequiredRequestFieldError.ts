import { HttpClientUtility } from '../src/facilityCore';
import { expect, should } from 'chai';

should();

const createRequiredRequestFieldError = HttpClientUtility.createRequiredRequestFieldError;

describe('createRequiredRequestFieldError', () => {

	it('should return error result', () => {
		createRequiredRequestFieldError('id')
			.should.deep.equal({ error: { code: 'InvalidRequest', message: '\'id\' is required.' } });
	});

});
