const fs = require('fs');
const path = require('path');
const http = require('http');
const https = require('https');
const crypto = require('crypto');

const baseDir = __dirname;
const erxiaoJs = path.join(baseDir, 'cat_spider_server.js');
const erxiaoMd5File = path.join(baseDir, 'cat_spider_server.js.md5');
const douerJs = path.join(baseDir, 'douer_spider_server.js');
const douerMd5File = path.join(baseDir, 'douer_spider_server.js.md5');

// Global flags required by CatVod engines
global.catServerFactory = undefined;
global.catDartServer = undefined;
global.catDartServerPort = undefined;

let erxiaoStarted = false;
let douerStarted = false;

// Dynamic custom spider engines registry: { [hash]: { port, app, started, md5 } }
const dynamicEngines = {};
let nextDynamicPort = 3100;

// Helper: HTTP request with Basic Auth, redirect following & User-Agent
function fetchHttp(targetUrl) {
  return new Promise((resolve, reject) => {
    try {
      const parsed = new URL(targetUrl);
      const isHttps = parsed.protocol === 'https:';
      const client = isHttps ? https : http;

      const options = {
        hostname: parsed.hostname,
        port: parsed.port || (isHttps ? 443 : 80),
        path: parsed.pathname + parsed.search,
        method: 'GET',
        headers: {
          'User-Agent': 'okhttp/4.9.0',
          'Accept': '*/*'
        }
      };

      if (parsed.username || parsed.password) {
        const auth = Buffer.from(`${parsed.username}:${parsed.password}`).toString('base64');
        options.headers['Authorization'] = `Basic ${auth}`;
      }

      const req = client.request(options, (res) => {
        if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
          const redirectUrl = new URL(res.headers.location, targetUrl).toString();
          return fetchHttp(redirectUrl).then(resolve).catch(reject);
        }

        let data = '';
        res.on('data', chunk => data += chunk);
        res.on('end', () => resolve({ status: res.statusCode, headers: res.headers, body: data }));
      });

      req.on('error', reject);
      req.setTimeout(15000, () => {
        req.destroy();
        reject(new Error('HTTP request timeout'));
      });
      req.end();
    } catch (e) {
      reject(e);
    }
  });
}

function getLocalFileMd5(filePath) {
  try {
    if (!fs.existsSync(filePath)) return '';
    const content = fs.readFileSync(filePath);
    return crypto.createHash('md5').update(content).digest('hex').toLowerCase();
  } catch {
    return '';
  }
}

// 1. Start Erxiao Engine (Port 9988) with auto-update
async function startErxiao(forceCheck = false) {
  try {
    let needsDownload = !fs.existsSync(erxiaoJs);
    if (!needsDownload && (forceCheck || !erxiaoStarted)) {
      try {
        const md5Resp = await fetchHttp('https://9280.kstore.vip/cat/index.js.md5');
        const remoteMd5 = md5Resp.body.trim().toLowerCase();
        const localMd5 = getLocalFileMd5(erxiaoJs);
        if (remoteMd5 && remoteMd5.length === 32 && remoteMd5 !== localMd5) {
          console.log(`[SpiderHost] Erxiao remote update detected (Remote: ${remoteMd5}, Local: ${localMd5}). Downloading...`);
          needsDownload = true;
        }
      } catch (e) {
        console.log('[SpiderHost] Erxiao update check failed (offline or network issue):', e.message);
      }
    }

    if (needsDownload) {
      console.log('[SpiderHost] Downloading latest Erxiao script...');
      const resp = await fetchHttp('https://9280.kstore.vip/cat/index.js');
      if (resp.body && resp.body.length > 5000) {
        fs.writeFileSync(erxiaoJs, resp.body);
        console.log('[SpiderHost] Erxiao script updated successfully, size:', resp.body.length);
      }
    }

    if (!erxiaoStarted) {
      const erxiaoApp = require(erxiaoJs);
      if (typeof erxiaoApp.start === 'function') {
        console.log('[SpiderHost] Starting Erxiao spider on port 9988...');
        await erxiaoApp.start(9988);
        erxiaoStarted = true;
        console.log('[SpiderHost] Erxiao spider started successfully on http://127.0.0.1:9988');
      }
    }
  } catch (err) {
    console.error('[SpiderHost] Erxiao start error:', err.message);
  }
}

