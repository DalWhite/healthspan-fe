// Offline-first local store: SQLite compiled to WebAssembly with the
// persistent OPFS VFS (the .db file lives in the browser's origin-private
// file system, so it survives reloads and works with no network at all).
//
// OPFS requires createSyncAccessHandle + Atomics.wait, which only exist in
// worker threads under cross-origin isolation, so sqlite3 runs inside a
// dedicated module worker (offline-db-worker.js) and this file is a thin
// postMessage wrapper with the same API the C# interop expects.
//
// C# talks to this via IJSRuntime:
//   hsdb.exec(sql, paramsJson) -> JSON array of row objects
//   hsdb.changes(sql, paramsJson) -> { changes, lastInsertRowId }
//   hsdb.state() -> { ready, backend: 'opfs'|'memory'|'failed' }
'use strict';

(function () {
    const workerUrl = new URL('js/sqlite/offline-db-worker.js', document.baseURI).href;
    let worker = null;
    let seq = 0;
    let backend = 'none';
    let workerFailed = false;
    const pending = new Map();
    const readyWaiters = [];

    // Deferred worker spawn: sqlite3.wasm compile must not compete with the
    // .NET/Blazor boot window. It starts once the page is idle, or earlier on
    // first hsdb.* use.
    function ensureWorker() {
        if (worker) return worker;
        worker = new Worker(workerUrl, { type: 'module' });

        worker.onmessage = (e) => {
            const msg = e.data || {};
            if (msg.type === 'ready') {
                backend = msg.backend;
                readyWaiters.splice(0).forEach((res) => res(true));
                return;
            }
            if (msg.type === 'failed') {
                workerFailed = true;
                readyWaiters.splice(0).forEach((res) => res(false));
                return;
            }
            const p = pending.get(msg.id);
            if (!p) return;
            pending.delete(msg.id);
            if (msg.error) p.reject(new Error(msg.error));
            else p.resolve(msg.result);
        };

        worker.onerror = (e) => {
            console.error('healthspan offline-db worker error:', e.message);
            workerFailed = true;
            for (const p of pending.values()) p.reject(new Error(e.message || 'offline-db worker error'));
            pending.clear();
            readyWaiters.splice(0).forEach((res) => res(false));
        };

        return worker;
    }

    const idleStart = window.requestIdleCallback ?? ((fn, opts) => setTimeout(fn, opts?.timeout ?? 4000));
    idleStart(() => ensureWorker(), { timeout: 4000 });

    function whenReady() {
        if (workerFailed) return Promise.reject(new Error('offline-db worker failed'));
        if (backend !== 'none') return Promise.resolve(true);
        ensureWorker();
        return new Promise((resolve, reject) => {
            readyWaiters.push((ok) => ok ? resolve(true) : reject(new Error('offline-db worker failed')));
        });
    }

    function call(method, sql, paramsJson) {
        return new Promise((resolve, reject) => {
            const id = ++seq;
            pending.set(id, { resolve, reject });
            worker.postMessage({ id, method, sql, bind: paramsJson ? JSON.parse(paramsJson) : [] });
        });
    }

    function callTx(statementsJson) {
        return new Promise((resolve, reject) => {
            const id = ++seq;
            pending.set(id, { resolve, reject });
            worker.postMessage({ id, method: 'tx', statements: JSON.parse(statementsJson) });
        });
    }

    globalThis.hsdb = {
        ready: async function () {
            try {
                await whenReady();
                return true;
            } catch (e) {
                return false;
            }
        },

        state: async function () {
            if (workerFailed) return { ready: false, backend: 'failed' };
            if (backend === 'none') return { ready: false, backend: 'none' };
            return { ready: true, backend };
        },

        // SELECT-style: returns a JSON string with an array of row objects.
        exec: async function (sql, paramsJson) {
            await whenReady();
            const rows = await call('exec', sql, paramsJson);
            return JSON.stringify(rows);
        },

        // INSERT/UPDATE/DELETE-style: returns { changes, lastInsertRowId }.
        changes: async function (sql, paramsJson) {
            await whenReady();
            return await call('changes', sql, paramsJson);
        },

        // Batch: statementsJson is a JSON array of {sql, bind} applied in a
        // single transaction (one OPFS sync for the whole batch).
        tx: async function (statementsJson) {
            await whenReady();
            return await callTx(statementsJson);
        },

        // Mirror a whole catalog body: the worker splits the rows and writes them
        // in one transaction, so the main thread only ships the response string.
        mirrorCatalog: async function (kind, body, idProperty, cachedAt) {
            await whenReady();
            return await new Promise((resolve, reject) => {
                const id = ++seq;
                pending.set(id, { resolve, reject });
                worker.postMessage({ id, method: 'mirrorCatalog', kind, body, idProperty, cachedAt });
            });
        },

        // Connectivity events are bridged to C#: pass a DotNetObjectReference
        // (preferred) or a global function name as a fallback.
        setConnectivityBridge: function (dotnetRefOrName) {
            const notify = () => {
                try {
                    if (dotnetRefOrName && typeof dotnetRefOrName.invokeMethodAsync === 'function') {
                        dotnetRefOrName.invokeMethodAsync('OnBrowserConnectivity', navigator.onLine);
                        return;
                    }
                    const fn = globalThis[dotnetRefOrName];
                    if (typeof fn === 'function') fn(navigator.onLine);
                } catch (e) {
                    console.warn('healthspan offline-db: connectivity bridge failed', e);
                }
            };
            window.addEventListener('online', notify);
            window.addEventListener('offline', notify);
            notify();
        }
    };
})();
