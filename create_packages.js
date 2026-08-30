const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const rootDir = path.resolve(__dirname);
const outDir = path.join(rootDir, 'dist');
const stagingDir = path.join(outDir, 'staging');
fs.mkdirSync(outDir, { recursive: true });

// Clean previous staging if any
if (fs.existsSync(stagingDir)) {
  fs.rmSync(stagingDir, { recursive: true, force: true });
}
fs.mkdirSync(stagingDir, { recursive: true });

const srcDir = path.join(rootDir, 'CatPawPlayer.WinUI', 'bin', 'Release', 'net8.0-windows10.0.19041.0', 'win-x64');
const zipPath = path.join(outDir, 'CatPawPlayer_v2.1.3_Portable.zip');

// Allowed subdirectories (Essential WinUI assets + runtime + Chinese/English language packs)
const allowedDirPrefixes = [
  'assets', 'controls', 'pages', 'microsoft.ui.xaml', 'node',
  'zh-cn', 'zh-hans', 'zh-hant', 'zh-tw', 'en-us', 'en-gb'
];

const skippedExactFiles = new Set([
  'mscordbi.dll',
  'mscordaccore.dll',
  'mscordaccore_amd64_amd64_8.0.2926.32403.dll',
  'microsoft.diasymreader.native.amd64.dll'
]);

console.log('Staging optimized release files...');
for (const item of fs.readdirSync(srcDir)) {
  const itemPath = path.join(srcDir, item);
  const stat = fs.statSync(itemPath);
  const lowerItem = item.toLowerCase();

  // Skip debug symbols, build logs, Windows metadata files, and temporary compiler files
  if (
    lowerItem.endsWith('.pdb') ||
    lowerItem.endsWith('.winmd') ||
    lowerItem.endsWith('.binlog') ||
    lowerItem.endsWith('.log') ||
    lowerItem.endsWith('.iobj') ||
    lowerItem.endsWith('.ipdb') ||
    skippedExactFiles.has(lowerItem)
  ) {
    continue;
  }

  if (stat.isDirectory()) {
    // Only include allowed directories, skip 80+ unused foreign language satellite folders
    const isAllowed = allowedDirPrefixes.some(p => lowerItem === p || lowerItem.startsWith(p));
    if (!isAllowed) {
      continue;
    }
    copyDirRecursive(itemPath, path.join(stagingDir, item));
  } else {
    fs.copyFileSync(itemPath, path.join(stagingDir, item));
  }
}

function copyDirRecursive(src, dest) {
  fs.mkdirSync(dest, { recursive: true });
  for (const child of fs.readdirSync(src)) {
    const s = path.join(src, child);
    const d = path.join(dest, child);
    if (fs.statSync(s).isDirectory()) {
      copyDirRecursive(s, d);
    } else {
      fs.copyFileSync(s, d);
    }
  }
}

console.log('Compressing portable zip archive (Optimal)...');
if (fs.existsSync(zipPath)) fs.unlinkSync(zipPath);

execSync(`"C:\\Windows\\System32\\tar.exe" -a -c -f "${zipPath}" -C "${stagingDir}" .`);

// Clean staging
fs.rmSync(stagingDir, { recursive: true, force: true });

const st = fs.statSync(zipPath);
console.log('Created portable zip:', zipPath, 'Size MB:', (st.size / (1024 * 1024)).toFixed(2));