// 2. Start Douer Engine (Port 2333) with auto-update
async function startDouer(forceCheck = false) {
  try {
    let needsDownload = !fs.existsSync(douerJs);
    if (!needsDownload && (forceCheck || !douerStarted)) {
      try {
        const md5Resp = await fetchHttp('https://woleigedouer:woleigedouer@catpaw.douer.me/index.js.md5');
        const remoteMd5 = md5Resp.body.trim().toLowerCase();
        const localMd5 = getLocalFileMd5(douerJs);
        if (remoteMd5 && remoteMd5.length === 32 && remoteMd5 !== localMd5) {
          console.log(`[SpiderHost] Douer remote update detected (Remote: ${remoteMd5}, Local: ${localMd5}). Downloading...`);
          needsDownload = true;
        }
      } catch (e) {
        console.log('[SpiderHost] Douer update check failed:', e.message);
      }
    }

    if (needsDownload) {
      console.log('[SpiderHost] Downloading latest Douer script...');
      const resp = await fetchHttp('https://woleigedouer:woleigedouer@catpaw.douer.me/index.js');
      if (resp.body && resp.body.length > 5000) {
        fs.writeFileSync(douerJs, resp.body);
        console.log('[SpiderHost] Douer script updated successfully, size:', resp.body.length);
      }
    }

    if (!douerStarted) {
      const douerApp = require(douerJs);
      if (typeof douerApp.start === 'function') {
        console.log('[SpiderHost] Starting Douer spider on port 2333...');
        await douerApp.start(2333);
        douerStarted = true;
        console.log('[SpiderHost] Douer spider started successfully on http://127.0.0.1:2333');
      }
    }
  } catch (err) {
    console.error('[SpiderHost] Douer start error:', err.message);
  }
}

