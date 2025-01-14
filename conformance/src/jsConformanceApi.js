// DO NOT EDIT: generated by fsdgenjs
/* eslint-disable */
'use strict';

import { HttpClientUtility } from 'facility-core';

/** Provides access to JsConformanceApi over HTTP via fetch. */
export function createHttpClient({ fetch, baseUri }) {
  return new JsConformanceApiHttpClient(fetch, baseUri);
}

const { fetchResponse, createResponseError, createRequiredRequestFieldError } = HttpClientUtility;

function parseBoolean(value) {
  if (typeof value === 'string') {
    const lowerValue = value.toLowerCase();
    if (lowerValue === 'true') {
      return true;
    }
    if (lowerValue === 'false') {
      return false;
    }
  }
  return undefined;
}

class JsConformanceApiHttpClient {
  constructor(fetch, baseUri) {
    if (typeof fetch !== 'function') {
      throw new TypeError('fetch must be a function.');
    }
    if (typeof baseUri === 'undefined') {
      baseUri = '';
    }
    if (/[^\/]$/.test(baseUri)) {
      baseUri += '/';
    }
    this._fetch = fetch;
    this._baseUri = baseUri;
  }

  /** Gets API information. */
  getApiInfo(request, context) {
    const uri = '';
    const fetchRequest = {
      method: 'GET',
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          if (result.json) {
            value = result.json;
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  /** Gets widgets. */
  getWidgets(request, context) {
    let uri = 'widgets';
    const query = [];
    request.query == null || query.push('q=' + encodeURIComponent(request.query));
    if (query.length) {
      uri = uri + '?' + query.join('&');
    }
    const fetchRequest = {
      method: 'GET',
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          if (result.json) {
            value = result.json;
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  /** Creates a new widget. */
  createWidget(request, context) {
    const uri = 'widgets';
    if (!request.widget) {
      return Promise.resolve(createRequiredRequestFieldError('widget'));
    }
    const fetchRequest = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request.widget)
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 201) {
          if (result.json) {
            value = { widget: result.json };
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        let headerValue;
        headerValue = result.response.headers.get('Location');
        if (headerValue != null) {
          value.url = headerValue;
        }
        headerValue = result.response.headers.get('eTag');
        if (headerValue != null) {
          value.eTag = headerValue;
        }
        return { value: value };
      });
  }

  /** Gets the specified widget. */
  getWidget(request, context) {
    const uriPartId = request.id != null && request.id.toString();
    if (!uriPartId) {
      return Promise.resolve(createRequiredRequestFieldError('id'));
    }
    const uri = `widgets/${uriPartId}`;
    const fetchRequest = {
      method: 'GET',
      headers: {},
    };
    if (request.ifNotETag != null) {
      fetchRequest.headers['If-None-Match'] = request.ifNotETag;
    }
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          if (result.json) {
            value = { widget: result.json };
          }
        }
        else if (status === 304) {
          value = { notModified: true };
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
  deleteWidget(request, context) {
    const uriPartId = request.id != null && request.id.toString();
    if (!uriPartId) {
      return Promise.resolve(createRequiredRequestFieldError('id'));
    }
    const uri = `widgets/${uriPartId}`;
    const fetchRequest = {
      method: 'DELETE',
      headers: {},
    };
    if (request.ifETag != null) {
      fetchRequest.headers['If-Match'] = request.ifETag;
    }
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 204) {
          value = {};
        }
        else if (status === 404) {
          value = { notFound: true };
        }
        else if (status === 409) {
          value = { conflict: true };
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  /** Gets the specified widgets. */
  getWidgetBatch(request, context) {
    const uri = 'widgets/get';
    if (!request.ids) {
      return Promise.resolve(createRequiredRequestFieldError('ids'));
    }
    const fetchRequest = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request.ids)
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          if (result.json) {
            value = { results: result.json };
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  mirrorFields(request, context) {
    const uri = 'mirrorFields';
    const fetchRequest = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          if (result.json) {
            value = result.json;
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  checkQuery(request, context) {
    let uri = 'checkQuery';
    const query = [];
    request.string == null || query.push('string=' + encodeURIComponent(request.string));
    request.boolean == null || query.push('boolean=' + request.boolean.toString());
    request.float == null || query.push('float=' + encodeURIComponent(request.float.toString()));
    request.double == null || query.push('double=' + encodeURIComponent(request.double.toString()));
    request.int32 == null || query.push('int32=' + request.int32.toString());
    request.int64 == null || query.push('int64=' + request.int64.toString());
    request.decimal == null || query.push('decimal=' + request.decimal.toString());
    request.enum == null || query.push('enum=' + request.enum);
    request.datetime == null || query.push('datetime=' + encodeURIComponent(request.datetime));
    if (query.length) {
      uri = uri + '?' + query.join('&');
    }
    const fetchRequest = {
      method: 'GET',
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          value = {};
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  checkPath(request, context) {
    const uriPartString = request.string != null && encodeURIComponent(request.string);
    if (!uriPartString) {
      return Promise.resolve(createRequiredRequestFieldError('string'));
    }
    const uriPartBoolean = request.boolean != null && request.boolean.toString();
    if (!uriPartBoolean) {
      return Promise.resolve(createRequiredRequestFieldError('boolean'));
    }
    const uriPartFloat = request.float != null && encodeURIComponent(request.float.toString());
    if (!uriPartFloat) {
      return Promise.resolve(createRequiredRequestFieldError('float'));
    }
    const uriPartDouble = request.double != null && encodeURIComponent(request.double.toString());
    if (!uriPartDouble) {
      return Promise.resolve(createRequiredRequestFieldError('double'));
    }
    const uriPartInt32 = request.int32 != null && request.int32.toString();
    if (!uriPartInt32) {
      return Promise.resolve(createRequiredRequestFieldError('int32'));
    }
    const uriPartInt64 = request.int64 != null && request.int64.toString();
    if (!uriPartInt64) {
      return Promise.resolve(createRequiredRequestFieldError('int64'));
    }
    const uriPartDecimal = request.decimal != null && request.decimal.toString();
    if (!uriPartDecimal) {
      return Promise.resolve(createRequiredRequestFieldError('decimal'));
    }
    const uriPartEnum = request.enum != null && request.enum;
    if (!uriPartEnum) {
      return Promise.resolve(createRequiredRequestFieldError('enum'));
    }
    const uriPartDatetime = request.datetime != null && encodeURIComponent(request.datetime);
    if (!uriPartDatetime) {
      return Promise.resolve(createRequiredRequestFieldError('datetime'));
    }
    const uri = `checkPath/${uriPartString}/${uriPartBoolean}/${uriPartFloat}/${uriPartDouble}/${uriPartInt32}/${uriPartInt64}/${uriPartDecimal}/${uriPartEnum}/${uriPartDatetime}`;
    const fetchRequest = {
      method: 'GET',
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          value = {};
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  mirrorHeaders(request, context) {
    const uri = 'mirrorHeaders';
    const fetchRequest = {
      method: 'GET',
      headers: {},
    };
    if (request.string != null) {
      fetchRequest.headers['string'] = request.string;
    }
    if (request.boolean != null) {
      fetchRequest.headers['boolean'] = request.boolean.toString();
    }
    if (request.float != null) {
      fetchRequest.headers['float'] = request.float.toString();
    }
    if (request.double != null) {
      fetchRequest.headers['double'] = request.double.toString();
    }
    if (request.int32 != null) {
      fetchRequest.headers['int32'] = request.int32.toString();
    }
    if (request.int64 != null) {
      fetchRequest.headers['int64'] = request.int64.toString();
    }
    if (request.decimal != null) {
      fetchRequest.headers['decimal'] = request.decimal.toString();
    }
    if (request.enum != null) {
      fetchRequest.headers['enum'] = request.enum;
    }
    if (request.datetime != null) {
      fetchRequest.headers['datetime'] = request.datetime;
    }
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          value = {};
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        let headerValue;
        headerValue = result.response.headers.get('string');
        if (headerValue != null) {
          value.string = headerValue;
        }
        headerValue = result.response.headers.get('boolean');
        if (headerValue != null) {
          value.boolean = parseBoolean(headerValue);
        }
        headerValue = result.response.headers.get('float');
        if (headerValue != null) {
          value.float = parseFloat(headerValue);
        }
        headerValue = result.response.headers.get('double');
        if (headerValue != null) {
          value.double = parseFloat(headerValue);
        }
        headerValue = result.response.headers.get('int32');
        if (headerValue != null) {
          value.int32 = parseInt(headerValue, 10);
        }
        headerValue = result.response.headers.get('int64');
        if (headerValue != null) {
          value.int64 = parseInt(headerValue, 10);
        }
        headerValue = result.response.headers.get('decimal');
        if (headerValue != null) {
          value.decimal = parseFloat(headerValue);
        }
        headerValue = result.response.headers.get('enum');
        if (headerValue != null) {
          value.enum = headerValue;
        }
        headerValue = result.response.headers.get('datetime');
        if (headerValue != null) {
          value.datetime = headerValue;
        }
        return { value: value };
      });
  }

  mixed(request, context) {
    const uriPartPath = request.path != null && encodeURIComponent(request.path);
    if (!uriPartPath) {
      return Promise.resolve(createRequiredRequestFieldError('path'));
    }
    let uri = `mixed/${uriPartPath}`;
    const query = [];
    request.query == null || query.push('query=' + encodeURIComponent(request.query));
    if (query.length) {
      uri = uri + '?' + query.join('&');
    }
    const fetchRequest = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        normal: request.normal
      })
    };
    if (request.header != null) {
      fetchRequest.headers['header'] = request.header;
    }
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          if (result.json) {
            value = result.json;
          }
        }
        else if (status === 202) {
          if (result.json) {
            value = { body: result.json };
          }
        }
        else if (status === 204) {
          value = { empty: true };
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        let headerValue;
        headerValue = result.response.headers.get('header');
        if (headerValue != null) {
          value.header = headerValue;
        }
        return { value: value };
      });
  }

  required(request, context) {
    let uri = 'required';
    const query = [];
    request.query == null || query.push('query=' + encodeURIComponent(request.query));
    if (query.length) {
      uri = uri + '?' + query.join('&');
    }
    const fetchRequest = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        normal: request.normal,
        widget: request.widget,
        widgets: request.widgets,
        widgetMatrix: request.widgetMatrix,
        widgetResult: request.widgetResult,
        widgetResults: request.widgetResults,
        widgetMap: request.widgetMap,
        hasWidget: request.hasWidget,
        point: request.point
      })
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          if (result.json) {
            value = result.json;
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }

  mirrorBytes(request, context) {
    const uri = 'mirrorBytes';
    if (!request.content) {
      return Promise.resolve(createRequiredRequestFieldError('content'));
    }
    const fetchRequest = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request.content)
    };
    if (request.type != null) {
      fetchRequest.headers['Content-Type'] = request.type;
    }
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          if (result.json) {
            value = { content: result.json };
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        let headerValue;
        headerValue = result.response.headers.get('Content-Type');
        if (headerValue != null) {
          value.type = headerValue;
        }
        return { value: value };
      });
  }

  mirrorText(request, context) {
    const uri = 'mirrorText';
    if (!request.content) {
      return Promise.resolve(createRequiredRequestFieldError('content'));
    }
    const fetchRequest = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request.content)
    };
    if (request.type != null) {
      fetchRequest.headers['Content-Type'] = request.type;
    }
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          if (result.json) {
            value = { content: result.json };
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        let headerValue;
        headerValue = result.response.headers.get('Content-Type');
        if (headerValue != null) {
          value.type = headerValue;
        }
        return { value: value };
      });
  }

  bodyTypes(request, context) {
    const uri = 'bodyTypes';
    if (!request.content) {
      return Promise.resolve(createRequiredRequestFieldError('content'));
    }
    const fetchRequest = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request.content)
    };
    return fetchResponse(this._fetch, this._baseUri + uri, fetchRequest, context)
      .then(result => {
        const status = result.response.status;
        let value = null;
        if (status === 200) {
          if (result.json) {
            value = { content: result.json };
          }
        }
        if (!value) {
          return createResponseError(status, result.json);
        }
        return { value: value };
      });
  }
}
