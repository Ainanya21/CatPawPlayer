const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const rootDir = path.resolve(__dirname);
const outDir = path.join(rootDir, 'dist');
fs.mkdirSync(outDir, { recursive: true });

const srcDir = path.join(rootDir, 'CatPawPlayer.WinUI', 'bin', 'Release', 'net8.0-windows10.0.19041.0', 'win-x64');
const zipPath = path.join(outDir, 'CatPawPlayer_v1.0.2_Portable.zip');

console.log('Creating portable zip archive with tar...');
execSync(`"C:\\Windows\\System32\\tar.exe" -a -c -f "${zipPath}" -C "${srcDir}" .`);

const st = fs.statSync(zipPath);
console.log('Created portable zip:', zipPath, 'Size MB:', (st.size / (1024 * 1024)).toFixed(2));
