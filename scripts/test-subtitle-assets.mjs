// Adapter contract checks without microphone, browser, Twitch or translation requests.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';

const asset = name => fs.readFileSync(new URL(`../NeoTwitch/Features/Integrations/Assets/${name}`, import.meta.url), 'utf8');
const script = html => [...html.matchAll(/<script>([\s\S]*?)<\/script>/g)].map(m => m[1]).join('\n');
const flush = async () => { for (let i = 0; i < 8; i++) await new Promise(setImmediate); };
const elements = new Map();
const document = { getElementById: id => { if (!elements.has(id)) elements.set(id, { textContent: '', style: {} }); return elements.get(id); } };
const cfg = { sourceLanguage: 'es-CO', targetLanguage: 'en', translate: true, showOriginal: true, fontSize: 40, color: '#FFFFFF', durationSeconds: 8, placeAtTop: false };
let recognizer, fail = false;
const posts = [], heartbeats = [];
class Recognizer extends EventTarget {
  constructor(options) { super(); this.options = options; recognizer = this; this.stopped = false; }
  async start() { this.stopped = false; }
  stop() { this.stopped = true; }
}
const context = vm.createContext({ document, window: { addEventListener() {} },
  JimakuRecognizer: Recognizer, JimakuTranslator: class { async checkChrome() {} async preloadChrome() {} async translateOne(text) { return { ok: true, text: 'EN: ' + text }; } },
  fetch: async (url, options) => { if (fail) throw Error('offline'); if (url === 'config') return { json: async () => cfg };
    posts.push(JSON.parse(options.body)); return { ok: true }; },
  setInterval: fn => heartbeats.push(fn), console });
vm.runInContext(script(asset('captions.html')), context);
await flush();
assert.equal(recognizer.options.lang, 'es-CO');
await document.getElementById('start').onclick();
recognizer.dispatchEvent(new CustomEvent('final', { detail: { text: '<script>hola</script>' } }));
await flush();
assert.ok(posts.some(p => p.translation === 'EN: <script>hola</script>'));
assert.equal(document.getElementById('preview').textContent, '<script>hola</script>\nEN: <script>hola</script>');
document.getElementById('stop').onclick();
await flush();
assert.ok(recognizer.stopped);
assert.equal(posts.at(-1).original, '');
fail = true;
for (let i = 0; i < 3; i++) { await heartbeats[0](); await flush(); }
assert.match(document.getElementById('status').textContent, /detuvo la integración/);

const timers = [];
let state = { original: 'Voz', translation: 'Voice', updatedAt: Date.now() };
const overlay = vm.createContext({ document, Date, fetch: async url => ({ json: async () => url === 'config' ? cfg : state }), setTimeout: fn => timers.push(fn) });
vm.runInContext(script(asset('subtitle-overlay.html')), overlay);
await flush();
assert.equal(document.getElementById('original').textContent, 'Voz');
state = { ...state, updatedAt: 0 };
await timers.shift()(); await flush();
assert.equal(document.getElementById('original').textContent, '');
assert.equal(document.getElementById('translation').textContent, '');
console.log('Subtitle adapters: captions, translation, pause, disconnect watchdog, safe text and expiration PASS');
