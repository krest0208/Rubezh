// Data migration only. Never evaluates the incomplete game or browser code.
import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import crypto from 'node:crypto';
import {gzipSync} from 'node:zlib';
const root = path.resolve(import.meta.dirname, '..');
const names = fs.readdirSync(path.join(root, 'game_parts')).filter(n => /^part_\d+\.part$/.test(n)).sort();
const bytes = Buffer.concat(names.map(n => fs.readFileSync(path.join(root, 'game_parts', n))));
const source = bytes.toString('utf8');
function literal(name) {
  const start = source.indexOf(`const ${name} = `);
  if (start < 0) throw new Error(`Missing ${name}`);
  const offset = start + `const ${name} = `.length;
  let depth = 0, quote = '', escape = false;
  for (let i = offset; i < source.length; i++) {
    const c = source[i];
    if (quote) { if (escape) escape = false; else if (c === '\\') escape = true; else if (c === quote) quote = ''; continue; }
    if (c === '"' || c === "'") { quote = c; continue; }
    if (c === '[' || c === '{') depth++;
    if (c === ']' || c === '}') { depth--; if (depth === 0) return source.slice(offset, i + 1); }
  }
  throw new Error(`Incomplete literal ${name}`);
}
const out = path.join(root, 'native', 'Game', 'Data');
fs.mkdirSync(out, {recursive:true});
function save(name, value) {
  const json=JSON.stringify(value)+'\n';
  fs.writeFileSync(path.join(out,name+'.json'),json);
  if(name.startsWith('campaign.')) fs.writeFileSync(path.join(out,name+'.json.gz'),gzipSync(json,{level:9}));
}
const ru = JSON.parse(literal('baseCampaign'));
const translations = JSON.parse(literal('campaignLocales'));
for (const [lang, campaign] of Object.entries({ru, ...translations})) {
  if (campaign.missions.length !== 1000) throw new Error(`${lang}: expected 1000 missions`);
  save('campaign.' + lang, campaign);
}
// Evaluate only isolated data literals, with L as an identity function and a timeout.
for (const name of ['TOWERS','ENEMIES','RESEARCH','THEMES','PATHS','ADVANCED_PATHS']) {
  save(name.toLowerCase(), vm.runInNewContext('(' + literal(name) + ')', {L:x=>x}, {timeout:1000}));
}
save('provenance', {sourceCommit:'71e0b11c4b0329de468649b31a664d57f733cb31', chunks:names,
  incompleteSourceSha256:crypto.createHash('sha256').update(bytes).digest('hex'),
  notes:'Exact data literals recovered from the incomplete v12 source. Combat, UI, saves and models are new implementations. PATHS includes the five literal plans; eight advanced plans are preserved. Missing source was not reconstructed.'});
console.log('Extracted 6 × 1000 mission records, tower/enemy/research stats, themes and route plans.');
