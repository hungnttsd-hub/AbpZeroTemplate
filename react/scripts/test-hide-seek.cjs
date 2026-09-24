const fs = require('node:fs');
const { spawnSync } = require('node:child_process');
fs.writeFileSync('.hide-seek-tests/package.json', '{"type":"commonjs"}');
for (const args of [['.hide-seek-tests/tests/hide-seek/rules.test.js'], ['--test', 'tests/hide-seek.test.cjs']]) {
  const result = spawnSync(process.execPath, args, { stdio: 'inherit' });
  if (result.status !== 0) process.exit(result.status ?? 1);
}
