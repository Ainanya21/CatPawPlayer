import { app, BrowserWindow, ipcMain } from 'electron';
import path from 'path';
import axios from 'axios';
import {
  startCatPawServer,
  getCatPawPort,
  getNetdiskCredentials,
  startNetdiskQrLogin,
  pollNetdiskQrLogin,
  saveNetdiskCookie,
  fetchHome,
  fetchCategory,
  fetchDetail,
  fetchPlayUrl,
  fetchSearch,
} from './catVodEngine';
import { SiteSource, SubscriptionConfig } from '../src/types';

// Enable Windows 11 Chromium GPU Hardware Acceleration Flags
app.commandLine.appendSwitch('enable-gpu-rasterization');
app.commandLine.appendSwitch('enable-zero-copy');
app.commandLine.appendSwitch('ignore-gpu-blocklist');
app.commandLine.appendSwitch('enable-features', 'VaapiVideoDecoder,CanvasOopRasterization,SmoothScrolling');

let mainWindow: BrowserWindow | null = null;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: 900,
    minHeight: 600,
    frame: false,
    titleBarStyle: 'hidden',
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      nodeIntegration: false,
      contextIsolation: true,
      webSecurity: false, // Bypass CORS for video streaming
    },
    backgroundColor: '#f1f5f9', // Windows native light theme canvas (eliminates startup black flash)
    show: false,
  });

  const isDev = process.env.NODE_ENV === 'development' || process.env.VITE_DEV_SERVER_URL;

  if (isDev && process.env.VITE_DEV_SERVER_URL) {
    mainWindow.loadURL(process.env.VITE_DEV_SERVER_URL);
    mainWindow.webContents.openDevTools({ mode: 'detach' });
  } else {
    mainWindow.loadFile(path.join(__dirname, '../dist/index.html'));
  }

  mainWindow.once('ready-to-show', () => {
    mainWindow?.show();
  });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

app.whenReady().then(() => {
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});

// Window Controls
ipcMain.handle('catvod:windowControl', (_, action: 'minimize' | 'maximize' | 'close') => {
  if (!mainWindow) return;
  if (action === 'minimize') mainWindow.minimize();
  if (action === 'maximize') {
    if (mainWindow.isMaximized()) mainWindow.unmaximize();
    else mainWindow.maximize();
  }
  if (action === 'close') mainWindow.close();
});

// IPC Proxy API Handlers
ipcMain.handle('catvod:fetchHome', async (_, site: SiteSource) => {
  return await fetchHome(site);
});

ipcMain.handle('catvod:fetchCategory', async (_, site: SiteSource, tid: string, page: number, extend?: any) => {
  return await fetchCategory(site, tid, page, extend);
});

ipcMain.handle('catvod:fetchDetail', async (_, site: SiteSource, vodId: string) => {
  return await fetchDetail(site, vodId);
});

ipcMain.handle('catvod:fetchPlayUrl', async (_, site: SiteSource, flag: string, playId: string) => {
  return await fetchPlayUrl(site, flag, playId);
});

ipcMain.handle('catvod:fetchSearch', async (_, site: SiteSource, keyword: string, quick?: boolean) => {
  return await fetchSearch(site, keyword, quick);
});

// Multi-Source Concurrent Aggregate Cross-Site Search
ipcMain.handle('catvod:fetchAggregateSearch', async (_, sites: SiteSource[], keyword: string) => {
  const promises = sites.map(async (site) => {
    try {
      const list = await fetchSearch(site, keyword);
      return {
        siteKey: site.key,
        siteName: site.name,
        siteType: site.type,
        list: list || [],
      };
    } catch (err) {
      return {
        siteKey: site.key,
        siteName: site.name,
        siteType: site.type,
        list: [],
      };
    }
  });

  const results = await Promise.allSettled(promises);
  return results
    .map((r) => (r.status === 'fulfilled' ? r.value : null))
    .filter((v): v is { siteKey: string; siteName: string; siteType: number; list: any[] } => v !== null && v.list.length > 0);
});

