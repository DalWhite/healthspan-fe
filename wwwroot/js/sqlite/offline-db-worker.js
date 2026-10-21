// healthspan offline-db worker: hosts sqlite3 + OPFS in a dedicated worker.
// OPFS requires createSyncAccessHandle + Atomics.wait, which only exist in
// worker threads under cross-origin isolation (COOP/COEP headers).
import sqlite3InitModule from './sqlite3-api.mjs';

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
        deleted INTEGER NOT NULL DEFAULT 0,
        goal TEXT,
        secondary_goals TEXT,
        modality TEXT,
        perceived_fatigue INTEGER
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
        deleted INTEGER NOT NULL DEFAULT 0,
        block_client_id TEXT,
        sort_order INTEGER NOT NULL DEFAULT 0,
        attempts INTEGER NOT NULL DEFAULT 1,
        completed_attempts INTEGER NOT NULL DEFAULT 1,
        quality_intent INTEGER NOT NULL DEFAULT 0,
        load_type TEXT
    );
    CREATE TABLE IF NOT EXISTS blocks (
        client_id TEXT PRIMARY KEY,
        server_id TEXT,
        workout_client_id TEXT NOT NULL,
        sort_order INTEGER NOT NULL DEFAULT 0,
        type TEXT NOT NULL DEFAULT 'Normal',
        label TEXT,
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
    CREATE INDEX IF NOT EXISTS ix_blocks_workout ON blocks(workout_client_id);
`;

// Databases created before the universal tracker lack the new columns. SQLite
// has no "ADD COLUMN IF NOT EXISTS", so each statement is attempted and the
// "duplicate column" error is swallowed: the migration is idempotent.
const MIGRATIONS = [
    'ALTER TABLE workouts ADD COLUMN goal TEXT',
    'ALTER TABLE workouts ADD COLUMN secondary_goals TEXT',
    'ALTER TABLE workouts ADD COLUMN modality TEXT',
    'ALTER TABLE workouts ADD COLUMN perceived_fatigue INTEGER',
    'ALTER TABLE exercises ADD COLUMN block_client_id TEXT',
    'ALTER TABLE exercises ADD COLUMN sort_order INTEGER NOT NULL DEFAULT 0',
    'ALTER TABLE exercises ADD COLUMN attempts INTEGER NOT NULL DEFAULT 1',
    'ALTER TABLE exercises ADD COLUMN completed_attempts INTEGER NOT NULL DEFAULT 1',
    'ALTER TABLE exercises ADD COLUMN quality_intent INTEGER NOT NULL DEFAULT 0',
    'ALTER TABLE exercises ADD COLUMN load_type TEXT',
];

let db = null;
let capi = null;
let backend = 'none';
let initPromise = null;

function normalizeValue(v) {
    if (typeof v === 'bigint') {
        return (v >= Number.MIN_SAFE_INTEGER && v <= Number.MAX_SAFE_INTEGER) ? Number(v) : String(v);
    }
    return v;
}

function normalizeRow(row) {
    const out = {};
    for (const k of Object.keys(row)) out[k] = normalizeValue(row[k]);
    return out;
}

async function init() {
    try {
        const sqlite3 = await sqlite3InitModule();
        capi = sqlite3.capi;
        const oo = sqlite3.oo1;
        if (oo && oo.OpfsDb) {
            db = new oo.OpfsDb(DB_FILENAME, 'ct');
            backend = 'opfs';
        } else {
            console.warn('healthspan offline-db worker: OPFS VFS unavailable, using transient in-memory DB.');
            db = new oo.DB(':memory:', 'ct');
            backend = 'memory';
        }
        db.exec(SCHEMA);
        for (const sql of MIGRATIONS) {
            try {
                db.exec(sql);
            } catch (migrationErr) {
                // 'duplicate column name' means the column already exists.
                const message = String((migrationErr && migrationErr.message) || migrationErr);
                if (!/duplicate column name/i.test(message)) {
                    throw migrationErr;
                }
            }
        }
        self.postMessage({ type: 'ready', backend });
    } catch (err) {
        console.error('healthspan offline-db worker: init failed', err);
        self.postMessage({ type: 'failed', error: String((err && err.message) || err) });
    }
}

function execRows(sql, bind) {
    const rows = db.exec({ sql, bind: bind || [], rowMode: 'object', returnValue: 'resultRows' });
    return (rows || []).map(normalizeRow);
}

self.onmessage = async (e) => {
    const { id, method, sql, bind } = e.data;
    try {
        if (!db) await initPromise;
        if (!db) throw new Error('offline-db worker init failed');
        let result;
        switch (method) {
            case 'exec':
                result = execRows(sql, bind);
                break;
            case 'changes': {
                const q = db.prepare(sql);
                try {
                    if (bind && bind.length) q.bind(bind);
                    q.step();
                } finally {
                    q.finalize();
                }
                result = {
                    changes: capi.sqlite3_changes(db.pointer),
                    lastInsertRowId: normalizeValue(capi.sqlite3_last_insert_rowid(db.pointer))
                };
                break;
            }
            case 'tx': {
                // statements: [{sql, bind}] applied in one transaction:
                // one OPFS sync for the whole batch instead of one per row.
                let total = 0;
                db.exec('BEGIN');
                try {
                    for (const st of (e.data.statements || [])) {
                        const q = db.prepare(st.sql);
                        try {
                            if (st.bind && st.bind.length) q.bind(st.bind);
                            q.step();
                        } finally {
                            q.finalize();
                        }
                        total += capi.sqlite3_changes(db.pointer);
                    }
                    db.exec('COMMIT');
                } catch (inner) {
                    try { db.exec('ROLLBACK'); } catch (_) { /* ignore */ }
                    throw inner;
                }
                result = { changes: total };
                break;
            }
            default:
                throw new Error('unknown method ' + method);
        }
        self.postMessage({ id, result });
    } catch (err) {
        self.postMessage({ id, error: String((err && err.message) || err) });
    }
};

initPromise = init();
