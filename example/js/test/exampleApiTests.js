import { createHttpClient } from '../exampleApi';
import { expect, should } from 'chai';

should();

function createFetchResponse(status, body) {
  const headers = {
    get: name => {
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

  it('missing fetch', () => {
    (() => { createHttpClient({}) })
      .should.throw('fetch must be a function.');
  });

  it('get widget returns 200', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        uri.should.equal('http://local.example.com/v1/widgets/xyzzy');
        request.method.should.equal('GET');
        expect(request.headers).to.be.empty;
        expect(request.body).to.not.exist;
        return createFetchResponse(200, { id: 'xyzzy', name: 'Xyzzy' });
      }
    }).getWidget({ id: 'xyzzy' }).then(result => {
      result.value.should.deep.equal({
        widget: { id: 'xyzzy', name: 'Xyzzy' },
      });
    });
  });

  it('get widget with etag returns 304', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        uri.should.equal('http://local.example.com/v1/widgets/xyzzy');
        request.method.should.equal('GET');
        expect(request.headers).to.be.ok;
        expect(request.headers['If-None-Match']).to.equal('"foo"');
        expect(request.body).to.not.exist;
        return createFetchResponse(304);
      }
    }).getWidget({ id: 'xyzzy', ifNoneMatch: '"foo"' }).then(result => {
      result.value.notModified.should.be.true;
    });
  });

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

  it('get widgets returns 201', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        return createFetchResponse(201, {});
      }
    }).getWidgets({}).then(result => {
      result.error.should.deep.equal({
        code: 'invalidResponse',
        message: 'Unexpected HTTP status code: 201'
      })
    });
  });

  it('get widgets returns 202', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        return createFetchResponse(202, {});
      }
    }).getWidgets({}).then(result => {
      result.value.should.deep.equal({
        job: {}
      });
    });
  });

  it('get widgets returns 204', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        return createFetchResponse(204);
      }
    }).getWidgets({}).then(result => {
      result.error.should.deep.equal({
        code: 'invalidResponse',
        message: 'Unexpected HTTP status code: 204'
      })
    });
  });

  it('get widgets returns 404', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        return createFetchResponse(404, '<html><body><h1>Not Found</hi></body></html>');
      }
    }).getWidgets({}).then(result => {
      result.error.should.deep.equal({
        code: 'notFound',
        message: 'HTTP client error: 404'
      })
    });
  });

  it('get widgets returns 500', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        return createFetchResponse(500, '<html><body><h1>Error</hi></body></html>');
      }
    }).getWidgets({}).then(result => {
      result.error.should.deep.equal({
        code: 'internalError',
        message: 'HTTP server error: 500'
      })
    });
  });

  it('get widgets returns 202 with bad data', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        return createFetchResponse(202, '<html><body><h1>Uh oh!</hi></body></html>');
      }
    }).getWidgets({}).then(result => {
      result.error.should.deep.equal({
        code: 'invalidResponse',
        message: 'Unexpected HTTP status code: 202'
      })
    });
  });

  it('get widgets with parameters', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        uri.should.equal('http://local.example.com/v1/widgets?q=x%C3%BDz&limit=10&sort=name&desc=true');
        request.method.should.equal('GET');
        expect(request.headers).to.not.exist;
        expect(request.body).to.not.exist;
        return createFetchResponse(200, {});
      }
    }).getWidgets({
      query: 'xýz',
      limit: 10,
      sort: 'name',
      desc: true
    }).then(result => {
      result.value.should.deep.equal({});
    });
  });

  it('get widgets with base URI without trailing slash', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        uri.should.equal('http://local.example.org/v1/widgets');
        return createFetchResponse(200, {});
      },
      baseUri: 'http://local.example.org/v1'
    }).getWidgets({});
  });

  it('get widgets with base URI with trailing slash', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        uri.should.equal('http://local.example.org/v1/widgets');
        return createFetchResponse(200, {});
      },
      baseUri: 'http://local.example.org/v1/'
    }).getWidgets({});
  });

  it('create widget returns 201', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        uri.should.equal('http://local.example.com/v1/widgets');
        request.method.should.equal('POST');
        request.headers.should.deep.equal({
          'Content-Type': 'application/json'
        });
        request.body.should.deep.equal(JSON.stringify({
          name: 'xyzzy'
        }));
        return createFetchResponse(201, {
          id: 'new',
          name: 'xyzzy'
        });
      }
    }).createWidget({
      widget: {
        name: 'xyzzy'
      }
    }).then(result => {
      result.value.should.deep.equal({
        widget: {
          id: 'new',
          name: 'xyzzy'
        }
      });
    });
  });

  it('get info returns 200', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        uri.should.equal('http://local.example.com/v1/');
        request.method.should.equal('GET');
        expect(request.headers).to.not.exist;
        expect(request.body).to.not.exist;
        return createFetchResponse(200, {});
      }
    }).getInfo({}).then(result => {
      result.value.should.deep.equal({});
    });
  });

  it('get info with base URI without trailing slash', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        uri.should.equal('http://local.example.org/v1/');
        return createFetchResponse(200, {});
      },
      baseUri: 'http://local.example.org/v1'
    }).getInfo({});
  });

  it('get info with base URI with trailing slash', () => {
    return createHttpClient({
      fetch: (uri, request) => {
        uri.should.equal('http://local.example.org/v1/');
        return createFetchResponse(200, {});
      },
      baseUri: 'http://local.example.org/v1/'
    }).getInfo({});
  });

});
