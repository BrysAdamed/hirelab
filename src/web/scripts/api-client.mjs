import { spawnSync } from 'node:child_process';
import { mkdirSync, readdirSync, rmSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const webRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const apiProject = resolve(webRoot, '../HireLab.Api/HireLab.Api.csproj');
const clientRoot = join(webRoot, 'projects/api-client');
const documentsDir = join(clientRoot, 'openapi');
const generatorCli = join(webRoot, 'node_modules/openapi-typescript/bin/cli.js');

const entryPoints = ['staff', 'candidate'];

const documentPath = (entryPoint) => join(documentsDir, `HireLab.Api_${entryPoint}.json`);
const schemaPath = (entryPoint) => join(clientRoot, 'src', entryPoint, 'schema.ts');

function run(command, args, options = {}) {
  const result = spawnSync(command, args, { cwd: webRoot, stdio: 'inherit', ...options });
  if (result.error) {
    throw result.error;
  }
  if (result.status !== 0) {
    process.exit(result.status ?? 1);
  }
  return result;
}

function exportDocuments() {
  mkdirSync(documentsDir, { recursive: true });
  for (const file of readdirSync(documentsDir).filter((name) => name.endsWith('.json'))) {
    rmSync(join(documentsDir, file));
  }

  run('dotnet', [
    'build',
    apiProject,
    '--no-incremental',
    '--nologo',
    '-v',
    'q',
    '-p:OpenApiGenerateDocumentsOnBuild=true',
  ]);
}

function generateClient() {
  for (const entryPoint of entryPoints) {
    run(process.execPath, [generatorCli, documentPath(entryPoint), '-o', schemaPath(entryPoint)]);
  }
}

function checkForDrift() {
  const paths = ['--', 'projects/api-client'];
  const status = run('git', ['status', '--porcelain', '--untracked-files=all', ...paths], {
    stdio: ['ignore', 'pipe', 'inherit'],
    encoding: 'utf8',
  });

  if (status.stdout.trim() === '') {
    console.log('API client is up to date.');
    return;
  }

  console.error('The committed API client differs from what the current API produces:');
  console.error(status.stdout);
  spawnSync('git', ['--no-pager', 'diff', '--stat', ...paths], { cwd: webRoot, stdio: 'inherit' });
  console.error('Run `npm run api:update` in src/web and commit the result.');
  process.exit(1);
}

const commands = {
  generate: () => generateClient(),
  update: () => {
    exportDocuments();
    generateClient();
  },
  check: () => {
    exportDocuments();
    generateClient();
    checkForDrift();
  },
};

const command = commands[process.argv[2]];
if (!command) {
  console.error(`Usage: node scripts/api-client.mjs <${Object.keys(commands).join('|')}>`);
  process.exit(2);
}
command();
