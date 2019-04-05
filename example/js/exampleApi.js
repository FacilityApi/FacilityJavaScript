// DO NOT EDIT: generated by fsdgenjs
/* eslint-disable */
'use strict';

import { HttpClientUtility } from 'facility-core';

/** Provides access to ExampleApi over HTTP via fetch. */
export function createHttpClient({ fetch, baseUri }) {
  return new ExampleApiHttpClient(fetch, baseUri);
}

const { fetchResponse, createResponseError, createRequiredRequestFieldError } = HttpClientUtility;

class ExampleApiHttpClient {
  constructor(fetch, baseUri) {
    if (typeof fetch !== 'function') {
      throw new TypeError('fetch must be a function.');
    }
    if (typeof baseUri === 'undefined') {
      baseUri = 'http://local.example.com/v1';
    }
    if (/[^\/]$/.test(baseUri)) {
      baseUri += '/';
    }
    this._fetch = fetch;
    this._baseUri = baseUri;
  }

  /** Gets widgets. */
  getWidgets(request) {
    let uri = 'widgets';
    const query = [];
    request.query == null || query.push('q=' + encodeURIComponent(request.query));
    request.limit == null || query.push('limit=' + request.limit.toString());
    request.sort == null || query.push('sort=' + request.sort);
    request.desc == null || query.push('desc=' + request.desc.toString());
    request.maxWeight == null || query.push('maxWeight=' + encodeURIComponent(request.maxWeight.toString()));
    request.minPrice == null || query.push('minPrice=' + request.minPrice.toString());
    if (query.length) {
      uri = uri + '?' + query.join('&');
    }
    const fetchRequest = {
      method: 'GET',
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (result.json) {
          if (status === 200) {
            value = result.json;
          }
          else if (status === 202) {
            value = { job: result.json };
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  /** Creates a new widget. */
  createWidget(request) {
    const uri = 'widgets';
    const fetchRequest = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request.widget)
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (result.json) {
          if (status === 201) {
            value = { widget: result.json };
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  /** Gets the specified widget. */
  getWidget(request) {
    const uriPartId = request.id != null && encodeURIComponent(request.id);
    if (!uriPartId) {
      return Promise.resolve(createRequiredRequestFieldError('id'));
    }
    const uri = `widgets/${uriPartId}`;
    const fetchRequest = {
      method: 'GET',
      headers: {},
    };
    if (request.ifNoneMatch != null) {
      fetchRequest.headers['If-None-Match'] = request.ifNoneMatch;
    }
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (result.json) {
          if (status === 200) {
            value = { widget: result.json };
          }
          else if (status === 304) {
            value = { notModified: true };
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        let headerValue;
        headerValue = result.response.headers.get('eTag');
        if (headerValue != null) {
          value.eTag = headerValue;
        }
        return { value: value };
      });
  }

  /** Deletes the specified widget. */
  deleteWidget(request) {
    const uriPartId = request.id != null && encodeURIComponent(request.id);
    if (!uriPartId) {
      return Promise.resolve(createRequiredRequestFieldError('id'));
    }
    const uri = `widgets/${uriPartId}`;
    const fetchRequest = {
      method: 'DELETE',
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (result.json) {
          if (status === 204) {
            value = {};
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  /** Edits widget. */
  editWidget(request) {
    const uriPartId = request.id != null && encodeURIComponent(request.id);
    if (!uriPartId) {
      return Promise.resolve(createRequiredRequestFieldError('id'));
    }
    const uri = `widgets/${uriPartId}`;
    const fetchRequest = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        ops: request.ops,
        weight: request.weight
      })
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (result.json) {
          if (status === 200) {
            value = { widget: result.json };
          }
          else if (status === 202) {
            value = { job: result.json };
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  /** Gets the specified widgets. */
  getWidgetBatch(request) {
    const uri = 'widgets/get';
    const fetchRequest = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request.ids)
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (result.json) {
          if (status === 200) {
            value = { results: result.json };
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  /**
   * Gets the widget weight.
   * @deprecated
   */
  getWidgetWeight(request) {
    const uriPartId = request.id != null && encodeURIComponent(request.id);
    if (!uriPartId) {
      return Promise.resolve(createRequiredRequestFieldError('id'));
    }
    const uri = `widgets/${uriPartId}/weight`;
    const fetchRequest = {
      method: 'GET',
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (result.json) {
          if (status === 200) {
            value = result.json;
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  /** Gets a widget preference. */
  getPreference(request) {
    const uriPartKey = request.key != null && encodeURIComponent(request.key);
    if (!uriPartKey) {
      return Promise.resolve(createRequiredRequestFieldError('key'));
    }
    const uri = `prefs/${uriPartKey}`;
    const fetchRequest = {
      method: 'GET',
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (result.json) {
          if (status === 200) {
            value = { value: result.json };
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  /** Sets a widget preference. */
  setPreference(request) {
    const uriPartKey = request.key != null && encodeURIComponent(request.key);
    if (!uriPartKey) {
      return Promise.resolve(createRequiredRequestFieldError('key'));
    }
    const uri = `prefs/${uriPartKey}`;
    const fetchRequest = {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request.value)
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (result.json) {
          if (status === 200) {
            value = { value: result.json };
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  /** Gets service info. */
  getInfo(request) {
    const uri = '';
    const fetchRequest = {
      method: 'GET',
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (result.json) {
          if (status === 200) {
            value = result.json;
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  /** Demonstrates the default HTTP behavior. */
  notRestful(request) {
    const uri = 'notRestful';
    const fetchRequest = {
      method: 'POST',
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (result.json) {
          if (status === 200) {
            value = {};
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  kitchen(request) {
    const uri = 'kitchen';
    const fetchRequest = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (result.json) {
          if (status === 200) {
            value = {};
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }
}
