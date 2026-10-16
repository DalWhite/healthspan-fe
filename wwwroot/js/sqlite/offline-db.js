// Offline-first local store: SQLite compiled to WebAssembly with the
// persistent OPFS VFS (the .db file lives in the browser's origin-private
// file system, so it survives reloads and works with no network at all).
//
// C# talks to this via IJSRuntime:
//   hsdb.exec(sql, paramsJson) -> JSON array of row objects
//   hsdb.changes(sql, paramsJson) -> { changes, lastInsertRowId }
//   hsdb.state() -> { ready, backend: 'opfs'|'memory'|'failed' }
'use strict';

(function () {
    const DB_FILENAME = '/healthspan-local.sqlite3';
    const SCHEMA = `
        CREATE TABLE IF NOT EXISTS sync_queue (
            op_id TEXT PRIMARY KEY,
            entity TEXT NOT NULL,
            action TEXT NOT NULL,
            client_entity_id TEXT,
            payload TEXT NOT NULL,
            created_at_utc TEXT NOT NULL,
            attempts INTEGER NOT NULL DEFAULT 0,
            last_error TEXT
        );
        CREATE TABLE IF NOT EXISTS response_cache (
            cache_key TEXT PRIMARY KEY,
            status_code INTEGER NOT NULL,
            content_type TEXT NOT NULL,
            body TEXT NOT NULL,
            cached_at_utc TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS workouts (
            client_id TEXT PRIMARY KEY,
            server_id TEXT,
            date TEXT NOT NULL,
            type TEXT NOT NULL,
            updated_at_utc TEXT NOT NULL,
            deleted INTEGER NOT NULL DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS exercises (
            client_id TEXT PRIMARY KEY,
            server_id TEXT,
            workout_client_id TEXT NOT NULL,
            type_id INTEGER NOT NULL,
            name TEXT,
            series INTEGER,
            repetitions INTEGER,
            weight REAL,
            body_weight REAL,
            added_weight REAL,
            comments TEXT,
            tags TEXT NOT NULL DEFAULT '[]',
            updated_at_utc TEXT NOT NULL,
            deleted INTEGER NOT NULL DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS mirror_catalog (
            kind TEXT NOT NULL,
            item_id TEXT NOT NULL,
            payload TEXT NOT NULL,
            cached_at_utc TEXT NOT NULL,
            PRIMARY KEY (kind, item_id)
        );
        CREATE INDEX IF NOT EXISTS ix_exercises_workout ON exercises(workout_client_id);
        CREATE INDEX IF NOT EXISTS ix_workouts_date ON workouts(date);
    `;

    let dbPromise = null;

    async function openDb() {
        if (typeof globalThis.sqlite3InitModule !== 'function') {
            throw new Error('sqlite3InitModule is not loaded (js/sqlite/sqlite3.js missing?)');
        }
        const sqlite3 = await globalThis.sqlite3InitModule();
        const oo = sqlite3.oo1;
        if (oo && oo.OpfsDb) {
            const db = new oo.OpfsDb(DB_FILENAME, 'ct');
            return { db, capi: sqlite3.capi, backend: 'opfs' };
        }
        // OPFS unavailable (old browser): fall back to a transient in-memory
        // DB so the app keeps working for this session at least.
        console.warn('healthspan offline-db: OPFS VFS unavailable, using transient in-memory DB.');
        const db = new oo.DB(':memory:', 'ct');
        return { db, capi: sqlite3.capi, backend: 'memory' };
    }

    function ensureDb() {
        if (!dbPromise) {
            dbPromise = openDb()
                .then((handle) => {
                    handle.db.exec(SCHEMA);
                    return handle;
                })
                .catch((err) => {
                    console.error('healthspan offline-db: init failed', err);
                    dbPromise = null;
                    throw err;
                });
        }
        return dbPromise;
    }

    function run(handle, sql, bind) {
        return handle.db.exec({ sql, bind: bind || [], returnValue: 'resultRows' });
    }

    globalThis.hsdb = {
        ready: async function () {
            try {
                await ensureDb();
                return true;
            } catch (e) {
                return false;
            }
        },

        state: async function () {
            if (!dbPromise) return { ready: false, backend: 'none' };
            try {
                const handle = await dbPromise;
                return { ready: true, backend: handle.backend };
            } catch (e) {
                return { ready: false, backend: 'failed' };
            }
        },

        // SELECT-style: returns a JSON string with an array of row objects.
        exec: async function (sql, paramsJson) {
            const handle = await ensureDb();
            const rows = run(handle, sql, paramsJson ? JSON.parse(paramsJson) : []);
            return JSON.stringify(rows);
        },

        // INSERT/UPDATE/DELETE-style: returns { changes, lastInsertRowId }.
        changes: async function (sql, paramsJson) {
            const handle = await ensureDb();
            const bind = paramsJson ? JSON.parse(paramsJson) : [];
            const q = handle.db.prepare(sql);
            try {
                if (bind.length) q.bind(bind);
                q.step();
            } finally {
                q.finalize();
            }
            const capi = handle.capi;
            return {
                changes: capi.sqlite3_changes(handle.db.pointer),
                lastInsertRowId: capi.sqlite3_last_insert_rowid(handle.db.pointer)
            };
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
