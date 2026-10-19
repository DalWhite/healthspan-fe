// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/, /^staticwebapp\.config\.json$/ ];

async function onInstall(event) {
    console.info('Service worker: Install');
    // Activate immediately so a new deploy takes over on the next open.
    self.skipWaiting();

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Take control of open clients immediately.
    await self.clients.claim();

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // Navigation requests: NETWORK-FIRST so a new deploy is picked up on
        // the next online open (cache is the offline fallback only).
        // Fingerprinted static assets stay cache-first (safe: content-hashed).
        const isNavigation = event.request.mode === 'navigate';
        if (isNavigation) {
            try {
                return await fetch(event.request);
            } catch (networkError) {
                const cache = await caches.open(cacheName);
                cachedResponse = await cache.match('index.html');
                return cachedResponse || networkError;
            }
        }

        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(event.request);
    }

    return cachedResponse || fetch(event.request);
}