// 3. Start Control Gateway (Port 9980) for subscription loading
const gateway = http.createServer(async (req, res) => {
  const reqUrl = new URL(req.url, 'http://127.0.0.1:9980');
  const pathname = reqUrl.pathname;

  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', '*');

  if (req.method === 'OPTIONS') {
    res.writeHead(200);
    res.end();
    return;
  }

  if (pathname === '/subscription/load') {
    let body = '';
    req.on('data', d => body += d);
    req.on('end', async () => {
      try {
        let subUrl = reqUrl.searchParams.get('url') || '';
        if (!subUrl && body) {
          try { subUrl = JSON.parse(body).url; } catch { subUrl = body.trim(); }
        }

        console.log('[ControlGateway] Loading subscription:', subUrl);

        // Case A: Douer Subscription
        if (subUrl.includes('douer') || subUrl.includes('catpaw.douer.me')) {
          await startDouer(true);
          const resp = await fetchHttp('http://127.0.0.1:2333/config');
          const json = JSON.parse(resp.body);
          const rawSites = json.video?.sites || json.sites || [];
          const sites = rawSites.map(s => ({
            ...s,
            apiBase: 'http://127.0.0.1:2333'
          }));
          res.writeHead(200, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({ sites }));
          return;
        }

        // Case B: Erxiao Subscription
        if (subUrl.includes('9280.kstore.vip') || subUrl.includes('cat') || subUrl.includes('二小')) {
          await startErxiao(true);
          const resp = await fetchHttp('http://127.0.0.1:9988/config');
          const json = JSON.parse(resp.body);
          const rawSites = json.video?.sites || json.sites || [];
          const sites = rawSites.map(s => ({
            ...s,
            apiBase: 'http://127.0.0.1:9988'
          }));
          res.writeHead(200, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({ sites }));
          return;
        }

        // Case C: Standard JSON, Base64 JSON, or Dynamic JS Spider Subscription
        try {
          const fetchRes = await fetchHttp(subUrl);
          let rawText = fetchRes.body.trim();

          // 1. Try parsing JSON directly
          let json = null;
          try { json = JSON.parse(rawText); } catch { }

          // 2. If not JSON, try Base64 decode (FongMi / TVBox base64 configs)
          if (!json && (rawText.startsWith('ey') || rawText.length > 50)) {
            try {
              const decoded = Buffer.from(rawText, 'base64').toString('utf8');
              json = JSON.parse(decoded);
            } catch { }
          }

          if (json && (json.sites || json.video?.sites)) {
            const rawSites = json.video?.sites || json.sites || [];
            const sites = rawSites.map(s => ({
              ...s,
              apiBase: 'http://127.0.0.1:9988'
            }));
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ sites }));
            return;
          }

          // 3. If .md5 or .js CatVod spider bundle
          if (subUrl.endsWith('.md5') || subUrl.endsWith('.js') || rawText.includes('websiteBundle') || rawText.includes('catServerFactory')) {
            let jsUrl = subUrl;
            if (subUrl.endsWith('.md5')) {
              jsUrl = subUrl.slice(0, -4);
            }

            let jsContent = rawText;
            if (subUrl.endsWith('.md5')) {
              const jsResp = await fetchHttp(jsUrl);
              jsContent = jsResp.body;
            }

            if (jsContent.length > 5000) {
              const hash = Buffer.from(subUrl).toString('hex').slice(0, 10);
              const customPath = path.join(baseDir, `custom_spider_${hash}.js`);
              fs.writeFileSync(customPath, jsContent);

              let engineInfo = dynamicEngines[hash];
              let assignedPort;
              if (!engineInfo) {
                assignedPort = nextDynamicPort++;
                const customApp = require(customPath);
                if (typeof customApp.start === 'function') {
                  console.log(`[SpiderHost] Starting dynamic spider engine on port ${assignedPort}...`);
                  await customApp.start(assignedPort);
                  dynamicEngines[hash] = { port: assignedPort, app: customApp, started: true };
                }
              } else {
                assignedPort = engineInfo.port;
              }

              const cfgResp = await fetchHttp(`http://127.0.0.1:${assignedPort}/config`);
              const cfgJson = JSON.parse(cfgResp.body);
              const rawSites = cfgJson.video?.sites || cfgJson.sites || [];
              const sites = rawSites.map(s => ({
                ...s,
                apiBase: `http://127.0.0.1:${assignedPort}`
              }));

              res.writeHead(200, { 'Content-Type': 'application/json' });
              res.end(JSON.stringify({ sites }));
              return;
            }
          }

          // 4. Single Direct CMS Site URL
          if (subUrl.includes('api.php') || subUrl.includes('/vod')) {
            const singleSite = [{
              key: 'custom_cms_' + Buffer.from(subUrl).toString('hex').slice(0, 6),
              name: '自定义源: ' + (reqUrl.hostname || 'CMS影视'),
              type: 1,
              api: subUrl,
              searchable: 1,
              quickSearch: 1,
              filterable: 1,
              apiBase: 'http://127.0.0.1:9988'
            }];
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ sites: singleSite }));
            return;
          }

          res.writeHead(200, { 'Content-Type': 'application/json' });
          res.end(rawText);
        } catch (err) {
          res.writeHead(500, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({ error: err.message }));
        }
      } catch (err) {
        res.writeHead(500, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ error: err.message }));
      }
    });
    return;
  }

  res.writeHead(404);
  res.end();
});

async function main() {
  gateway.listen(9980, '127.0.0.1', () => {
    console.log('[SpiderHost] Subscription Control Gateway listening on http://127.0.0.1:9980');
  });

  // Start primary Erxiao and Douer engines concurrently with MD5 auto-checks
  await startErxiao(true);
  await startDouer(true);
}

main().catch(err => console.error('[SpiderHost Main Error]', err));