// Auto-repair truncated or damaged JSON files (e.g. unclosed strings/brackets in XPTV AV.json)
function tryRepairJSON(str: string): any {
  let cleaned = str.trim();
  cleaned = cleaned.replace(/,\s*"[^"]*":\s*"[^"]*$/s, '');
  cleaned = cleaned.replace(/,\s*"[^"]*":\s*$/s, '');
  cleaned = cleaned.replace(/,\s*"[^"]*"$/s, '');
  cleaned = cleaned.replace(/,\s*\{[^{}]*$/s, '');

  let openBraces = (cleaned.match(/\{/g) || []).length - (cleaned.match(/\}/g) || []).length;
  let openBrackets = (cleaned.match(/\[/g) || []).length - (cleaned.match(/\]/g) || []).length;

  while (openBrackets > 0) {
    cleaned += ']';
    openBrackets--;
  }
  while (openBraces > 0) {
    cleaned += '}';
    openBraces--;
  }

  return JSON.parse(cleaned);
}

ipcMain.handle('catvod:fetchSubscription', async (_, subUrl: string): Promise<SubscriptionConfig> => {
  let targetUrl = subUrl.trim();

  if (targetUrl.endsWith('.md5') || targetUrl.endsWith('.js') || targetUrl.includes('/cat/')) {
    try {
      console.log('[Subscription] Detected CatPaw JS Spider Bundle URL, starting CatPaw microservice...');
      return await startCatPawServer(targetUrl);
    } catch (err: any) {
      console.error('[Subscription] CatPaw JS Bundle execution failed:', err.message);
      throw new Error(`猫源脚本启动失败: ${err.message}`);
    }
  }

  try {
    const resp = await axios.get(targetUrl, { timeout: 15000, responseType: 'text' });
    let data = resp.data;

    if (typeof data === 'string' && (data.includes('var ') || data.includes('const ') || data.includes('function'))) {
      try {
        console.log('[Subscription] Content appears to be JS code, starting CatPaw microservice...');
        return await startCatPawServer(targetUrl);
      } catch (e: any) {
        // Fallback to JSON parse
      }
    }

    if (typeof data === 'string') {
      try {
        data = JSON.parse(data);
      } catch (e) {
        try {
          data = tryRepairJSON(data);
          console.log('[Subscription] Successfully auto-repaired truncated JSON file!');
        } catch (eRep) {
          try {
            const decoded = Buffer.from(data, 'base64').toString('utf-8');
            try {
              data = JSON.parse(decoded);
            } catch (eDec) {
              data = tryRepairJSON(decoded);
            }
          } catch (e2) {
            throw new Error('订阅文件损坏无法解析：格式不符合 JSON 规范。');
          }
        }
      }
    }

    if (!data || !Array.isArray(data.sites)) {
      throw new Error('订阅文件中未包含有效的 sites 站点列表');
    }

    return data;
  } catch (err: any) {
    console.error('[Subscription Fetch Failed]', err.message);
    throw new Error(`无法获取或解析订阅文件: ${err.message}`);
  }
});

// Netdisk & Port API Handlers
ipcMain.handle('catvod:getCatPawPort', async () => {
  return await getCatPawPort();
});

ipcMain.handle('catvod:getNetdiskCredentials', async () => {
  return await getNetdiskCredentials();
});

ipcMain.handle('catvod:startNetdiskQrLogin', async (_, provider: string) => {
  return await startNetdiskQrLogin(provider);
});

ipcMain.handle('catvod:pollNetdiskQrLogin', async (_, provider: string, taskId?: string) => {
  return await pollNetdiskQrLogin(provider, taskId);
});

ipcMain.handle('catvod:saveNetdiskCookie', async (_, provider: string, cookie: string) => {
  return await saveNetdiskCookie(provider, cookie);
});
