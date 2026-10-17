import { rmSync } from "node:fs";
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const project = join(root, '..', 'src', 'Chemistry', 'ReactionLab.Chemistry.Browser');
const output = join(root, 'chemistry-wasm');

for (const folder of [output, join(project, 'obj', 'Release'), join(project, 'bin', 'Release')]) {
    rmSync(folder, { recursive: true, force: true });
}

execFileSync('dotnet', ['publish', project, '-c', 'Release', '-p:RunAOTCompilation=true', '-o', output], {stdio: 'inherit' });