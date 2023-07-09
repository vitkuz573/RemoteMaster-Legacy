(() => {
  'use strict';
  var e = {};
  e.g = (function () {
    if ('object' == typeof globalThis) return globalThis;
    try {
      return this || new Function('return this')();
    } catch (e) {
      if ('object' == typeof window) return window;
    }
  })();
  const t = [0, 2e3, 1e4, 3e4, null];
  class n {
    constructor(e) {
      this._retryDelays = void 0 !== e ? [...e, null] : t;
    }
    nextRetryDelayInMilliseconds(e) {
      return this._retryDelays[e.previousRetryCount];
    }
  }
  class o {}
  (o.Authorization = 'Authorization'), (o.Cookie = 'Cookie');
  class r {
    constructor(e, t, n) {
      (this.statusCode = e), (this.statusText = t), (this.content = n);
    }
  }
  class i {
    get(e, t) {
      return this.send({ ...t, method: 'GET', url: e });
    }
    post(e, t) {
      return this.send({ ...t, method: 'POST', url: e });
    }
    delete(e, t) {
      return this.send({ ...t, method: 'DELETE', url: e });
    }
    getCookieString(e) {
      return '';
    }
  }
  class s extends i {
    constructor(e, t) {
      super(), (this._innerClient = e), (this._accessTokenFactory = t);
    }
    async send(e) {
      let t = !0;
      this._accessTokenFactory &&
        (!this._accessToken || (e.url && e.url.indexOf('/negotiate?') > 0)) &&
        ((t = !1), (this._accessToken = await this._accessTokenFactory())),
        this._setAuthorizationHeader(e);
      const n = await this._innerClient.send(e);
      return t && 401 === n.statusCode && this._accessTokenFactory
        ? ((this._accessToken = await this._accessTokenFactory()),
          this._setAuthorizationHeader(e),
          await this._innerClient.send(e))
        : n;
    }
    _setAuthorizationHeader(e) {
      e.headers || (e.headers = {}),
        this._accessToken
          ? (e.headers[o.Authorization] = `Bearer ${this._accessToken}`)
          : this._accessTokenFactory &&
            e.headers[o.Authorization] &&
            delete e.headers[o.Authorization];
    }
    getCookieString(e) {
      return this._innerClient.getCookieString(e);
    }
  }
  class a extends Error {
    constructor(e, t) {
      const n = new.target.prototype;
      super(`${e}: Status code '${t}'`),
        (this.statusCode = t),
        (this.__proto__ = n);
    }
  }
  class c extends Error {
    constructor(e = 'A timeout occurred.') {
      const t = new.target.prototype;
      super(e), (this.__proto__ = t);
    }
  }
  class h extends Error {
    constructor(e = 'An abort occurred.') {
      const t = new.target.prototype;
      super(e), (this.__proto__ = t);
    }
  }
  class l extends Error {
    constructor(e, t) {
      const n = new.target.prototype;
      super(e),
        (this.transport = t),
        (this.errorType = 'UnsupportedTransportError'),
        (this.__proto__ = n);
    }
  }
  class u extends Error {
    constructor(e, t) {
      const n = new.target.prototype;
      super(e),
        (this.transport = t),
        (this.errorType = 'DisabledTransportError'),
        (this.__proto__ = n);
    }
  }
  class d extends Error {
    constructor(e, t) {
      const n = new.target.prototype;
      super(e),
        (this.transport = t),
        (this.errorType = 'FailedToStartTransportError'),
        (this.__proto__ = n);
    }
  }
  class p extends Error {
    constructor(e) {
      const t = new.target.prototype;
      super(e),
        (this.errorType = 'FailedToNegotiateWithServerError'),
        (this.__proto__ = t);
    }
  }
  class g extends Error {
    constructor(e, t) {
      const n = new.target.prototype;
      super(e), (this.innerErrors = t), (this.__proto__ = n);
    }
  }
  var f;
  !(function (e) {
    (e[(e.Trace = 0)] = 'Trace'),
      (e[(e.Debug = 1)] = 'Debug'),
      (e[(e.Information = 2)] = 'Information'),
      (e[(e.Warning = 3)] = 'Warning'),
      (e[(e.Error = 4)] = 'Error'),
      (e[(e.Critical = 5)] = 'Critical'),
      (e[(e.None = 6)] = 'None');
  })(f || (f = {}));
  class _ {
    constructor() {}
    log(e, t) {}
  }
  _.instance = new _();
  const w = '7.0.7';
  class y {
    static isRequired(e, t) {
      if (null == e) throw new Error(`The '${t}' argument is required.`);
    }
    static isNotEmpty(e, t) {
      if (!e || e.match(/^\s*$/))
        throw new Error(`The '${t}' argument should not be empty.`);
    }
    static isIn(e, t, n) {
      if (!(e in t)) throw new Error(`Unknown ${n} value: ${e}.`);
    }
  }
  class v {
    static get isBrowser() {
      return 'object' == typeof window && 'object' == typeof window.document;
    }
    static get isWebWorker() {
      return 'object' == typeof self && 'importScripts' in self;
    }
    static get isReactNative() {
      return 'object' == typeof window && void 0 === window.document;
    }
    static get isNode() {
      return !this.isBrowser && !this.isWebWorker && !this.isReactNative;
    }
  }
  function m(e, t) {
    let n = '';
    return (
      b(e)
        ? ((n = `Binary data of length ${e.byteLength}`),
          t &&
            (n += `. Content: '${(function (e) {
              const t = new Uint8Array(e);
              let n = '';
              return (
                t.forEach((e) => {
                  n += `0x${e < 16 ? '0' : ''}${e.toString(16)} `;
                }),
                n.substr(0, n.length - 1)
              );
            })(e)}'`))
        : 'string' == typeof e &&
          ((n = `String data of length ${e.length}`),
          t && (n += `. Content: '${e}'`)),
      n
    );
  }
  function b(e) {
    return (
      e &&
      'undefined' != typeof ArrayBuffer &&
      (e instanceof ArrayBuffer ||
        (e.constructor && 'ArrayBuffer' === e.constructor.name))
    );
  }
  async function S(e, t, n, o, r, i) {
    const s = {},
      [a, c] = E();
    (s[a] = c),
      e.log(
        f.Trace,
        `(${t} transport) sending data. ${m(r, i.logMessageContent)}.`
      );
    const h = b(r) ? 'arraybuffer' : 'text',
      l = await n.post(o, {
        content: r,
        headers: { ...s, ...i.headers },
        responseType: h,
        timeout: i.timeout,
        withCredentials: i.withCredentials,
      });
    e.log(
      f.Trace,
      `(${t} transport) request complete. Response status: ${l.statusCode}.`
    );
  }
  class C {
    constructor(e, t) {
      (this._subject = e), (this._observer = t);
    }
    dispose() {
      const e = this._subject.observers.indexOf(this._observer);
      e > -1 && this._subject.observers.splice(e, 1),
        0 === this._subject.observers.length &&
          this._subject.cancelCallback &&
          this._subject.cancelCallback().catch((e) => {});
    }
  }
  class I {
    constructor(e) {
      (this._minLevel = e), (this.out = console);
    }
    log(e, t) {
      if (e >= this._minLevel) {
        const n = `[${new Date().toISOString()}] ${f[e]}: ${t}`;
        switch (e) {
          case f.Critical:
          case f.Error:
            this.out.error(n);
            break;
          case f.Warning:
            this.out.warn(n);
            break;
          case f.Information:
            this.out.info(n);
            break;
          default:
            this.out.log(n);
        }
      }
    }
  }
  function E() {
    let e = 'X-SignalR-User-Agent';
    return (
      v.isNode && (e = 'User-Agent'),
      [e, k(w, T(), v.isNode ? 'NodeJS' : 'Browser', U())]
    );
  }
  function k(e, t, n, o) {
    let r = 'Microsoft SignalR/';
    const i = e.split('.');
    return (
      (r += `${i[0]}.${i[1]}`),
      (r += ` (${e}; `),
      (r += t && '' !== t ? `${t}; ` : 'Unknown OS; '),
      (r += `${n}`),
      (r += o ? `; ${o}` : '; Unknown Runtime Version'),
      (r += ')'),
      r
    );
  }
  function T() {
    if (!v.isNode) return '';
    switch (process.platform) {
      case 'win32':
        return 'Windows NT';
      case 'darwin':
        return 'macOS';
      case 'linux':
        return 'Linux';
      default:
        return process.platform;
    }
  }
  function U() {
    if (v.isNode) return process.versions.node;
  }
  function x(e) {
    return e.stack ? e.stack : e.message ? e.message : `${e}`;
  }
  class P extends i {
    constructor(t) {
      if ((super(), (this._logger = t), 'undefined' == typeof fetch)) {
        const e = require;
        (this._jar = new (e('tough-cookie').CookieJar)()),
          (this._fetchType = e('node-fetch')),
          (this._fetchType = e('fetch-cookie')(this._fetchType, this._jar));
      } else
        this._fetchType = fetch.bind(
          (function () {
            if ('undefined' != typeof globalThis) return globalThis;
            if ('undefined' != typeof self) return self;
            if ('undefined' != typeof window) return window;
            if (void 0 !== e.g) return e.g;
            throw new Error('could not find global');
          })()
        );
      if ('undefined' == typeof AbortController) {
        const e = require;
        this._abortControllerType = e('abort-controller');
      } else this._abortControllerType = AbortController;
    }
    async send(e) {
      if (e.abortSignal && e.abortSignal.aborted) throw new h();
      if (!e.method) throw new Error('No method defined.');
      if (!e.url) throw new Error('No url defined.');
      const t = new this._abortControllerType();
      let n;
      e.abortSignal &&
        (e.abortSignal.onabort = () => {
          t.abort(), (n = new h());
        });
      let o,
        i = null;
      if (e.timeout) {
        const o = e.timeout;
        i = setTimeout(() => {
          t.abort(),
            this._logger.log(f.Warning, 'Timeout from HTTP request.'),
            (n = new c());
        }, o);
      }
      '' === e.content && (e.content = void 0),
        e.content &&
          ((e.headers = e.headers || {}),
          b(e.content)
            ? (e.headers['Content-Type'] = 'application/octet-stream')
            : (e.headers['Content-Type'] = 'text/plain;charset=UTF-8'));
      try {
        o = await this._fetchType(e.url, {
          body: e.content,
          cache: 'no-cache',
          credentials: !0 === e.withCredentials ? 'include' : 'same-origin',
          headers: { 'X-Requested-With': 'XMLHttpRequest', ...e.headers },
          method: e.method,
          mode: 'cors',
          redirect: 'follow',
          signal: t.signal,
        });
      } catch (e) {
        if (n) throw n;
        throw (
          (this._logger.log(f.Warning, `Error from HTTP request. ${e}.`), e)
        );
      } finally {
        i && clearTimeout(i), e.abortSignal && (e.abortSignal.onabort = null);
      }
      if (!o.ok) {
        const e = await D(o, 'text');
        throw new a(e || o.statusText, o.status);
      }
      const s = D(o, e.responseType),
        l = await s;
      return new r(o.status, o.statusText, l);
    }
    getCookieString(e) {
      let t = '';
      return (
        v.isNode &&
          this._jar &&
          this._jar.getCookies(e, (e, n) => (t = n.join('; '))),
        t
      );
    }
  }
  function D(e, t) {
    let n;
    switch (t) {
      case 'arraybuffer':
        n = e.arrayBuffer();
        break;
      case 'text':
      default:
        n = e.text();
        break;
      case 'blob':
      case 'document':
      case 'json':
        throw new Error(`${t} is not supported.`);
    }
    return n;
  }
  class $ extends i {
    constructor(e) {
      super(), (this._logger = e);
    }
    send(e) {
      return e.abortSignal && e.abortSignal.aborted
        ? Promise.reject(new h())
        : e.method
        ? e.url
          ? new Promise((t, n) => {
              const o = new XMLHttpRequest();
              o.open(e.method, e.url, !0),
                (o.withCredentials =
                  void 0 === e.withCredentials || e.withCredentials),
                o.setRequestHeader('X-Requested-With', 'XMLHttpRequest'),
                '' === e.content && (e.content = void 0),
                e.content &&
                  (b(e.content)
                    ? o.setRequestHeader(
                        'Content-Type',
                        'application/octet-stream'
                      )
                    : o.setRequestHeader(
                        'Content-Type',
                        'text/plain;charset=UTF-8'
                      ));
              const i = e.headers;
              i &&
                Object.keys(i).forEach((e) => {
                  o.setRequestHeader(e, i[e]);
                }),
                e.responseType && (o.responseType = e.responseType),
                e.abortSignal &&
                  (e.abortSignal.onabort = () => {
                    o.abort(), n(new h());
                  }),
                e.timeout && (o.timeout = e.timeout),
                (o.onload = () => {
                  e.abortSignal && (e.abortSignal.onabort = null),
                    o.status >= 200 && o.status < 300
                      ? t(
                          new r(
                            o.status,
                            o.statusText,
                            o.response || o.responseText
                          )
                        )
                      : n(
                          new a(
                            o.response || o.responseText || o.statusText,
                            o.status
                          )
                        );
                }),
                (o.onerror = () => {
                  this._logger.log(
                    f.Warning,
                    `Error from HTTP request. ${o.status}: ${o.statusText}.`
                  ),
                    n(new a(o.statusText, o.status));
                }),
                (o.ontimeout = () => {
                  this._logger.log(f.Warning, 'Timeout from HTTP request.'),
                    n(new c());
                }),
                o.send(e.content);
            })
          : Promise.reject(new Error('No url defined.'))
        : Promise.reject(new Error('No method defined.'));
    }
  }
  class R extends i {
    constructor(e) {
      if ((super(), 'undefined' != typeof fetch || v.isNode))
        this._httpClient = new P(e);
      else {
        if ('undefined' == typeof XMLHttpRequest)
          throw new Error('No usable HttpClient found.');
        this._httpClient = new $(e);
      }
    }
    send(e) {
      return e.abortSignal && e.abortSignal.aborted
        ? Promise.reject(new h())
        : e.method
        ? e.url
          ? this._httpClient.send(e)
          : Promise.reject(new Error('No url defined.'))
        : Promise.reject(new Error('No method defined.'));
    }
    getCookieString(e) {
      return this._httpClient.getCookieString(e);
    }
  }
  var A, M, B, L;
  !(function (e) {
    (e[(e.None = 0)] = 'None'),
      (e[(e.WebSockets = 1)] = 'WebSockets'),
      (e[(e.ServerSentEvents = 2)] = 'ServerSentEvents'),
      (e[(e.LongPolling = 4)] = 'LongPolling');
  })(A || (A = {})),
    (function (e) {
      (e[(e.Text = 1)] = 'Text'), (e[(e.Binary = 2)] = 'Binary');
    })(M || (M = {}));
  class W {
    constructor() {
      (this._isAborted = !1), (this.onabort = null);
    }
    abort() {
      this._isAborted ||
        ((this._isAborted = !0), this.onabort && this.onabort());
    }
    get signal() {
      return this;
    }
    get aborted() {
      return this._isAborted;
    }
  }
  class H {
    constructor(e, t, n) {
      (this._httpClient = e),
        (this._logger = t),
        (this._pollAbort = new W()),
        (this._options = n),
        (this._running = !1),
        (this.onreceive = null),
        (this.onclose = null);
    }
    get pollAborted() {
      return this._pollAbort.aborted;
    }
    async connect(e, t) {
      if (
        (y.isRequired(e, 'url'),
        y.isRequired(t, 'transferFormat'),
        y.isIn(t, M, 'transferFormat'),
        (this._url = e),
        this._logger.log(f.Trace, '(LongPolling transport) Connecting.'),
        t === M.Binary &&
          'undefined' != typeof XMLHttpRequest &&
          'string' != typeof new XMLHttpRequest().responseType)
      )
        throw new Error(
          'Binary protocols over XmlHttpRequest not implementing advanced features are not supported.'
        );
      const [n, o] = E(),
        r = { [n]: o, ...this._options.headers },
        i = {
          abortSignal: this._pollAbort.signal,
          headers: r,
          timeout: 1e5,
          withCredentials: this._options.withCredentials,
        };
      t === M.Binary && (i.responseType = 'arraybuffer');
      const s = `${e}&_=${Date.now()}`;
      this._logger.log(f.Trace, `(LongPolling transport) polling: ${s}.`);
      const c = await this._httpClient.get(s, i);
      200 !== c.statusCode
        ? (this._logger.log(
            f.Error,
            `(LongPolling transport) Unexpected response code: ${c.statusCode}.`
          ),
          (this._closeError = new a(c.statusText || '', c.statusCode)),
          (this._running = !1))
        : (this._running = !0),
        (this._receiving = this._poll(this._url, i));
    }
    async _poll(e, t) {
      try {
        for (; this._running; )
          try {
            const n = `${e}&_=${Date.now()}`;
            this._logger.log(f.Trace, `(LongPolling transport) polling: ${n}.`);
            const o = await this._httpClient.get(n, t);
            204 === o.statusCode
              ? (this._logger.log(
                  f.Information,
                  '(LongPolling transport) Poll terminated by server.'
                ),
                (this._running = !1))
              : 200 !== o.statusCode
              ? (this._logger.log(
                  f.Error,
                  `(LongPolling transport) Unexpected response code: ${o.statusCode}.`
                ),
                (this._closeError = new a(o.statusText || '', o.statusCode)),
                (this._running = !1))
              : o.content
              ? (this._logger.log(
                  f.Trace,
                  `(LongPolling transport) data received. ${m(
                    o.content,
                    this._options.logMessageContent
                  )}.`
                ),
                this.onreceive && this.onreceive(o.content))
              : this._logger.log(
                  f.Trace,
                  '(LongPolling transport) Poll timed out, reissuing.'
                );
          } catch (e) {
            this._running
              ? e instanceof c
                ? this._logger.log(
                    f.Trace,
                    '(LongPolling transport) Poll timed out, reissuing.'
                  )
                : ((this._closeError = e), (this._running = !1))
              : this._logger.log(
                  f.Trace,
                  `(LongPolling transport) Poll errored after shutdown: ${e.message}`
                );
          }
      } finally {
        this._logger.log(f.Trace, '(LongPolling transport) Polling complete.'),
          this.pollAborted || this._raiseOnClose();
      }
    }
    async send(e) {
      return this._running
        ? S(
            this._logger,
            'LongPolling',
            this._httpClient,
            this._url,
            e,
            this._options
          )
        : Promise.reject(
            new Error('Cannot send until the transport is connected')
          );
    }
    async stop() {
      this._logger.log(f.Trace, '(LongPolling transport) Stopping polling.'),
        (this._running = !1),
        this._pollAbort.abort();
      try {
        await this._receiving,
          this._logger.log(
            f.Trace,
            `(LongPolling transport) sending DELETE request to ${this._url}.`
          );
        const e = {},
          [t, n] = E();
        e[t] = n;
        const o = {
          headers: { ...e, ...this._options.headers },
          timeout: this._options.timeout,
          withCredentials: this._options.withCredentials,
        };
        await this._httpClient.delete(this._url, o),
          this._logger.log(
            f.Trace,
            '(LongPolling transport) DELETE request sent.'
          );
      } finally {
        this._logger.log(f.Trace, '(LongPolling transport) Stop finished.'),
          this._raiseOnClose();
      }
    }
    _raiseOnClose() {
      if (this.onclose) {
        let e = '(LongPolling transport) Firing onclose event.';
        this._closeError && (e += ' Error: ' + this._closeError),
          this._logger.log(f.Trace, e),
          this.onclose(this._closeError);
      }
    }
  }
  class N {
    constructor(e, t, n, o) {
      (this._httpClient = e),
        (this._accessToken = t),
        (this._logger = n),
        (this._options = o),
        (this.onreceive = null),
        (this.onclose = null);
    }
    async connect(e, t) {
      return (
        y.isRequired(e, 'url'),
        y.isRequired(t, 'transferFormat'),
        y.isIn(t, M, 'transferFormat'),
        this._logger.log(f.Trace, '(SSE transport) Connecting.'),
        (this._url = e),
        this._accessToken &&
          (e +=
            (e.indexOf('?') < 0 ? '?' : '&') +
            `access_token=${encodeURIComponent(this._accessToken)}`),
        new Promise((n, o) => {
          let r,
            i = !1;
          if (t === M.Text) {
            if (v.isBrowser || v.isWebWorker)
              r = new this._options.EventSource(e, {
                withCredentials: this._options.withCredentials,
              });
            else {
              const t = this._httpClient.getCookieString(e),
                n = {};
              n.Cookie = t;
              const [o, i] = E();
              (n[o] = i),
                (r = new this._options.EventSource(e, {
                  withCredentials: this._options.withCredentials,
                  headers: { ...n, ...this._options.headers },
                }));
            }
            try {
              (r.onmessage = (e) => {
                if (this.onreceive)
                  try {
                    this._logger.log(
                      f.Trace,
                      `(SSE transport) data received. ${m(
                        e.data,
                        this._options.logMessageContent
                      )}.`
                    ),
                      this.onreceive(e.data);
                  } catch (e) {
                    return void this._close(e);
                  }
              }),
                (r.onerror = (e) => {
                  i
                    ? this._close()
                    : o(
                        new Error(
                          'EventSource failed to connect. The connection could not be found on the server, either the connection ID is not present on the server, or a proxy is refusing/buffering the connection. If you have multiple servers check that sticky sessions are enabled.'
                        )
                      );
                }),
                (r.onopen = () => {
                  this._logger.log(
                    f.Information,
                    `SSE connected to ${this._url}`
                  ),
                    (this._eventSource = r),
                    (i = !0),
                    n();
                });
            } catch (e) {
              return void o(e);
            }
          } else
            o(
              new Error(
                "The Server-Sent Events transport only supports the 'Text' transfer format"
              )
            );
        })
      );
    }
    async send(e) {
      return this._eventSource
        ? S(this._logger, 'SSE', this._httpClient, this._url, e, this._options)
        : Promise.reject(
            new Error('Cannot send until the transport is connected')
          );
    }
    stop() {
      return this._close(), Promise.resolve();
    }
    _close(e) {
      this._eventSource &&
        (this._eventSource.close(),
        (this._eventSource = void 0),
        this.onclose && this.onclose(e));
    }
  }
  class j {
    constructor(e, t, n, o, r, i) {
      (this._logger = n),
        (this._accessTokenFactory = t),
        (this._logMessageContent = o),
        (this._webSocketConstructor = r),
        (this._httpClient = e),
        (this.onreceive = null),
        (this.onclose = null),
        (this._headers = i);
    }
    async connect(e, t) {
      let n;
      return (
        y.isRequired(e, 'url'),
        y.isRequired(t, 'transferFormat'),
        y.isIn(t, M, 'transferFormat'),
        this._logger.log(f.Trace, '(WebSockets transport) Connecting.'),
        this._accessTokenFactory && (n = await this._accessTokenFactory()),
        new Promise((r, i) => {
          let s;
          e = e.replace(/^http/, 'ws');
          const a = this._httpClient.getCookieString(e);
          let c = !1;
          if (v.isNode || v.isReactNative) {
            const t = {},
              [r, i] = E();
            (t[r] = i),
              n && (t[o.Authorization] = `Bearer ${n}`),
              a && (t[o.Cookie] = a),
              (s = new this._webSocketConstructor(e, void 0, {
                headers: { ...t, ...this._headers },
              }));
          } else
            n &&
              (e +=
                (e.indexOf('?') < 0 ? '?' : '&') +
                `access_token=${encodeURIComponent(n)}`);
          s || (s = new this._webSocketConstructor(e)),
            t === M.Binary && (s.binaryType = 'arraybuffer'),
            (s.onopen = (t) => {
              this._logger.log(f.Information, `WebSocket connected to ${e}.`),
                (this._webSocket = s),
                (c = !0),
                r();
            }),
            (s.onerror = (e) => {
              let t = null;
              (t =
                'undefined' != typeof ErrorEvent && e instanceof ErrorEvent
                  ? e.error
                  : 'There was an error with the transport'),
                this._logger.log(f.Information, `(WebSockets transport) ${t}.`);
            }),
            (s.onmessage = (e) => {
              if (
                (this._logger.log(
                  f.Trace,
                  `(WebSockets transport) data received. ${m(
                    e.data,
                    this._logMessageContent
                  )}.`
                ),
                this.onreceive)
              )
                try {
                  this.onreceive(e.data);
                } catch (e) {
                  return void this._close(e);
                }
            }),
            (s.onclose = (e) => {
              if (c) this._close(e);
              else {
                let t = null;
                (t =
                  'undefined' != typeof ErrorEvent && e instanceof ErrorEvent
                    ? e.error
                    : 'WebSocket failed to connect. The connection could not be found on the server, either the endpoint may not be a SignalR endpoint, the connection ID is not present on the server, or there is a proxy blocking WebSockets. If you have multiple servers check that sticky sessions are enabled.'),
                  i(new Error(t));
              }
            });
        })
      );
    }
    send(e) {
      return this._webSocket &&
        this._webSocket.readyState === this._webSocketConstructor.OPEN
        ? (this._logger.log(
            f.Trace,
            `(WebSockets transport) sending data. ${m(
              e,
              this._logMessageContent
            )}.`
          ),
          this._webSocket.send(e),
          Promise.resolve())
        : Promise.reject('WebSocket is not in the OPEN state');
    }
    stop() {
      return this._webSocket && this._close(void 0), Promise.resolve();
    }
    _close(e) {
      this._webSocket &&
        ((this._webSocket.onclose = () => {}),
        (this._webSocket.onmessage = () => {}),
        (this._webSocket.onerror = () => {}),
        this._webSocket.close(),
        (this._webSocket = void 0)),
        this._logger.log(f.Trace, '(WebSockets transport) socket closed.'),
        this.onclose &&
          (!this._isCloseEvent(e) || (!1 !== e.wasClean && 1e3 === e.code)
            ? e instanceof Error
              ? this.onclose(e)
              : this.onclose()
            : this.onclose(
                new Error(
                  `WebSocket closed with status code: ${e.code} (${
                    e.reason || 'no reason given'
                  }).`
                )
              ));
    }
    _isCloseEvent(e) {
      return e && 'boolean' == typeof e.wasClean && 'number' == typeof e.code;
    }
  }
  class F {
    constructor(e, t = {}) {
      var n;
      if (
        ((this._stopPromiseResolver = () => {}),
        (this.features = {}),
        (this._negotiateVersion = 1),
        y.isRequired(e, 'url'),
        (this._logger =
          void 0 === (n = t.logger)
            ? new I(f.Information)
            : null === n
            ? _.instance
            : void 0 !== n.log
            ? n
            : new I(n)),
        (this.baseUrl = this._resolveUrl(e)),
        ((t = t || {}).logMessageContent =
          void 0 !== t.logMessageContent && t.logMessageContent),
        'boolean' != typeof t.withCredentials && void 0 !== t.withCredentials)
      )
        throw new Error(
          "withCredentials option was not a 'boolean' or 'undefined' value"
        );
      (t.withCredentials = void 0 === t.withCredentials || t.withCredentials),
        (t.timeout = void 0 === t.timeout ? 1e5 : t.timeout);
      let o = null,
        r = null;
      if (v.isNode) {
        const e = require;
        (o = e('ws')), (r = e('eventsource'));
      }
      v.isNode || 'undefined' == typeof WebSocket || t.WebSocket
        ? v.isNode && !t.WebSocket && o && (t.WebSocket = o)
        : (t.WebSocket = WebSocket),
        v.isNode || 'undefined' == typeof EventSource || t.EventSource
          ? v.isNode && !t.EventSource && void 0 !== r && (t.EventSource = r)
          : (t.EventSource = EventSource),
        (this._httpClient = new s(
          t.httpClient || new R(this._logger),
          t.accessTokenFactory
        )),
        (this._connectionState = 'Disconnected'),
        (this._connectionStarted = !1),
        (this._options = t),
        (this.onreceive = null),
        (this.onclose = null);
    }
    async start(e) {
      if (
        ((e = e || M.Binary),
        y.isIn(e, M, 'transferFormat'),
        this._logger.log(
          f.Debug,
          `Starting connection with transfer format '${M[e]}'.`
        ),
        'Disconnected' !== this._connectionState)
      )
        return Promise.reject(
          new Error(
            "Cannot start an HttpConnection that is not in the 'Disconnected' state."
          )
        );
      if (
        ((this._connectionState = 'Connecting'),
        (this._startInternalPromise = this._startInternal(e)),
        await this._startInternalPromise,
        'Disconnecting' === this._connectionState)
      ) {
        const e =
          'Failed to start the HttpConnection before stop() was called.';
        return (
          this._logger.log(f.Error, e),
          await this._stopPromise,
          Promise.reject(new h(e))
        );
      }
      if ('Connected' !== this._connectionState) {
        const e =
          "HttpConnection.startInternal completed gracefully but didn't enter the connection into the connected state!";
        return this._logger.log(f.Error, e), Promise.reject(new h(e));
      }
      this._connectionStarted = !0;
    }
    send(e) {
      return 'Connected' !== this._connectionState
        ? Promise.reject(
            new Error(
              "Cannot send data if the connection is not in the 'Connected' State."
            )
          )
        : (this._sendQueue || (this._sendQueue = new O(this.transport)),
          this._sendQueue.send(e));
    }
    async stop(e) {
      return 'Disconnected' === this._connectionState
        ? (this._logger.log(
            f.Debug,
            `Call to HttpConnection.stop(${e}) ignored because the connection is already in the disconnected state.`
          ),
          Promise.resolve())
        : 'Disconnecting' === this._connectionState
        ? (this._logger.log(
            f.Debug,
            `Call to HttpConnection.stop(${e}) ignored because the connection is already in the disconnecting state.`
          ),
          this._stopPromise)
        : ((this._connectionState = 'Disconnecting'),
          (this._stopPromise = new Promise((e) => {
            this._stopPromiseResolver = e;
          })),
          await this._stopInternal(e),
          void (await this._stopPromise));
    }
    async _stopInternal(e) {
      this._stopError = e;
      try {
        await this._startInternalPromise;
      } catch (e) {}
      if (this.transport) {
        try {
          await this.transport.stop();
        } catch (e) {
          this._logger.log(
            f.Error,
            `HttpConnection.transport.stop() threw error '${e}'.`
          ),
            this._stopConnection();
        }
        this.transport = void 0;
      } else
        this._logger.log(
          f.Debug,
          'HttpConnection.transport is undefined in HttpConnection.stop() because start() failed.'
        );
    }
    async _startInternal(e) {
      let t = this.baseUrl;
      (this._accessTokenFactory = this._options.accessTokenFactory),
        (this._httpClient._accessTokenFactory = this._accessTokenFactory);
      try {
        if (this._options.skipNegotiation) {
          if (this._options.transport !== A.WebSockets)
            throw new Error(
              'Negotiation can only be skipped when using the WebSocket transport directly.'
            );
          (this.transport = this._constructTransport(A.WebSockets)),
            await this._startTransport(t, e);
        } else {
          let n = null,
            o = 0;
          do {
            if (
              ((n = await this._getNegotiationResponse(t)),
              'Disconnecting' === this._connectionState ||
                'Disconnected' === this._connectionState)
            )
              throw new h('The connection was stopped during negotiation.');
            if (n.error) throw new Error(n.error);
            if (n.ProtocolVersion)
              throw new Error(
                'Detected a connection attempt to an ASP.NET SignalR Server. This client only supports connecting to an ASP.NET Core SignalR Server. See https://aka.ms/signalr-core-differences for details.'
              );
            if ((n.url && (t = n.url), n.accessToken)) {
              const e = n.accessToken;
              (this._accessTokenFactory = () => e),
                (this._httpClient._accessToken = e),
                (this._httpClient._accessTokenFactory = void 0);
            }
            o++;
          } while (n.url && o < 100);
          if (100 === o && n.url)
            throw new Error('Negotiate redirection limit exceeded.');
          await this._createTransport(t, this._options.transport, n, e);
        }
        this.transport instanceof H && (this.features.inherentKeepAlive = !0),
          'Connecting' === this._connectionState &&
            (this._logger.log(
              f.Debug,
              'The HttpConnection connected successfully.'
            ),
            (this._connectionState = 'Connected'));
      } catch (e) {
        return (
          this._logger.log(f.Error, 'Failed to start the connection: ' + e),
          (this._connectionState = 'Disconnected'),
          (this.transport = void 0),
          this._stopPromiseResolver(),
          Promise.reject(e)
        );
      }
    }
    async _getNegotiationResponse(e) {
      const t = {},
        [n, o] = E();
      t[n] = o;
      const r = this._resolveNegotiateUrl(e);
      this._logger.log(f.Debug, `Sending negotiation request: ${r}.`);
      try {
        const e = await this._httpClient.post(r, {
          content: '',
          headers: { ...t, ...this._options.headers },
          timeout: this._options.timeout,
          withCredentials: this._options.withCredentials,
        });
        if (200 !== e.statusCode)
          return Promise.reject(
            new Error(
              `Unexpected status code returned from negotiate '${e.statusCode}'`
            )
          );
        const n = JSON.parse(e.content);
        return (
          (!n.negotiateVersion || n.negotiateVersion < 1) &&
            (n.connectionToken = n.connectionId),
          n
        );
      } catch (e) {
        let t = 'Failed to complete negotiation with the server: ' + e;
        return (
          e instanceof a &&
            404 === e.statusCode &&
            (t +=
              ' Either this is not a SignalR endpoint or there is a proxy blocking the connection.'),
          this._logger.log(f.Error, t),
          Promise.reject(new p(t))
        );
      }
    }
    _createConnectUrl(e, t) {
      return t ? e + (-1 === e.indexOf('?') ? '?' : '&') + `id=${t}` : e;
    }
    async _createTransport(e, t, n, o) {
      let r = this._createConnectUrl(e, n.connectionToken);
      if (this._isITransport(t))
        return (
          this._logger.log(
            f.Debug,
            'Connection was provided an instance of ITransport, using that directly.'
          ),
          (this.transport = t),
          await this._startTransport(r, o),
          void (this.connectionId = n.connectionId)
        );
      const i = [],
        s = n.availableTransports || [];
      let a = n;
      for (const n of s) {
        const s = this._resolveTransportOrError(n, t, o);
        if (s instanceof Error) i.push(`${n.transport} failed:`), i.push(s);
        else if (this._isITransport(s)) {
          if (((this.transport = s), !a)) {
            try {
              a = await this._getNegotiationResponse(e);
            } catch (e) {
              return Promise.reject(e);
            }
            r = this._createConnectUrl(e, a.connectionToken);
          }
          try {
            return (
              await this._startTransport(r, o),
              void (this.connectionId = a.connectionId)
            );
          } catch (e) {
            if (
              (this._logger.log(
                f.Error,
                `Failed to start the transport '${n.transport}': ${e}`
              ),
              (a = void 0),
              i.push(new d(`${n.transport} failed: ${e}`, A[n.transport])),
              'Connecting' !== this._connectionState)
            ) {
              const e = 'Failed to select transport before stop() was called.';
              return this._logger.log(f.Debug, e), Promise.reject(new h(e));
            }
          }
        }
      }
      return i.length > 0
        ? Promise.reject(
            new g(
              `Unable to connect to the server with any of the available transports. ${i.join(
                ' '
              )}`,
              i
            )
          )
        : Promise.reject(
            new Error(
              'None of the transports supported by the client are supported by the server.'
            )
          );
    }
    _constructTransport(e) {
      switch (e) {
        case A.WebSockets:
          if (!this._options.WebSocket)
            throw new Error(
              "'WebSocket' is not supported in your environment."
            );
          return new j(
            this._httpClient,
            this._accessTokenFactory,
            this._logger,
            this._options.logMessageContent,
            this._options.WebSocket,
            this._options.headers || {}
          );
        case A.ServerSentEvents:
          if (!this._options.EventSource)
            throw new Error(
              "'EventSource' is not supported in your environment."
            );
          return new N(
            this._httpClient,
            this._httpClient._accessToken,
            this._logger,
            this._options
          );
        case A.LongPolling:
          return new H(this._httpClient, this._logger, this._options);
        default:
          throw new Error(`Unknown transport: ${e}.`);
      }
    }
    _startTransport(e, t) {
      return (
        (this.transport.onreceive = this.onreceive),
        (this.transport.onclose = (e) => this._stopConnection(e)),
        this.transport.connect(e, t)
      );
    }
    _resolveTransportOrError(e, t, n) {
      const o = A[e.transport];
      if (null == o)
        return (
          this._logger.log(
            f.Debug,
            `Skipping transport '${e.transport}' because it is not supported by this client.`
          ),
          new Error(
            `Skipping transport '${e.transport}' because it is not supported by this client.`
          )
        );
      if (
        !(function (e, t) {
          return !e || 0 != (t & e);
        })(t, o)
      )
        return (
          this._logger.log(
            f.Debug,
            `Skipping transport '${A[o]}' because it was disabled by the client.`
          ),
          new u(`'${A[o]}' is disabled by the client.`, o)
        );
      if (!(e.transferFormats.map((e) => M[e]).indexOf(n) >= 0))
        return (
          this._logger.log(
            f.Debug,
            `Skipping transport '${A[o]}' because it does not support the requested transfer format '${M[n]}'.`
          ),
          new Error(`'${A[o]}' does not support ${M[n]}.`)
        );
      if (
        (o === A.WebSockets && !this._options.WebSocket) ||
        (o === A.ServerSentEvents && !this._options.EventSource)
      )
        return (
          this._logger.log(
            f.Debug,
            `Skipping transport '${A[o]}' because it is not supported in your environment.'`
          ),
          new l(`'${A[o]}' is not supported in your environment.`, o)
        );
      this._logger.log(f.Debug, `Selecting transport '${A[o]}'.`);
      try {
        return this._constructTransport(o);
      } catch (e) {
        return e;
      }
    }
    _isITransport(e) {
      return e && 'object' == typeof e && 'connect' in e;
    }
    _stopConnection(e) {
      if (
        (this._logger.log(
          f.Debug,
          `HttpConnection.stopConnection(${e}) called while in state ${this._connectionState}.`
        ),
        (this.transport = void 0),
        (e = this._stopError || e),
        (this._stopError = void 0),
        'Disconnected' !== this._connectionState)
      ) {
        if ('Connecting' === this._connectionState)
          throw (
            (this._logger.log(
              f.Warning,
              `Call to HttpConnection.stopConnection(${e}) was ignored because the connection is still in the connecting state.`
            ),
            new Error(
              `HttpConnection.stopConnection(${e}) was called while the connection is still in the connecting state.`
            ))
          );
        if (
          ('Disconnecting' === this._connectionState &&
            this._stopPromiseResolver(),
          e
            ? this._logger.log(
                f.Error,
                `Connection disconnected with error '${e}'.`
              )
            : this._logger.log(f.Information, 'Connection disconnected.'),
          this._sendQueue &&
            (this._sendQueue.stop().catch((e) => {
              this._logger.log(
                f.Error,
                `TransportSendQueue.stop() threw error '${e}'.`
              );
            }),
            (this._sendQueue = void 0)),
          (this.connectionId = void 0),
          (this._connectionState = 'Disconnected'),
          this._connectionStarted)
        ) {
          this._connectionStarted = !1;
          try {
            this.onclose && this.onclose(e);
          } catch (t) {
            this._logger.log(
              f.Error,
              `HttpConnection.onclose(${e}) threw error '${t}'.`
            );
          }
        }
      } else
        this._logger.log(
          f.Debug,
          `Call to HttpConnection.stopConnection(${e}) was ignored because the connection is already in the disconnected state.`
        );
    }
    _resolveUrl(e) {
      if (
        0 === e.lastIndexOf('https://', 0) ||
        0 === e.lastIndexOf('http://', 0)
      )
        return e;
      if (!v.isBrowser) throw new Error(`Cannot resolve '${e}'.`);
      const t = window.document.createElement('a');
      return (
        (t.href = e),
        this._logger.log(f.Information, `Normalizing '${e}' to '${t.href}'.`),
        t.href
      );
    }
    _resolveNegotiateUrl(e) {
      const t = e.indexOf('?');
      let n = e.substring(0, -1 === t ? e.length : t);
      return (
        '/' !== n[n.length - 1] && (n += '/'),
        (n += 'negotiate'),
        (n += -1 === t ? '' : e.substring(t)),
        -1 === n.indexOf('negotiateVersion') &&
          ((n += -1 === t ? '?' : '&'),
          (n += 'negotiateVersion=' + this._negotiateVersion)),
        n
      );
    }
  }
  class O {
    constructor(e) {
      (this._transport = e),
        (this._buffer = []),
        (this._executing = !0),
        (this._sendBufferedData = new z()),
        (this._transportResult = new z()),
        (this._sendLoopPromise = this._sendLoop());
    }
    send(e) {
      return (
        this._bufferData(e),
        this._transportResult || (this._transportResult = new z()),
        this._transportResult.promise
      );
    }
    stop() {
      return (
        (this._executing = !1),
        this._sendBufferedData.resolve(),
        this._sendLoopPromise
      );
    }
    _bufferData(e) {
      if (this._buffer.length && typeof this._buffer[0] != typeof e)
        throw new Error(
          `Expected data to be of type ${typeof this
            ._buffer} but was of type ${typeof e}`
        );
      this._buffer.push(e), this._sendBufferedData.resolve();
    }
    async _sendLoop() {
      for (;;) {
        if ((await this._sendBufferedData.promise, !this._executing)) {
          this._transportResult &&
            this._transportResult.reject('Connection stopped.');
          break;
        }
        this._sendBufferedData = new z();
        const e = this._transportResult;
        this._transportResult = void 0;
        const t =
          'string' == typeof this._buffer[0]
            ? this._buffer.join('')
            : O._concatBuffers(this._buffer);
        this._buffer.length = 0;
        try {
          await this._transport.send(t), e.resolve();
        } catch (t) {
          e.reject(t);
        }
      }
    }
    static _concatBuffers(e) {
      const t = e.map((e) => e.byteLength).reduce((e, t) => e + t),
        n = new Uint8Array(t);
      let o = 0;
      for (const t of e) n.set(new Uint8Array(t), o), (o += t.byteLength);
      return n.buffer;
    }
  }
  class z {
    constructor() {
      this.promise = new Promise(
        (e, t) => ([this._resolver, this._rejecter] = [e, t])
      );
    }
    resolve() {
      this._resolver();
    }
    reject(e) {
      this._rejecter(e);
    }
  }
  class q {
    static write(e) {
      return `${e}${q.RecordSeparator}`;
    }
    static parse(e) {
      if (e[e.length - 1] !== q.RecordSeparator)
        throw new Error('Message is incomplete.');
      const t = e.split(q.RecordSeparator);
      return t.pop(), t;
    }
  }
  (q.RecordSeparatorCode = 30),
    (q.RecordSeparator = String.fromCharCode(q.RecordSeparatorCode));
  class V {
    writeHandshakeRequest(e) {
      return q.write(JSON.stringify(e));
    }
    parseHandshakeResponse(e) {
      let t, n;
      if (b(e)) {
        const o = new Uint8Array(e),
          r = o.indexOf(q.RecordSeparatorCode);
        if (-1 === r) throw new Error('Message is incomplete.');
        const i = r + 1;
        (t = String.fromCharCode.apply(
          null,
          Array.prototype.slice.call(o.slice(0, i))
        )),
          (n = o.byteLength > i ? o.slice(i).buffer : null);
      } else {
        const o = e,
          r = o.indexOf(q.RecordSeparator);
        if (-1 === r) throw new Error('Message is incomplete.');
        const i = r + 1;
        (t = o.substring(0, i)), (n = o.length > i ? o.substring(i) : null);
      }
      const o = q.parse(t),
        r = JSON.parse(o[0]);
      if (r.type)
        throw new Error('Expected a handshake response from the server.');
      return [n, r];
    }
  }
  !(function (e) {
    (e[(e.Invocation = 1)] = 'Invocation'),
      (e[(e.StreamItem = 2)] = 'StreamItem'),
      (e[(e.Completion = 3)] = 'Completion'),
      (e[(e.StreamInvocation = 4)] = 'StreamInvocation'),
      (e[(e.CancelInvocation = 5)] = 'CancelInvocation'),
      (e[(e.Ping = 6)] = 'Ping'),
      (e[(e.Close = 7)] = 'Close');
  })(B || (B = {}));
  class K {
    constructor() {
      this.observers = [];
    }
    next(e) {
      for (const t of this.observers) t.next(e);
    }
    error(e) {
      for (const t of this.observers) t.error && t.error(e);
    }
    complete() {
      for (const e of this.observers) e.complete && e.complete();
    }
    subscribe(e) {
      return this.observers.push(e), new C(this, e);
    }
  }
  !(function (e) {
    (e.Disconnected = 'Disconnected'),
      (e.Connecting = 'Connecting'),
      (e.Connected = 'Connected'),
      (e.Disconnecting = 'Disconnecting'),
      (e.Reconnecting = 'Reconnecting');
  })(L || (L = {}));
  class X {
    constructor(e, t, n, o) {
      (this._nextKeepAlive = 0),
        (this._freezeEventListener = () => {
          this._logger.log(
            f.Warning,
            'The page is being frozen, this will likely lead to the connection being closed and messages being lost. For more information see the docs at https://docs.microsoft.com/aspnet/core/signalr/javascript-client#bsleep'
          );
        }),
        y.isRequired(e, 'connection'),
        y.isRequired(t, 'logger'),
        y.isRequired(n, 'protocol'),
        (this.serverTimeoutInMilliseconds = 3e4),
        (this.keepAliveIntervalInMilliseconds = 15e3),
        (this._logger = t),
        (this._protocol = n),
        (this.connection = e),
        (this._reconnectPolicy = o),
        (this._handshakeProtocol = new V()),
        (this.connection.onreceive = (e) => this._processIncomingData(e)),
        (this.connection.onclose = (e) => this._connectionClosed(e)),
        (this._callbacks = {}),
        (this._methods = {}),
        (this._closedCallbacks = []),
        (this._reconnectingCallbacks = []),
        (this._reconnectedCallbacks = []),
        (this._invocationId = 0),
        (this._receivedHandshakeResponse = !1),
        (this._connectionState = L.Disconnected),
        (this._connectionStarted = !1),
        (this._cachedPingMessage = this._protocol.writeMessage({
          type: B.Ping,
        }));
    }
    static create(e, t, n, o) {
      return new X(e, t, n, o);
    }
    get state() {
      return this._connectionState;
    }
    get connectionId() {
      return (this.connection && this.connection.connectionId) || null;
    }
    get baseUrl() {
      return this.connection.baseUrl || '';
    }
    set baseUrl(e) {
      if (
        this._connectionState !== L.Disconnected &&
        this._connectionState !== L.Reconnecting
      )
        throw new Error(
          'The HubConnection must be in the Disconnected or Reconnecting state to change the url.'
        );
      if (!e) throw new Error('The HubConnection url must be a valid url.');
      this.connection.baseUrl = e;
    }
    start() {
      return (
        (this._startPromise = this._startWithStateTransitions()),
        this._startPromise
      );
    }
    async _startWithStateTransitions() {
      if (this._connectionState !== L.Disconnected)
        return Promise.reject(
          new Error(
            "Cannot start a HubConnection that is not in the 'Disconnected' state."
          )
        );
      (this._connectionState = L.Connecting),
        this._logger.log(f.Debug, 'Starting HubConnection.');
      try {
        await this._startInternal(),
          v.isBrowser &&
            window.document.addEventListener(
              'freeze',
              this._freezeEventListener
            ),
          (this._connectionState = L.Connected),
          (this._connectionStarted = !0),
          this._logger.log(f.Debug, 'HubConnection connected successfully.');
      } catch (e) {
        return (
          (this._connectionState = L.Disconnected),
          this._logger.log(
            f.Debug,
            `HubConnection failed to start successfully because of error '${e}'.`
          ),
          Promise.reject(e)
        );
      }
    }
    async _startInternal() {
      (this._stopDuringStartError = void 0),
        (this._receivedHandshakeResponse = !1);
      const e = new Promise((e, t) => {
        (this._handshakeResolver = e), (this._handshakeRejecter = t);
      });
      await this.connection.start(this._protocol.transferFormat);
      try {
        const t = {
          protocol: this._protocol.name,
          version: this._protocol.version,
        };
        if (
          (this._logger.log(f.Debug, 'Sending handshake request.'),
          await this._sendMessage(
            this._handshakeProtocol.writeHandshakeRequest(t)
          ),
          this._logger.log(
            f.Information,
            `Using HubProtocol '${this._protocol.name}'.`
          ),
          this._cleanupTimeout(),
          this._resetTimeoutPeriod(),
          this._resetKeepAliveInterval(),
          await e,
          this._stopDuringStartError)
        )
          throw this._stopDuringStartError;
        this.connection.features.inherentKeepAlive ||
          (await this._sendMessage(this._cachedPingMessage));
      } catch (e) {
        throw (
          (this._logger.log(
            f.Debug,
            `Hub handshake failed with error '${e}' during start(). Stopping HubConnection.`
          ),
          this._cleanupTimeout(),
          this._cleanupPingTimer(),
          await this.connection.stop(e),
          e)
        );
      }
    }
    async stop() {
      const e = this._startPromise;
      (this._stopPromise = this._stopInternal()), await this._stopPromise;
      try {
        await e;
      } catch (e) {}
    }
    _stopInternal(e) {
      return this._connectionState === L.Disconnected
        ? (this._logger.log(
            f.Debug,
            `Call to HubConnection.stop(${e}) ignored because it is already in the disconnected state.`
          ),
          Promise.resolve())
        : this._connectionState === L.Disconnecting
        ? (this._logger.log(
            f.Debug,
            `Call to HttpConnection.stop(${e}) ignored because the connection is already in the disconnecting state.`
          ),
          this._stopPromise)
        : ((this._connectionState = L.Disconnecting),
          this._logger.log(f.Debug, 'Stopping HubConnection.'),
          this._reconnectDelayHandle
            ? (this._logger.log(
                f.Debug,
                'Connection stopped during reconnect delay. Done reconnecting.'
              ),
              clearTimeout(this._reconnectDelayHandle),
              (this._reconnectDelayHandle = void 0),
              this._completeClose(),
              Promise.resolve())
            : (this._cleanupTimeout(),
              this._cleanupPingTimer(),
              (this._stopDuringStartError =
                e ||
                new h(
                  'The connection was stopped before the hub handshake could complete.'
                )),
              this.connection.stop(e)));
    }
    stream(e, ...t) {
      const [n, o] = this._replaceStreamingParams(t),
        r = this._createStreamInvocation(e, t, o);
      let i;
      const s = new K();
      return (
        (s.cancelCallback = () => {
          const e = this._createCancelInvocation(r.invocationId);
          return (
            delete this._callbacks[r.invocationId],
            i.then(() => this._sendWithProtocol(e))
          );
        }),
        (this._callbacks[r.invocationId] = (e, t) => {
          t
            ? s.error(t)
            : e &&
              (e.type === B.Completion
                ? e.error
                  ? s.error(new Error(e.error))
                  : s.complete()
                : s.next(e.item));
        }),
        (i = this._sendWithProtocol(r).catch((e) => {
          s.error(e), delete this._callbacks[r.invocationId];
        })),
        this._launchStreams(n, i),
        s
      );
    }
    _sendMessage(e) {
      return this._resetKeepAliveInterval(), this.connection.send(e);
    }
    _sendWithProtocol(e) {
      return this._sendMessage(this._protocol.writeMessage(e));
    }
    send(e, ...t) {
      const [n, o] = this._replaceStreamingParams(t),
        r = this._sendWithProtocol(this._createInvocation(e, t, !0, o));
      return this._launchStreams(n, r), r;
    }
    invoke(e, ...t) {
      const [n, o] = this._replaceStreamingParams(t),
        r = this._createInvocation(e, t, !1, o);
      return new Promise((e, t) => {
        this._callbacks[r.invocationId] = (n, o) => {
          o
            ? t(o)
            : n &&
              (n.type === B.Completion
                ? n.error
                  ? t(new Error(n.error))
                  : e(n.result)
                : t(new Error(`Unexpected message type: ${n.type}`)));
        };
        const o = this._sendWithProtocol(r).catch((e) => {
          t(e), delete this._callbacks[r.invocationId];
        });
        this._launchStreams(n, o);
      });
    }
    on(e, t) {
      e &&
        t &&
        ((e = e.toLowerCase()),
        this._methods[e] || (this._methods[e] = []),
        -1 === this._methods[e].indexOf(t) && this._methods[e].push(t));
    }
    off(e, t) {
      if (!e) return;
      e = e.toLowerCase();
      const n = this._methods[e];
      if (n)
        if (t) {
          const o = n.indexOf(t);
          -1 !== o &&
            (n.splice(o, 1), 0 === n.length && delete this._methods[e]);
        } else delete this._methods[e];
    }
    onclose(e) {
      e && this._closedCallbacks.push(e);
    }
    onreconnecting(e) {
      e && this._reconnectingCallbacks.push(e);
    }
    onreconnected(e) {
      e && this._reconnectedCallbacks.push(e);
    }
    _processIncomingData(e) {
      if (
        (this._cleanupTimeout(),
        this._receivedHandshakeResponse ||
          ((e = this._processHandshakeResponse(e)),
          (this._receivedHandshakeResponse = !0)),
        e)
      ) {
        const t = this._protocol.parseMessages(e, this._logger);
        for (const e of t)
          switch (e.type) {
            case B.Invocation:
              this._invokeClientMethod(e);
              break;
            case B.StreamItem:
            case B.Completion: {
              const t = this._callbacks[e.invocationId];
              if (t) {
                e.type === B.Completion &&
                  delete this._callbacks[e.invocationId];
                try {
                  t(e);
                } catch (e) {
                  this._logger.log(
                    f.Error,
                    `Stream callback threw error: ${x(e)}`
                  );
                }
              }
              break;
            }
            case B.Ping:
              break;
            case B.Close: {
              this._logger.log(
                f.Information,
                'Close message received from server.'
              );
              const t = e.error
                ? new Error('Server returned an error on close: ' + e.error)
                : void 0;
              !0 === e.allowReconnect
                ? this.connection.stop(t)
                : (this._stopPromise = this._stopInternal(t));
              break;
            }
            default:
              this._logger.log(f.Warning, `Invalid message type: ${e.type}.`);
          }
      }
      this._resetTimeoutPeriod();
    }
    _processHandshakeResponse(e) {
      let t, n;
      try {
        [n, t] = this._handshakeProtocol.parseHandshakeResponse(e);
      } catch (e) {
        const t = 'Error parsing handshake response: ' + e;
        this._logger.log(f.Error, t);
        const n = new Error(t);
        throw (this._handshakeRejecter(n), n);
      }
      if (t.error) {
        const e = 'Server returned handshake error: ' + t.error;
        this._logger.log(f.Error, e);
        const n = new Error(e);
        throw (this._handshakeRejecter(n), n);
      }
      return (
        this._logger.log(f.Debug, 'Server handshake complete.'),
        this._handshakeResolver(),
        n
      );
    }
    _resetKeepAliveInterval() {
      this.connection.features.inherentKeepAlive ||
        ((this._nextKeepAlive =
          new Date().getTime() + this.keepAliveIntervalInMilliseconds),
        this._cleanupPingTimer());
    }
    _resetTimeoutPeriod() {
      if (
        !(
          (this.connection.features &&
            this.connection.features.inherentKeepAlive) ||
          ((this._timeoutHandle = setTimeout(
            () => this.serverTimeout(),
            this.serverTimeoutInMilliseconds
          )),
          void 0 !== this._pingServerHandle)
        )
      ) {
        let e = this._nextKeepAlive - new Date().getTime();
        e < 0 && (e = 0),
          (this._pingServerHandle = setTimeout(async () => {
            if (this._connectionState === L.Connected)
              try {
                await this._sendMessage(this._cachedPingMessage);
              } catch {
                this._cleanupPingTimer();
              }
          }, e));
      }
    }
    serverTimeout() {
      this.connection.stop(
        new Error(
          'Server timeout elapsed without receiving a message from the server.'
        )
      );
    }
    async _invokeClientMethod(e) {
      const t = e.target.toLowerCase(),
        n = this._methods[t];
      if (!n)
        return (
          this._logger.log(
            f.Warning,
            `No client method with the name '${t}' found.`
          ),
          void (
            e.invocationId &&
            (this._logger.log(
              f.Warning,
              `No result given for '${t}' method and invocation ID '${e.invocationId}'.`
            ),
            await this._sendWithProtocol(
              this._createCompletionMessage(
                e.invocationId,
                "Client didn't provide a result.",
                null
              )
            ))
          )
        );
      const o = n.slice(),
        r = !!e.invocationId;
      let i, s, a;
      for (const n of o)
        try {
          const o = i;
          (i = await n.apply(this, e.arguments)),
            r &&
              i &&
              o &&
              (this._logger.log(
                f.Error,
                `Multiple results provided for '${t}'. Sending error to server.`
              ),
              (a = this._createCompletionMessage(
                e.invocationId,
                'Client provided multiple results.',
                null
              ))),
            (s = void 0);
        } catch (e) {
          (s = e),
            this._logger.log(
              f.Error,
              `A callback for the method '${t}' threw error '${e}'.`
            );
        }
      a
        ? await this._sendWithProtocol(a)
        : r
        ? (s
            ? (a = this._createCompletionMessage(e.invocationId, `${s}`, null))
            : void 0 !== i
            ? (a = this._createCompletionMessage(e.invocationId, null, i))
            : (this._logger.log(
                f.Warning,
                `No result given for '${t}' method and invocation ID '${e.invocationId}'.`
              ),
              (a = this._createCompletionMessage(
                e.invocationId,
                "Client didn't provide a result.",
                null
              ))),
          await this._sendWithProtocol(a))
        : i &&
          this._logger.log(
            f.Error,
            `Result given for '${t}' method but server is not expecting a result.`
          );
    }
    _connectionClosed(e) {
      this._logger.log(
        f.Debug,
        `HubConnection.connectionClosed(${e}) called while in state ${this._connectionState}.`
      ),
        (this._stopDuringStartError =
          this._stopDuringStartError ||
          e ||
          new h(
            'The underlying connection was closed before the hub handshake could complete.'
          )),
        this._handshakeResolver && this._handshakeResolver(),
        this._cancelCallbacksWithError(
          e ||
            new Error(
              'Invocation canceled due to the underlying connection being closed.'
            )
        ),
        this._cleanupTimeout(),
        this._cleanupPingTimer(),
        this._connectionState === L.Disconnecting
          ? this._completeClose(e)
          : this._connectionState === L.Connected && this._reconnectPolicy
          ? this._reconnect(e)
          : this._connectionState === L.Connected && this._completeClose(e);
    }
    _completeClose(e) {
      if (this._connectionStarted) {
        (this._connectionState = L.Disconnected),
          (this._connectionStarted = !1),
          v.isBrowser &&
            window.document.removeEventListener(
              'freeze',
              this._freezeEventListener
            );
        try {
          this._closedCallbacks.forEach((t) => t.apply(this, [e]));
        } catch (t) {
          this._logger.log(
            f.Error,
            `An onclose callback called with error '${e}' threw error '${t}'.`
          );
        }
      }
    }
    async _reconnect(e) {
      const t = Date.now();
      let n = 0,
        o =
          void 0 !== e
            ? e
            : new Error('Attempting to reconnect due to a unknown error.'),
        r = this._getNextRetryDelay(n++, 0, o);
      if (null === r)
        return (
          this._logger.log(
            f.Debug,
            'Connection not reconnecting because the IRetryPolicy returned null on the first reconnect attempt.'
          ),
          void this._completeClose(e)
        );
      if (
        ((this._connectionState = L.Reconnecting),
        e
          ? this._logger.log(
              f.Information,
              `Connection reconnecting because of error '${e}'.`
            )
          : this._logger.log(f.Information, 'Connection reconnecting.'),
        0 !== this._reconnectingCallbacks.length)
      ) {
        try {
          this._reconnectingCallbacks.forEach((t) => t.apply(this, [e]));
        } catch (t) {
          this._logger.log(
            f.Error,
            `An onreconnecting callback called with error '${e}' threw error '${t}'.`
          );
        }
        if (this._connectionState !== L.Reconnecting)
          return void this._logger.log(
            f.Debug,
            'Connection left the reconnecting state in onreconnecting callback. Done reconnecting.'
          );
      }
      for (; null !== r; ) {
        if (
          (this._logger.log(
            f.Information,
            `Reconnect attempt number ${n} will start in ${r} ms.`
          ),
          await new Promise((e) => {
            this._reconnectDelayHandle = setTimeout(e, r);
          }),
          (this._reconnectDelayHandle = void 0),
          this._connectionState !== L.Reconnecting)
        )
          return void this._logger.log(
            f.Debug,
            'Connection left the reconnecting state during reconnect delay. Done reconnecting.'
          );
        try {
          if (
            (await this._startInternal(),
            (this._connectionState = L.Connected),
            this._logger.log(
              f.Information,
              'HubConnection reconnected successfully.'
            ),
            0 !== this._reconnectedCallbacks.length)
          )
            try {
              this._reconnectedCallbacks.forEach((e) =>
                e.apply(this, [this.connection.connectionId])
              );
            } catch (e) {
              this._logger.log(
                f.Error,
                `An onreconnected callback called with connectionId '${this.connection.connectionId}; threw error '${e}'.`
              );
            }
          return;
        } catch (e) {
          if (
            (this._logger.log(
              f.Information,
              `Reconnect attempt failed because of error '${e}'.`
            ),
            this._connectionState !== L.Reconnecting)
          )
            return (
              this._logger.log(
                f.Debug,
                `Connection moved to the '${this._connectionState}' from the reconnecting state during reconnect attempt. Done reconnecting.`
              ),
              void (
                this._connectionState === L.Disconnecting &&
                this._completeClose()
              )
            );
          (o = e instanceof Error ? e : new Error(e.toString())),
            (r = this._getNextRetryDelay(n++, Date.now() - t, o));
        }
      }
      this._logger.log(
        f.Information,
        `Reconnect retries have been exhausted after ${
          Date.now() - t
        } ms and ${n} failed attempts. Connection disconnecting.`
      ),
        this._completeClose();
    }
    _getNextRetryDelay(e, t, n) {
      try {
        return this._reconnectPolicy.nextRetryDelayInMilliseconds({
          elapsedMilliseconds: t,
          previousRetryCount: e,
          retryReason: n,
        });
      } catch (n) {
        return (
          this._logger.log(
            f.Error,
            `IRetryPolicy.nextRetryDelayInMilliseconds(${e}, ${t}) threw error '${n}'.`
          ),
          null
        );
      }
    }
    _cancelCallbacksWithError(e) {
      const t = this._callbacks;
      (this._callbacks = {}),
        Object.keys(t).forEach((n) => {
          const o = t[n];
          try {
            o(null, e);
          } catch (t) {
            this._logger.log(
              f.Error,
              `Stream 'error' callback called with '${e}' threw error: ${x(t)}`
            );
          }
        });
    }
    _cleanupPingTimer() {
      this._pingServerHandle &&
        (clearTimeout(this._pingServerHandle),
        (this._pingServerHandle = void 0));
    }
    _cleanupTimeout() {
      this._timeoutHandle && clearTimeout(this._timeoutHandle);
    }
    _createInvocation(e, t, n, o) {
      if (n)
        return 0 !== o.length
          ? { arguments: t, streamIds: o, target: e, type: B.Invocation }
          : { arguments: t, target: e, type: B.Invocation };
      {
        const n = this._invocationId;
        return (
          this._invocationId++,
          0 !== o.length
            ? {
                arguments: t,
                invocationId: n.toString(),
                streamIds: o,
                target: e,
                type: B.Invocation,
              }
            : {
                arguments: t,
                invocationId: n.toString(),
                target: e,
                type: B.Invocation,
              }
        );
      }
    }
    _launchStreams(e, t) {
      if (0 !== e.length) {
        t || (t = Promise.resolve());
        for (const n in e)
          e[n].subscribe({
            complete: () => {
              t = t.then(() =>
                this._sendWithProtocol(this._createCompletionMessage(n))
              );
            },
            error: (e) => {
              let o;
              (o =
                e instanceof Error
                  ? e.message
                  : e && e.toString
                  ? e.toString()
                  : 'Unknown error'),
                (t = t.then(() =>
                  this._sendWithProtocol(this._createCompletionMessage(n, o))
                ));
            },
            next: (e) => {
              t = t.then(() =>
                this._sendWithProtocol(this._createStreamItemMessage(n, e))
              );
            },
          });
      }
    }
    _replaceStreamingParams(e) {
      const t = [],
        n = [];
      for (let o = 0; o < e.length; o++) {
        const r = e[o];
        if (this._isObservable(r)) {
          const i = this._invocationId;
          this._invocationId++,
            (t[i] = r),
            n.push(i.toString()),
            e.splice(o, 1);
        }
      }
      return [t, n];
    }
    _isObservable(e) {
      return e && e.subscribe && 'function' == typeof e.subscribe;
    }
    _createStreamInvocation(e, t, n) {
      const o = this._invocationId;
      return (
        this._invocationId++,
        0 !== n.length
          ? {
              arguments: t,
              invocationId: o.toString(),
              streamIds: n,
              target: e,
              type: B.StreamInvocation,
            }
          : {
              arguments: t,
              invocationId: o.toString(),
              target: e,
              type: B.StreamInvocation,
            }
      );
    }
    _createCancelInvocation(e) {
      return { invocationId: e, type: B.CancelInvocation };
    }
    _createStreamItemMessage(e, t) {
      return { invocationId: e, item: t, type: B.StreamItem };
    }
    _createCompletionMessage(e, t, n) {
      return t
        ? { error: t, invocationId: e, type: B.Completion }
        : { invocationId: e, result: n, type: B.Completion };
    }
  }
  class J {
    constructor() {
      (this.name = 'json'), (this.version = 1), (this.transferFormat = M.Text);
    }
    parseMessages(e, t) {
      if ('string' != typeof e)
        throw new Error(
          'Invalid input for JSON hub protocol. Expected a string.'
        );
      if (!e) return [];
      null === t && (t = _.instance);
      const n = q.parse(e),
        o = [];
      for (const e of n) {
        const n = JSON.parse(e);
        if ('number' != typeof n.type) throw new Error('Invalid payload.');
        switch (n.type) {
          case B.Invocation:
            this._isInvocationMessage(n);
            break;
          case B.StreamItem:
            this._isStreamItemMessage(n);
            break;
          case B.Completion:
            this._isCompletionMessage(n);
            break;
          case B.Ping:
          case B.Close:
            break;
          default:
            t.log(
              f.Information,
              "Unknown message type '" + n.type + "' ignored."
            );
            continue;
        }
        o.push(n);
      }
      return o;
    }
    writeMessage(e) {
      return q.write(JSON.stringify(e));
    }
    _isInvocationMessage(e) {
      this._assertNotEmptyString(
        e.target,
        'Invalid payload for Invocation message.'
      ),
        void 0 !== e.invocationId &&
          this._assertNotEmptyString(
            e.invocationId,
            'Invalid payload for Invocation message.'
          );
    }
    _isStreamItemMessage(e) {
      if (
        (this._assertNotEmptyString(
          e.invocationId,
          'Invalid payload for StreamItem message.'
        ),
        void 0 === e.item)
      )
        throw new Error('Invalid payload for StreamItem message.');
    }
    _isCompletionMessage(e) {
      if (e.result && e.error)
        throw new Error('Invalid payload for Completion message.');
      !e.result &&
        e.error &&
        this._assertNotEmptyString(
          e.error,
          'Invalid payload for Completion message.'
        ),
        this._assertNotEmptyString(
          e.invocationId,
          'Invalid payload for Completion message.'
        );
    }
    _assertNotEmptyString(e, t) {
      if ('string' != typeof e || '' === e) throw new Error(t);
    }
  }
  const Q = {
    trace: f.Trace,
    debug: f.Debug,
    info: f.Information,
    information: f.Information,
    warn: f.Warning,
    warning: f.Warning,
    error: f.Error,
    critical: f.Critical,
    none: f.None,
  };
  class G {
    configureLogging(e) {
      if ((y.isRequired(e, 'logging'), void 0 !== e.log)) this.logger = e;
      else if ('string' == typeof e) {
        const t = (function (e) {
          const t = Q[e.toLowerCase()];
          if (void 0 !== t) return t;
          throw new Error(`Unknown log level: ${e}`);
        })(e);
        this.logger = new I(t);
      } else this.logger = new I(e);
      return this;
    }
    withUrl(e, t) {
      return (
        y.isRequired(e, 'url'),
        y.isNotEmpty(e, 'url'),
        (this.url = e),
        (this.httpConnectionOptions =
          'object' == typeof t
            ? { ...this.httpConnectionOptions, ...t }
            : { ...this.httpConnectionOptions, transport: t }),
        this
      );
    }
    withHubProtocol(e) {
      return y.isRequired(e, 'protocol'), (this.protocol = e), this;
    }
    withAutomaticReconnect(e) {
      if (this.reconnectPolicy)
        throw new Error('A reconnectPolicy has already been set.');
      return (
        e
          ? Array.isArray(e)
            ? (this.reconnectPolicy = new n(e))
            : (this.reconnectPolicy = e)
          : (this.reconnectPolicy = new n()),
        this
      );
    }
    build() {
      const e = this.httpConnectionOptions || {};
      if ((void 0 === e.logger && (e.logger = this.logger), !this.url))
        throw new Error(
          "The 'HubConnectionBuilder.withUrl' method must be called before building the connection."
        );
      const t = new F(this.url, e);
      return X.create(
        t,
        this.logger || _.instance,
        this.protocol || new J(),
        this.reconnectPolicy
      );
    }
  }
  var Y,
    Z,
    ee,
    te = 4294967295;
  function ne(e, t, n) {
    var o = Math.floor(n / 4294967296),
      r = n;
    e.setUint32(t, o), e.setUint32(t + 4, r);
  }
  function oe(e, t) {
    return 4294967296 * e.getInt32(t) + e.getUint32(t + 4);
  }
  var re =
    ('undefined' == typeof process ||
      'never' !==
        (null ===
          (Y = null === process || void 0 === process ? void 0 : process.env) ||
        void 0 === Y
          ? void 0
          : Y.TEXT_ENCODING)) &&
    'undefined' != typeof TextEncoder &&
    'undefined' != typeof TextDecoder;
  function ie(e) {
    for (var t = e.length, n = 0, o = 0; o < t; ) {
      var r = e.charCodeAt(o++);
      if (0 != (4294967168 & r))
        if (0 == (4294965248 & r)) n += 2;
        else {
          if (r >= 55296 && r <= 56319 && o < t) {
            var i = e.charCodeAt(o);
            56320 == (64512 & i) &&
              (++o, (r = ((1023 & r) << 10) + (1023 & i) + 65536));
          }
          n += 0 == (4294901760 & r) ? 3 : 4;
        }
      else n++;
    }
    return n;
  }
  var se = re ? new TextEncoder() : void 0,
    ae = re
      ? 'undefined' != typeof process &&
        'force' !==
          (null ===
            (Z =
              null === process || void 0 === process ? void 0 : process.env) ||
          void 0 === Z
            ? void 0
            : Z.TEXT_ENCODING)
        ? 200
        : 0
      : te,
    ce = (null == se ? void 0 : se.encodeInto)
      ? function (e, t, n) {
          se.encodeInto(e, t.subarray(n));
        }
      : function (e, t, n) {
          t.set(se.encode(e), n);
        };
  function he(e, t, n) {
    for (var o = t, r = o + n, i = [], s = ''; o < r; ) {
      var a = e[o++];
      if (0 == (128 & a)) i.push(a);
      else if (192 == (224 & a)) {
        var c = 63 & e[o++];
        i.push(((31 & a) << 6) | c);
      } else if (224 == (240 & a)) {
        c = 63 & e[o++];
        var h = 63 & e[o++];
        i.push(((31 & a) << 12) | (c << 6) | h);
      } else if (240 == (248 & a)) {
        var l =
          ((7 & a) << 18) |
          ((c = 63 & e[o++]) << 12) |
          ((h = 63 & e[o++]) << 6) |
          (63 & e[o++]);
        l > 65535 &&
          ((l -= 65536),
          i.push(((l >>> 10) & 1023) | 55296),
          (l = 56320 | (1023 & l))),
          i.push(l);
      } else i.push(a);
      i.length >= 4096 &&
        ((s += String.fromCharCode.apply(String, i)), (i.length = 0));
    }
    return i.length > 0 && (s += String.fromCharCode.apply(String, i)), s;
  }
  var le,
    ue = re ? new TextDecoder() : null,
    de = re
      ? 'undefined' != typeof process &&
        'force' !==
          (null ===
            (ee =
              null === process || void 0 === process ? void 0 : process.env) ||
          void 0 === ee
            ? void 0
            : ee.TEXT_DECODER)
        ? 200
        : 0
      : te,
    pe = function (e, t) {
      (this.type = e), (this.data = t);
    },
    ge =
      ((le = function (e, t) {
        return (
          (le =
            Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array &&
              function (e, t) {
                e.__proto__ = t;
              }) ||
            function (e, t) {
              for (var n in t)
                Object.prototype.hasOwnProperty.call(t, n) && (e[n] = t[n]);
            }),
          le(e, t)
        );
      }),
      function (e, t) {
        if ('function' != typeof t && null !== t)
          throw new TypeError(
            'Class extends value ' + String(t) + ' is not a constructor or null'
          );
        function n() {
          this.constructor = e;
        }
        le(e, t),
          (e.prototype =
            null === t
              ? Object.create(t)
              : ((n.prototype = t.prototype), new n()));
      }),
    fe = (function (e) {
      function t(n) {
        var o = e.call(this, n) || this,
          r = Object.create(t.prototype);
        return (
          Object.setPrototypeOf(o, r),
          Object.defineProperty(o, 'name', {
            configurable: !0,
            enumerable: !1,
            value: t.name,
          }),
          o
        );
      }
      return ge(t, e), t;
    })(Error),
    _e = {
      type: -1,
      encode: function (e) {
        var t, n, o, r;
        return e instanceof Date
          ? (function (e) {
              var t,
                n = e.sec,
                o = e.nsec;
              if (n >= 0 && o >= 0 && n <= 17179869183) {
                if (0 === o && n <= 4294967295) {
                  var r = new Uint8Array(4);
                  return (t = new DataView(r.buffer)).setUint32(0, n), r;
                }
                var i = n / 4294967296,
                  s = 4294967295 & n;
                return (
                  (r = new Uint8Array(8)),
                  (t = new DataView(r.buffer)).setUint32(0, (o << 2) | (3 & i)),
                  t.setUint32(4, s),
                  r
                );
              }
              return (
                (r = new Uint8Array(12)),
                (t = new DataView(r.buffer)).setUint32(0, o),
                ne(t, 4, n),
                r
              );
            })(
              ((o =
                1e6 * ((t = e.getTime()) - 1e3 * (n = Math.floor(t / 1e3)))),
              { sec: n + (r = Math.floor(o / 1e9)), nsec: o - 1e9 * r })
            )
          : null;
      },
      decode: function (e) {
        var t = (function (e) {
          var t = new DataView(e.buffer, e.byteOffset, e.byteLength);
          switch (e.byteLength) {
            case 4:
              return { sec: t.getUint32(0), nsec: 0 };
            case 8:
              var n = t.getUint32(0);
              return {
                sec: 4294967296 * (3 & n) + t.getUint32(4),
                nsec: n >>> 2,
              };
            case 12:
              return { sec: oe(t, 4), nsec: t.getUint32(0) };
            default:
              throw new fe(
                'Unrecognized data size for timestamp (expected 4, 8, or 12): '.concat(
                  e.length
                )
              );
          }
        })(e);
        return new Date(1e3 * t.sec + t.nsec / 1e6);
      },
    },
    we = (function () {
      function e() {
        (this.builtInEncoders = []),
          (this.builtInDecoders = []),
          (this.encoders = []),
          (this.decoders = []),
          this.register(_e);
      }
      return (
        (e.prototype.register = function (e) {
          var t = e.type,
            n = e.encode,
            o = e.decode;
          if (t >= 0) (this.encoders[t] = n), (this.decoders[t] = o);
          else {
            var r = 1 + t;
            (this.builtInEncoders[r] = n), (this.builtInDecoders[r] = o);
          }
        }),
        (e.prototype.tryToEncode = function (e, t) {
          for (var n = 0; n < this.builtInEncoders.length; n++)
            if (null != (o = this.builtInEncoders[n]) && null != (r = o(e, t)))
              return new pe(-1 - n, r);
          for (n = 0; n < this.encoders.length; n++) {
            var o, r;
            if (null != (o = this.encoders[n]) && null != (r = o(e, t)))
              return new pe(n, r);
          }
          return e instanceof pe ? e : null;
        }),
        (e.prototype.decode = function (e, t, n) {
          var o = t < 0 ? this.builtInDecoders[-1 - t] : this.decoders[t];
          return o ? o(e, t, n) : new pe(t, e);
        }),
        (e.defaultCodec = new e()),
        e
      );
    })();
  function ye(e) {
    return e instanceof Uint8Array
      ? e
      : ArrayBuffer.isView(e)
      ? new Uint8Array(e.buffer, e.byteOffset, e.byteLength)
      : e instanceof ArrayBuffer
      ? new Uint8Array(e)
      : Uint8Array.from(e);
  }
  var ve = (function () {
    function e(e, t, n, o, r, i, s, a) {
      void 0 === e && (e = we.defaultCodec),
        void 0 === t && (t = void 0),
        void 0 === n && (n = 100),
        void 0 === o && (o = 2048),
        void 0 === r && (r = !1),
        void 0 === i && (i = !1),
        void 0 === s && (s = !1),
        void 0 === a && (a = !1),
        (this.extensionCodec = e),
        (this.context = t),
        (this.maxDepth = n),
        (this.initialBufferSize = o),
        (this.sortKeys = r),
        (this.forceFloat32 = i),
        (this.ignoreUndefined = s),
        (this.forceIntegerToFloat = a),
        (this.pos = 0),
        (this.view = new DataView(new ArrayBuffer(this.initialBufferSize))),
        (this.bytes = new Uint8Array(this.view.buffer));
    }
    return (
      (e.prototype.reinitializeState = function () {
        this.pos = 0;
      }),
      (e.prototype.encodeSharedRef = function (e) {
        return (
          this.reinitializeState(),
          this.doEncode(e, 1),
          this.bytes.subarray(0, this.pos)
        );
      }),
      (e.prototype.encode = function (e) {
        return (
          this.reinitializeState(),
          this.doEncode(e, 1),
          this.bytes.slice(0, this.pos)
        );
      }),
      (e.prototype.doEncode = function (e, t) {
        if (t > this.maxDepth)
          throw new Error('Too deep objects in depth '.concat(t));
        null == e
          ? this.encodeNil()
          : 'boolean' == typeof e
          ? this.encodeBoolean(e)
          : 'number' == typeof e
          ? this.encodeNumber(e)
          : 'string' == typeof e
          ? this.encodeString(e)
          : this.encodeObject(e, t);
      }),
      (e.prototype.ensureBufferSizeToWrite = function (e) {
        var t = this.pos + e;
        this.view.byteLength < t && this.resizeBuffer(2 * t);
      }),
      (e.prototype.resizeBuffer = function (e) {
        var t = new ArrayBuffer(e),
          n = new Uint8Array(t),
          o = new DataView(t);
        n.set(this.bytes), (this.view = o), (this.bytes = n);
      }),
      (e.prototype.encodeNil = function () {
        this.writeU8(192);
      }),
      (e.prototype.encodeBoolean = function (e) {
        !1 === e ? this.writeU8(194) : this.writeU8(195);
      }),
      (e.prototype.encodeNumber = function (e) {
        Number.isSafeInteger(e) && !this.forceIntegerToFloat
          ? e >= 0
            ? e < 128
              ? this.writeU8(e)
              : e < 256
              ? (this.writeU8(204), this.writeU8(e))
              : e < 65536
              ? (this.writeU8(205), this.writeU16(e))
              : e < 4294967296
              ? (this.writeU8(206), this.writeU32(e))
              : (this.writeU8(207), this.writeU64(e))
            : e >= -32
            ? this.writeU8(224 | (e + 32))
            : e >= -128
            ? (this.writeU8(208), this.writeI8(e))
            : e >= -32768
            ? (this.writeU8(209), this.writeI16(e))
            : e >= -2147483648
            ? (this.writeU8(210), this.writeI32(e))
            : (this.writeU8(211), this.writeI64(e))
          : this.forceFloat32
          ? (this.writeU8(202), this.writeF32(e))
          : (this.writeU8(203), this.writeF64(e));
      }),
      (e.prototype.writeStringHeader = function (e) {
        if (e < 32) this.writeU8(160 + e);
        else if (e < 256) this.writeU8(217), this.writeU8(e);
        else if (e < 65536) this.writeU8(218), this.writeU16(e);
        else {
          if (!(e < 4294967296))
            throw new Error('Too long string: '.concat(e, ' bytes in UTF-8'));
          this.writeU8(219), this.writeU32(e);
        }
      }),
      (e.prototype.encodeString = function (e) {
        if (e.length > ae) {
          var t = ie(e);
          this.ensureBufferSizeToWrite(5 + t),
            this.writeStringHeader(t),
            ce(e, this.bytes, this.pos),
            (this.pos += t);
        } else
          (t = ie(e)),
            this.ensureBufferSizeToWrite(5 + t),
            this.writeStringHeader(t),
            (function (e, t, n) {
              for (var o = e.length, r = n, i = 0; i < o; ) {
                var s = e.charCodeAt(i++);
                if (0 != (4294967168 & s)) {
                  if (0 == (4294965248 & s)) t[r++] = ((s >> 6) & 31) | 192;
                  else {
                    if (s >= 55296 && s <= 56319 && i < o) {
                      var a = e.charCodeAt(i);
                      56320 == (64512 & a) &&
                        (++i, (s = ((1023 & s) << 10) + (1023 & a) + 65536));
                    }
                    0 == (4294901760 & s)
                      ? ((t[r++] = ((s >> 12) & 15) | 224),
                        (t[r++] = ((s >> 6) & 63) | 128))
                      : ((t[r++] = ((s >> 18) & 7) | 240),
                        (t[r++] = ((s >> 12) & 63) | 128),
                        (t[r++] = ((s >> 6) & 63) | 128));
                  }
                  t[r++] = (63 & s) | 128;
                } else t[r++] = s;
              }
            })(e, this.bytes, this.pos),
            (this.pos += t);
      }),
      (e.prototype.encodeObject = function (e, t) {
        var n = this.extensionCodec.tryToEncode(e, this.context);
        if (null != n) this.encodeExtension(n);
        else if (Array.isArray(e)) this.encodeArray(e, t);
        else if (ArrayBuffer.isView(e)) this.encodeBinary(e);
        else {
          if ('object' != typeof e)
            throw new Error(
              'Unrecognized object: '.concat(Object.prototype.toString.apply(e))
            );
          this.encodeMap(e, t);
        }
      }),
      (e.prototype.encodeBinary = function (e) {
        var t = e.byteLength;
        if (t < 256) this.writeU8(196), this.writeU8(t);
        else if (t < 65536) this.writeU8(197), this.writeU16(t);
        else {
          if (!(t < 4294967296))
            throw new Error('Too large binary: '.concat(t));
          this.writeU8(198), this.writeU32(t);
        }
        var n = ye(e);
        this.writeU8a(n);
      }),
      (e.prototype.encodeArray = function (e, t) {
        var n = e.length;
        if (n < 16) this.writeU8(144 + n);
        else if (n < 65536) this.writeU8(220), this.writeU16(n);
        else {
          if (!(n < 4294967296)) throw new Error('Too large array: '.concat(n));
          this.writeU8(221), this.writeU32(n);
        }
        for (var o = 0, r = e; o < r.length; o++) {
          var i = r[o];
          this.doEncode(i, t + 1);
        }
      }),
      (e.prototype.countWithoutUndefined = function (e, t) {
        for (var n = 0, o = 0, r = t; o < r.length; o++)
          void 0 !== e[r[o]] && n++;
        return n;
      }),
      (e.prototype.encodeMap = function (e, t) {
        var n = Object.keys(e);
        this.sortKeys && n.sort();
        var o = this.ignoreUndefined
          ? this.countWithoutUndefined(e, n)
          : n.length;
        if (o < 16) this.writeU8(128 + o);
        else if (o < 65536) this.writeU8(222), this.writeU16(o);
        else {
          if (!(o < 4294967296))
            throw new Error('Too large map object: '.concat(o));
          this.writeU8(223), this.writeU32(o);
        }
        for (var r = 0, i = n; r < i.length; r++) {
          var s = i[r],
            a = e[s];
          (this.ignoreUndefined && void 0 === a) ||
            (this.encodeString(s), this.doEncode(a, t + 1));
        }
      }),
      (e.prototype.encodeExtension = function (e) {
        var t = e.data.length;
        if (1 === t) this.writeU8(212);
        else if (2 === t) this.writeU8(213);
        else if (4 === t) this.writeU8(214);
        else if (8 === t) this.writeU8(215);
        else if (16 === t) this.writeU8(216);
        else if (t < 256) this.writeU8(199), this.writeU8(t);
        else if (t < 65536) this.writeU8(200), this.writeU16(t);
        else {
          if (!(t < 4294967296))
            throw new Error('Too large extension object: '.concat(t));
          this.writeU8(201), this.writeU32(t);
        }
        this.writeI8(e.type), this.writeU8a(e.data);
      }),
      (e.prototype.writeU8 = function (e) {
        this.ensureBufferSizeToWrite(1),
          this.view.setUint8(this.pos, e),
          this.pos++;
      }),
      (e.prototype.writeU8a = function (e) {
        var t = e.length;
        this.ensureBufferSizeToWrite(t),
          this.bytes.set(e, this.pos),
          (this.pos += t);
      }),
      (e.prototype.writeI8 = function (e) {
        this.ensureBufferSizeToWrite(1),
          this.view.setInt8(this.pos, e),
          this.pos++;
      }),
      (e.prototype.writeU16 = function (e) {
        this.ensureBufferSizeToWrite(2),
          this.view.setUint16(this.pos, e),
          (this.pos += 2);
      }),
      (e.prototype.writeI16 = function (e) {
        this.ensureBufferSizeToWrite(2),
          this.view.setInt16(this.pos, e),
          (this.pos += 2);
      }),
      (e.prototype.writeU32 = function (e) {
        this.ensureBufferSizeToWrite(4),
          this.view.setUint32(this.pos, e),
          (this.pos += 4);
      }),
      (e.prototype.writeI32 = function (e) {
        this.ensureBufferSizeToWrite(4),
          this.view.setInt32(this.pos, e),
          (this.pos += 4);
      }),
      (e.prototype.writeF32 = function (e) {
        this.ensureBufferSizeToWrite(4),
          this.view.setFloat32(this.pos, e),
          (this.pos += 4);
      }),
      (e.prototype.writeF64 = function (e) {
        this.ensureBufferSizeToWrite(8),
          this.view.setFloat64(this.pos, e),
          (this.pos += 8);
      }),
      (e.prototype.writeU64 = function (e) {
        this.ensureBufferSizeToWrite(8),
          (function (e, t, n) {
            var o = n / 4294967296,
              r = n;
            e.setUint32(t, o), e.setUint32(t + 4, r);
          })(this.view, this.pos, e),
          (this.pos += 8);
      }),
      (e.prototype.writeI64 = function (e) {
        this.ensureBufferSizeToWrite(8),
          ne(this.view, this.pos, e),
          (this.pos += 8);
      }),
      e
    );
  })();
  function me(e) {
    return ''
      .concat(e < 0 ? '-' : '', '0x')
      .concat(Math.abs(e).toString(16).padStart(2, '0'));
  }
  var be = (function () {
      function e(e, t) {
        void 0 === e && (e = 16),
          void 0 === t && (t = 16),
          (this.maxKeyLength = e),
          (this.maxLengthPerKey = t),
          (this.hit = 0),
          (this.miss = 0),
          (this.caches = []);
        for (var n = 0; n < this.maxKeyLength; n++) this.caches.push([]);
      }
      return (
        (e.prototype.canBeCached = function (e) {
          return e > 0 && e <= this.maxKeyLength;
        }),
        (e.prototype.find = function (e, t, n) {
          e: for (var o = 0, r = this.caches[n - 1]; o < r.length; o++) {
            for (var i = r[o], s = i.bytes, a = 0; a < n; a++)
              if (s[a] !== e[t + a]) continue e;
            return i.str;
          }
          return null;
        }),
        (e.prototype.store = function (e, t) {
          var n = this.caches[e.length - 1],
            o = { bytes: e, str: t };
          n.length >= this.maxLengthPerKey
            ? (n[(Math.random() * n.length) | 0] = o)
            : n.push(o);
        }),
        (e.prototype.decode = function (e, t, n) {
          var o = this.find(e, t, n);
          if (null != o) return this.hit++, o;
          this.miss++;
          var r = he(e, t, n),
            i = Uint8Array.prototype.slice.call(e, t, t + n);
          return this.store(i, r), r;
        }),
        e
      );
    })(),
    Se = function (e, t) {
      var n,
        o,
        r,
        i,
        s = {
          label: 0,
          sent: function () {
            if (1 & r[0]) throw r[1];
            return r[1];
          },
          trys: [],
          ops: [],
        };
      return (
        (i = { next: a(0), throw: a(1), return: a(2) }),
        'function' == typeof Symbol &&
          (i[Symbol.iterator] = function () {
            return this;
          }),
        i
      );
      function a(i) {
        return function (a) {
          return (function (i) {
            if (n) throw new TypeError('Generator is already executing.');
            for (; s; )
              try {
                if (
                  ((n = 1),
                  o &&
                    (r =
                      2 & i[0]
                        ? o.return
                        : i[0]
                        ? o.throw || ((r = o.return) && r.call(o), 0)
                        : o.next) &&
                    !(r = r.call(o, i[1])).done)
                )
                  return r;
                switch (((o = 0), r && (i = [2 & i[0], r.value]), i[0])) {
                  case 0:
                  case 1:
                    r = i;
                    break;
                  case 4:
                    return s.label++, { value: i[1], done: !1 };
                  case 5:
                    s.label++, (o = i[1]), (i = [0]);
                    continue;
                  case 7:
                    (i = s.ops.pop()), s.trys.pop();
                    continue;
                  default:
                    if (
                      !(
                        (r = (r = s.trys).length > 0 && r[r.length - 1]) ||
                        (6 !== i[0] && 2 !== i[0])
                      )
                    ) {
                      s = 0;
                      continue;
                    }
                    if (3 === i[0] && (!r || (i[1] > r[0] && i[1] < r[3]))) {
                      s.label = i[1];
                      break;
                    }
                    if (6 === i[0] && s.label < r[1]) {
                      (s.label = r[1]), (r = i);
                      break;
                    }
                    if (r && s.label < r[2]) {
                      (s.label = r[2]), s.ops.push(i);
                      break;
                    }
                    r[2] && s.ops.pop(), s.trys.pop();
                    continue;
                }
                i = t.call(e, s);
              } catch (e) {
                (i = [6, e]), (o = 0);
              } finally {
                n = r = 0;
              }
            if (5 & i[0]) throw i[1];
            return { value: i[0] ? i[1] : void 0, done: !0 };
          })([i, a]);
        };
      }
    },
    Ce = function (e) {
      if (!Symbol.asyncIterator)
        throw new TypeError('Symbol.asyncIterator is not defined.');
      var t,
        n = e[Symbol.asyncIterator];
      return n
        ? n.call(e)
        : ((e =
            'function' == typeof __values ? __values(e) : e[Symbol.iterator]()),
          (t = {}),
          o('next'),
          o('throw'),
          o('return'),
          (t[Symbol.asyncIterator] = function () {
            return this;
          }),
          t);
      function o(n) {
        t[n] =
          e[n] &&
          function (t) {
            return new Promise(function (o, r) {
              !(function (e, t, n, o) {
                Promise.resolve(o).then(function (t) {
                  e({ value: t, done: n });
                }, t);
              })(o, r, (t = e[n](t)).done, t.value);
            });
          };
      }
    },
    Ie = function (e) {
      return this instanceof Ie ? ((this.v = e), this) : new Ie(e);
    },
    Ee = new DataView(new ArrayBuffer(0)),
    ke = new Uint8Array(Ee.buffer),
    Te = (function () {
      try {
        Ee.getInt8(0);
      } catch (e) {
        return e.constructor;
      }
      throw new Error('never reached');
    })(),
    Ue = new Te('Insufficient data'),
    xe = new be(),
    Pe = (function () {
      function e(e, t, n, o, r, i, s, a) {
        void 0 === e && (e = we.defaultCodec),
          void 0 === t && (t = void 0),
          void 0 === n && (n = te),
          void 0 === o && (o = te),
          void 0 === r && (r = te),
          void 0 === i && (i = te),
          void 0 === s && (s = te),
          void 0 === a && (a = xe),
          (this.extensionCodec = e),
          (this.context = t),
          (this.maxStrLength = n),
          (this.maxBinLength = o),
          (this.maxArrayLength = r),
          (this.maxMapLength = i),
          (this.maxExtLength = s),
          (this.keyDecoder = a),
          (this.totalPos = 0),
          (this.pos = 0),
          (this.view = Ee),
          (this.bytes = ke),
          (this.headByte = -1),
          (this.stack = []);
      }
      return (
        (e.prototype.reinitializeState = function () {
          (this.totalPos = 0), (this.headByte = -1), (this.stack.length = 0);
        }),
        (e.prototype.setBuffer = function (e) {
          (this.bytes = ye(e)),
            (this.view = (function (e) {
              if (e instanceof ArrayBuffer) return new DataView(e);
              var t = ye(e);
              return new DataView(t.buffer, t.byteOffset, t.byteLength);
            })(this.bytes)),
            (this.pos = 0);
        }),
        (e.prototype.appendBuffer = function (e) {
          if (-1 !== this.headByte || this.hasRemaining(1)) {
            var t = this.bytes.subarray(this.pos),
              n = ye(e),
              o = new Uint8Array(t.length + n.length);
            o.set(t), o.set(n, t.length), this.setBuffer(o);
          } else this.setBuffer(e);
        }),
        (e.prototype.hasRemaining = function (e) {
          return this.view.byteLength - this.pos >= e;
        }),
        (e.prototype.createExtraByteError = function (e) {
          var t = this.view,
            n = this.pos;
          return new RangeError(
            'Extra '
              .concat(t.byteLength - n, ' of ')
              .concat(t.byteLength, ' byte(s) found at buffer[')
              .concat(e, ']')
          );
        }),
        (e.prototype.decode = function (e) {
          this.reinitializeState(), this.setBuffer(e);
          var t = this.doDecodeSync();
          if (this.hasRemaining(1)) throw this.createExtraByteError(this.pos);
          return t;
        }),
        (e.prototype.decodeMulti = function (e) {
          return Se(this, function (t) {
            switch (t.label) {
              case 0:
                this.reinitializeState(), this.setBuffer(e), (t.label = 1);
              case 1:
                return this.hasRemaining(1) ? [4, this.doDecodeSync()] : [3, 3];
              case 2:
                return t.sent(), [3, 1];
              case 3:
                return [2];
            }
          });
        }),
        (e.prototype.decodeAsync = function (e) {
          var t, n, o, r, i, s, a, c;
          return (
            (i = this),
            (s = void 0),
            (c = function () {
              var i, s, a, c, h, l, u, d;
              return Se(this, function (p) {
                switch (p.label) {
                  case 0:
                    (i = !1), (p.label = 1);
                  case 1:
                    p.trys.push([1, 6, 7, 12]), (t = Ce(e)), (p.label = 2);
                  case 2:
                    return [4, t.next()];
                  case 3:
                    if ((n = p.sent()).done) return [3, 5];
                    if (((a = n.value), i))
                      throw this.createExtraByteError(this.totalPos);
                    this.appendBuffer(a);
                    try {
                      (s = this.doDecodeSync()), (i = !0);
                    } catch (e) {
                      if (!(e instanceof Te)) throw e;
                    }
                    (this.totalPos += this.pos), (p.label = 4);
                  case 4:
                    return [3, 2];
                  case 5:
                    return [3, 12];
                  case 6:
                    return (c = p.sent()), (o = { error: c }), [3, 12];
                  case 7:
                    return (
                      p.trys.push([7, , 10, 11]),
                      n && !n.done && (r = t.return) ? [4, r.call(t)] : [3, 9]
                    );
                  case 8:
                    p.sent(), (p.label = 9);
                  case 9:
                    return [3, 11];
                  case 10:
                    if (o) throw o.error;
                    return [7];
                  case 11:
                    return [7];
                  case 12:
                    if (i) {
                      if (this.hasRemaining(1))
                        throw this.createExtraByteError(this.totalPos);
                      return [2, s];
                    }
                    throw (
                      ((l = (h = this).headByte),
                      (u = h.pos),
                      (d = h.totalPos),
                      new RangeError(
                        'Insufficient data in parsing '
                          .concat(me(l), ' at ')
                          .concat(d, ' (')
                          .concat(u, ' in the current buffer)')
                      ))
                    );
                }
              });
            }),
            new ((a = void 0) || (a = Promise))(function (e, t) {
              function n(e) {
                try {
                  r(c.next(e));
                } catch (e) {
                  t(e);
                }
              }
              function o(e) {
                try {
                  r(c.throw(e));
                } catch (e) {
                  t(e);
                }
              }
              function r(t) {
                var r;
                t.done
                  ? e(t.value)
                  : ((r = t.value),
                    r instanceof a
                      ? r
                      : new a(function (e) {
                          e(r);
                        })).then(n, o);
              }
              r((c = c.apply(i, s || [])).next());
            })
          );
        }),
        (e.prototype.decodeArrayStream = function (e) {
          return this.decodeMultiAsync(e, !0);
        }),
        (e.prototype.decodeStream = function (e) {
          return this.decodeMultiAsync(e, !1);
        }),
        (e.prototype.decodeMultiAsync = function (e, t) {
          return (function (e, t, n) {
            if (!Symbol.asyncIterator)
              throw new TypeError('Symbol.asyncIterator is not defined.');
            var o,
              r = n.apply(e, t || []),
              i = [];
            return (
              (o = {}),
              s('next'),
              s('throw'),
              s('return'),
              (o[Symbol.asyncIterator] = function () {
                return this;
              }),
              o
            );
            function s(e) {
              r[e] &&
                (o[e] = function (t) {
                  return new Promise(function (n, o) {
                    i.push([e, t, n, o]) > 1 || a(e, t);
                  });
                });
            }
            function a(e, t) {
              try {
                (n = r[e](t)).value instanceof Ie
                  ? Promise.resolve(n.value.v).then(c, h)
                  : l(i[0][2], n);
              } catch (e) {
                l(i[0][3], e);
              }
              var n;
            }
            function c(e) {
              a('next', e);
            }
            function h(e) {
              a('throw', e);
            }
            function l(e, t) {
              e(t), i.shift(), i.length && a(i[0][0], i[0][1]);
            }
          })(this, arguments, function () {
            var n, o, r, i, s, a, c, h, l;
            return Se(this, function (u) {
              switch (u.label) {
                case 0:
                  (n = t), (o = -1), (u.label = 1);
                case 1:
                  u.trys.push([1, 13, 14, 19]), (r = Ce(e)), (u.label = 2);
                case 2:
                  return [4, Ie(r.next())];
                case 3:
                  if ((i = u.sent()).done) return [3, 12];
                  if (((s = i.value), t && 0 === o))
                    throw this.createExtraByteError(this.totalPos);
                  this.appendBuffer(s),
                    n &&
                      ((o = this.readArraySize()), (n = !1), this.complete()),
                    (u.label = 4);
                case 4:
                  u.trys.push([4, 9, , 10]), (u.label = 5);
                case 5:
                  return [4, Ie(this.doDecodeSync())];
                case 6:
                  return [4, u.sent()];
                case 7:
                  return u.sent(), 0 == --o ? [3, 8] : [3, 5];
                case 8:
                  return [3, 10];
                case 9:
                  if (!((a = u.sent()) instanceof Te)) throw a;
                  return [3, 10];
                case 10:
                  (this.totalPos += this.pos), (u.label = 11);
                case 11:
                  return [3, 2];
                case 12:
                  return [3, 19];
                case 13:
                  return (c = u.sent()), (h = { error: c }), [3, 19];
                case 14:
                  return (
                    u.trys.push([14, , 17, 18]),
                    i && !i.done && (l = r.return)
                      ? [4, Ie(l.call(r))]
                      : [3, 16]
                  );
                case 15:
                  u.sent(), (u.label = 16);
                case 16:
                  return [3, 18];
                case 17:
                  if (h) throw h.error;
                  return [7];
                case 18:
                  return [7];
                case 19:
                  return [2];
              }
            });
          });
        }),
        (e.prototype.doDecodeSync = function () {
          e: for (;;) {
            var e = this.readHeadByte(),
              t = void 0;
            if (e >= 224) t = e - 256;
            else if (e < 192)
              if (e < 128) t = e;
              else if (e < 144) {
                if (0 != (o = e - 128)) {
                  this.pushMapState(o), this.complete();
                  continue e;
                }
                t = {};
              } else if (e < 160) {
                if (0 != (o = e - 144)) {
                  this.pushArrayState(o), this.complete();
                  continue e;
                }
                t = [];
              } else {
                var n = e - 160;
                t = this.decodeUtf8String(n, 0);
              }
            else if (192 === e) t = null;
            else if (194 === e) t = !1;
            else if (195 === e) t = !0;
            else if (202 === e) t = this.readF32();
            else if (203 === e) t = this.readF64();
            else if (204 === e) t = this.readU8();
            else if (205 === e) t = this.readU16();
            else if (206 === e) t = this.readU32();
            else if (207 === e) t = this.readU64();
            else if (208 === e) t = this.readI8();
            else if (209 === e) t = this.readI16();
            else if (210 === e) t = this.readI32();
            else if (211 === e) t = this.readI64();
            else if (217 === e)
              (n = this.lookU8()), (t = this.decodeUtf8String(n, 1));
            else if (218 === e)
              (n = this.lookU16()), (t = this.decodeUtf8String(n, 2));
            else if (219 === e)
              (n = this.lookU32()), (t = this.decodeUtf8String(n, 4));
            else if (220 === e) {
              if (0 !== (o = this.readU16())) {
                this.pushArrayState(o), this.complete();
                continue e;
              }
              t = [];
            } else if (221 === e) {
              if (0 !== (o = this.readU32())) {
                this.pushArrayState(o), this.complete();
                continue e;
              }
              t = [];
            } else if (222 === e) {
              if (0 !== (o = this.readU16())) {
                this.pushMapState(o), this.complete();
                continue e;
              }
              t = {};
            } else if (223 === e) {
              if (0 !== (o = this.readU32())) {
                this.pushMapState(o), this.complete();
                continue e;
              }
              t = {};
            } else if (196 === e) {
              var o = this.lookU8();
              t = this.decodeBinary(o, 1);
            } else if (197 === e)
              (o = this.lookU16()), (t = this.decodeBinary(o, 2));
            else if (198 === e)
              (o = this.lookU32()), (t = this.decodeBinary(o, 4));
            else if (212 === e) t = this.decodeExtension(1, 0);
            else if (213 === e) t = this.decodeExtension(2, 0);
            else if (214 === e) t = this.decodeExtension(4, 0);
            else if (215 === e) t = this.decodeExtension(8, 0);
            else if (216 === e) t = this.decodeExtension(16, 0);
            else if (199 === e)
              (o = this.lookU8()), (t = this.decodeExtension(o, 1));
            else if (200 === e)
              (o = this.lookU16()), (t = this.decodeExtension(o, 2));
            else {
              if (201 !== e)
                throw new fe('Unrecognized type byte: '.concat(me(e)));
              (o = this.lookU32()), (t = this.decodeExtension(o, 4));
            }
            this.complete();
            for (var r = this.stack; r.length > 0; ) {
              var i = r[r.length - 1];
              if (0 === i.type) {
                if (
                  ((i.array[i.position] = t),
                  i.position++,
                  i.position !== i.size)
                )
                  continue e;
                r.pop(), (t = i.array);
              } else {
                if (1 === i.type) {
                  if ((void 0, 'string' != (s = typeof t) && 'number' !== s))
                    throw new fe(
                      'The type of key must be string or number but ' + typeof t
                    );
                  if ('__proto__' === t)
                    throw new fe('The key __proto__ is not allowed');
                  (i.key = t), (i.type = 2);
                  continue e;
                }
                if (
                  ((i.map[i.key] = t), i.readCount++, i.readCount !== i.size)
                ) {
                  (i.key = null), (i.type = 1);
                  continue e;
                }
                r.pop(), (t = i.map);
              }
            }
            return t;
          }
          var s;
        }),
        (e.prototype.readHeadByte = function () {
          return (
            -1 === this.headByte && (this.headByte = this.readU8()),
            this.headByte
          );
        }),
        (e.prototype.complete = function () {
          this.headByte = -1;
        }),
        (e.prototype.readArraySize = function () {
          var e = this.readHeadByte();
          switch (e) {
            case 220:
              return this.readU16();
            case 221:
              return this.readU32();
            default:
              if (e < 160) return e - 144;
              throw new fe('Unrecognized array type byte: '.concat(me(e)));
          }
        }),
        (e.prototype.pushMapState = function (e) {
          if (e > this.maxMapLength)
            throw new fe(
              'Max length exceeded: map length ('
                .concat(e, ') > maxMapLengthLength (')
                .concat(this.maxMapLength, ')')
            );
          this.stack.push({
            type: 1,
            size: e,
            key: null,
            readCount: 0,
            map: {},
          });
        }),
        (e.prototype.pushArrayState = function (e) {
          if (e > this.maxArrayLength)
            throw new fe(
              'Max length exceeded: array length ('
                .concat(e, ') > maxArrayLength (')
                .concat(this.maxArrayLength, ')')
            );
          this.stack.push({
            type: 0,
            size: e,
            array: new Array(e),
            position: 0,
          });
        }),
        (e.prototype.decodeUtf8String = function (e, t) {
          var n;
          if (e > this.maxStrLength)
            throw new fe(
              'Max length exceeded: UTF-8 byte length ('
                .concat(e, ') > maxStrLength (')
                .concat(this.maxStrLength, ')')
            );
          if (this.bytes.byteLength < this.pos + t + e) throw Ue;
          var o,
            r = this.pos + t;
          return (
            (o =
              this.stateIsMapKey() &&
              (null === (n = this.keyDecoder) || void 0 === n
                ? void 0
                : n.canBeCached(e))
                ? this.keyDecoder.decode(this.bytes, r, e)
                : e > de
                ? (function (e, t, n) {
                    var o = e.subarray(t, t + n);
                    return ue.decode(o);
                  })(this.bytes, r, e)
                : he(this.bytes, r, e)),
            (this.pos += t + e),
            o
          );
        }),
        (e.prototype.stateIsMapKey = function () {
          return (
            this.stack.length > 0 &&
            1 === this.stack[this.stack.length - 1].type
          );
        }),
        (e.prototype.decodeBinary = function (e, t) {
          if (e > this.maxBinLength)
            throw new fe(
              'Max length exceeded: bin length ('
                .concat(e, ') > maxBinLength (')
                .concat(this.maxBinLength, ')')
            );
          if (!this.hasRemaining(e + t)) throw Ue;
          var n = this.pos + t,
            o = this.bytes.subarray(n, n + e);
          return (this.pos += t + e), o;
        }),
        (e.prototype.decodeExtension = function (e, t) {
          if (e > this.maxExtLength)
            throw new fe(
              'Max length exceeded: ext length ('
                .concat(e, ') > maxExtLength (')
                .concat(this.maxExtLength, ')')
            );
          var n = this.view.getInt8(this.pos + t),
            o = this.decodeBinary(e, t + 1);
          return this.extensionCodec.decode(o, n, this.context);
        }),
        (e.prototype.lookU8 = function () {
          return this.view.getUint8(this.pos);
        }),
        (e.prototype.lookU16 = function () {
          return this.view.getUint16(this.pos);
        }),
        (e.prototype.lookU32 = function () {
          return this.view.getUint32(this.pos);
        }),
        (e.prototype.readU8 = function () {
          var e = this.view.getUint8(this.pos);
          return this.pos++, e;
        }),
        (e.prototype.readI8 = function () {
          var e = this.view.getInt8(this.pos);
          return this.pos++, e;
        }),
        (e.prototype.readU16 = function () {
          var e = this.view.getUint16(this.pos);
          return (this.pos += 2), e;
        }),
        (e.prototype.readI16 = function () {
          var e = this.view.getInt16(this.pos);
          return (this.pos += 2), e;
        }),
        (e.prototype.readU32 = function () {
          var e = this.view.getUint32(this.pos);
          return (this.pos += 4), e;
        }),
        (e.prototype.readI32 = function () {
          var e = this.view.getInt32(this.pos);
          return (this.pos += 4), e;
        }),
        (e.prototype.readU64 = function () {
          var e,
            t,
            n =
              ((e = this.view),
              (t = this.pos),
              4294967296 * e.getUint32(t) + e.getUint32(t + 4));
          return (this.pos += 8), n;
        }),
        (e.prototype.readI64 = function () {
          var e = oe(this.view, this.pos);
          return (this.pos += 8), e;
        }),
        (e.prototype.readF32 = function () {
          var e = this.view.getFloat32(this.pos);
          return (this.pos += 4), e;
        }),
        (e.prototype.readF64 = function () {
          var e = this.view.getFloat64(this.pos);
          return (this.pos += 8), e;
        }),
        e
      );
    })();
  class De {
    static write(e) {
      let t = e.byteLength || e.length;
      const n = [];
      do {
        let e = 127 & t;
        (t >>= 7), t > 0 && (e |= 128), n.push(e);
      } while (t > 0);
      t = e.byteLength || e.length;
      const o = new Uint8Array(n.length + t);
      return o.set(n, 0), o.set(e, n.length), o.buffer;
    }
    static parse(e) {
      const t = [],
        n = new Uint8Array(e),
        o = [0, 7, 14, 21, 28];
      for (let r = 0; r < e.byteLength; ) {
        let i,
          s = 0,
          a = 0;
        do {
          (i = n[r + s]), (a |= (127 & i) << o[s]), s++;
        } while (s < Math.min(5, e.byteLength - r) && 0 != (128 & i));
        if (0 != (128 & i) && s < 5)
          throw new Error('Cannot read message size.');
        if (5 === s && i > 7)
          throw new Error('Messages bigger than 2GB are not supported.');
        if (!(n.byteLength >= r + s + a))
          throw new Error('Incomplete message.');
        t.push(
          n.slice ? n.slice(r + s, r + s + a) : n.subarray(r + s, r + s + a)
        ),
          (r = r + s + a);
      }
      return t;
    }
  }
  const $e = new Uint8Array([145, B.Ping]);
  class Re {
    constructor(e) {
      (this.name = 'messagepack'),
        (this.version = 1),
        (this.transferFormat = M.Binary),
        (this._errorResult = 1),
        (this._voidResult = 2),
        (this._nonVoidResult = 3),
        (e = e || {}),
        (this._encoder = new ve(
          e.extensionCodec,
          e.context,
          e.maxDepth,
          e.initialBufferSize,
          e.sortKeys,
          e.forceFloat32,
          e.ignoreUndefined,
          e.forceIntegerToFloat
        )),
        (this._decoder = new Pe(
          e.extensionCodec,
          e.context,
          e.maxStrLength,
          e.maxBinLength,
          e.maxArrayLength,
          e.maxMapLength,
          e.maxExtLength
        ));
    }
    parseMessages(e, t) {
      if (
        !(n = e) ||
        'undefined' == typeof ArrayBuffer ||
        !(
          n instanceof ArrayBuffer ||
          (n.constructor && 'ArrayBuffer' === n.constructor.name)
        )
      )
        throw new Error(
          'Invalid input for MessagePack hub protocol. Expected an ArrayBuffer.'
        );
      var n;
      null === t && (t = _.instance);
      const o = De.parse(e),
        r = [];
      for (const e of o) {
        const n = this._parseMessage(e, t);
        n && r.push(n);
      }
      return r;
    }
    writeMessage(e) {
      switch (e.type) {
        case B.Invocation:
          return this._writeInvocation(e);
        case B.StreamInvocation:
          return this._writeStreamInvocation(e);
        case B.StreamItem:
          return this._writeStreamItem(e);
        case B.Completion:
          return this._writeCompletion(e);
        case B.Ping:
          return De.write($e);
        case B.CancelInvocation:
          return this._writeCancelInvocation(e);
        default:
          throw new Error('Invalid message type.');
      }
    }
    _parseMessage(e, t) {
      if (0 === e.length) throw new Error('Invalid payload.');
      const n = this._decoder.decode(e);
      if (0 === n.length || !(n instanceof Array))
        throw new Error('Invalid payload.');
      const o = n[0];
      switch (o) {
        case B.Invocation:
          return this._createInvocationMessage(this._readHeaders(n), n);
        case B.StreamItem:
          return this._createStreamItemMessage(this._readHeaders(n), n);
        case B.Completion:
          return this._createCompletionMessage(this._readHeaders(n), n);
        case B.Ping:
          return this._createPingMessage(n);
        case B.Close:
          return this._createCloseMessage(n);
        default:
          return (
            t.log(f.Information, "Unknown message type '" + o + "' ignored."),
            null
          );
      }
    }
    _createCloseMessage(e) {
      if (e.length < 2) throw new Error('Invalid payload for Close message.');
      return {
        allowReconnect: e.length >= 3 ? e[2] : void 0,
        error: e[1],
        type: B.Close,
      };
    }
    _createPingMessage(e) {
      if (e.length < 1) throw new Error('Invalid payload for Ping message.');
      return { type: B.Ping };
    }
    _createInvocationMessage(e, t) {
      if (t.length < 5)
        throw new Error('Invalid payload for Invocation message.');
      const n = t[2];
      return n
        ? {
            arguments: t[4],
            headers: e,
            invocationId: n,
            streamIds: [],
            target: t[3],
            type: B.Invocation,
          }
        : {
            arguments: t[4],
            headers: e,
            streamIds: [],
            target: t[3],
            type: B.Invocation,
          };
    }
    _createStreamItemMessage(e, t) {
      if (t.length < 4)
        throw new Error('Invalid payload for StreamItem message.');
      return { headers: e, invocationId: t[2], item: t[3], type: B.StreamItem };
    }
    _createCompletionMessage(e, t) {
      if (t.length < 4)
        throw new Error('Invalid payload for Completion message.');
      const n = t[3];
      if (n !== this._voidResult && t.length < 5)
        throw new Error('Invalid payload for Completion message.');
      let o, r;
      switch (n) {
        case this._errorResult:
          o = t[4];
          break;
        case this._nonVoidResult:
          r = t[4];
      }
      return {
        error: o,
        headers: e,
        invocationId: t[2],
        result: r,
        type: B.Completion,
      };
    }
    _writeInvocation(e) {
      let t;
      return (
        (t = e.streamIds
          ? this._encoder.encode([
              B.Invocation,
              e.headers || {},
              e.invocationId || null,
              e.target,
              e.arguments,
              e.streamIds,
            ])
          : this._encoder.encode([
              B.Invocation,
              e.headers || {},
              e.invocationId || null,
              e.target,
              e.arguments,
            ])),
        De.write(t.slice())
      );
    }
    _writeStreamInvocation(e) {
      let t;
      return (
        (t = e.streamIds
          ? this._encoder.encode([
              B.StreamInvocation,
              e.headers || {},
              e.invocationId,
              e.target,
              e.arguments,
              e.streamIds,
            ])
          : this._encoder.encode([
              B.StreamInvocation,
              e.headers || {},
              e.invocationId,
              e.target,
              e.arguments,
            ])),
        De.write(t.slice())
      );
    }
    _writeStreamItem(e) {
      const t = this._encoder.encode([
        B.StreamItem,
        e.headers || {},
        e.invocationId,
        e.item,
      ]);
      return De.write(t.slice());
    }
    _writeCompletion(e) {
      const t = e.error
        ? this._errorResult
        : void 0 !== e.result
        ? this._nonVoidResult
        : this._voidResult;
      let n;
      switch (t) {
        case this._errorResult:
          n = this._encoder.encode([
            B.Completion,
            e.headers || {},
            e.invocationId,
            t,
            e.error,
          ]);
          break;
        case this._voidResult:
          n = this._encoder.encode([
            B.Completion,
            e.headers || {},
            e.invocationId,
            t,
          ]);
          break;
        case this._nonVoidResult:
          n = this._encoder.encode([
            B.Completion,
            e.headers || {},
            e.invocationId,
            t,
            e.result,
          ]);
      }
      return De.write(n.slice());
    }
    _writeCancelInvocation(e) {
      const t = this._encoder.encode([
        B.CancelInvocation,
        e.headers || {},
        e.invocationId,
      ]);
      return De.write(t.slice());
    }
    _readHeaders(e) {
      const t = e[1];
      if ('object' != typeof t) throw new Error('Invalid headers.');
      return t;
    }
  }
  let Ae = [];
  window.setupSignalRConnection = function (e, t) {
    let n = new G()
      .withUrl(`http://${e}:5076/hubs/control`, {
        skipNegotiation: !0,
        transport: A.WebSockets,
      })
      .withHubProtocol(new Re())
      .configureLogging(f.Information)
      .build();
    n.on('ScreenUpdate', (e) => {
      if ((Ae.push(e.Data), e.IsEndOfImage)) {
        let e = new Blob(Ae, { type: 'image/png' }),
          n = URL.createObjectURL(e);
        t.invokeMethodAsync('UpdateScreenDataUrl', n), (Ae = []);
      }
    }),
      n.start().catch((e) => console.error(e.toString()));
  };
})();
