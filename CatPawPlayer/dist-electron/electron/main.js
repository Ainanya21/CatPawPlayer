"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const electron_1 = require("electron");
const path_1 = require("path");
const axios_1 = require("axios");
const catVodEngine_1 = require("./catVodEngine");
// Enable Windows 11 Chromium GPU Hardware Acceleration Flags
electron_1.app.commandLine.appendSwitch('enable-gpu-rasterization');
electron_1.app.commandLine.appendSwitch('enable-zero-copy');
electron_1.app.commandLine.appendSwitch('ignore-gpu-blocklist');
electron_1.app.commandLine.appendSwitch('enable-features', 'VaapiVideoDecoder,CanvasOopRasterization,SmoothScrolling');
let mainWindow = null;
function createWindow() {
    mainWindow = new electron_1.BrowserWindow({
        width: 1280,
        height: 800,
        minWidth: 900,
        minHeight: 600,
        frame: false,
        titleBarStyle: 'hidden',
        webPreferences: {
            preload: path_1.default.join(__dirname, 'preload.js'),
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
    }
    else {
        mainWindow.loadFile(path_1.default.join(__dirname, '../dist/index.html'));
    }
    mainWindow.once('ready-to-show', () => {
        mainWindow?.show();
    });
    mainWindow.on('closed', () => {
        mainWindow = null;
    });
}
electron_1.app.whenReady().then(() => {
    createWindow();
    electron_1.app.on('activate', () => {
        if (electron_1.BrowserWindow.getAllWindows().length === 0)
            createWindow();
    });
});
electron_1.app.on('window-all-closed', () => {
    if (process.platform !== 'darwin')
        electron_1.app.quit();
});
// Window Controls
electron_1.ipcMain.handle('catvod:windowControl', (_, action) => {
    if (!mainWindow)
        return;
    if (action === 'minimize')
        mainWindow.minimize();
    if (action === 'maximize') {
        if (mainWindow.isMaximized())
            mainWindow.unmaximize();
        else
            mainWindow.maximize();
    }
    if (action === 'close')
        mainWindow.close();
});
// IPC Proxy API Handlers
electron_1.ipcMain.handle('catvod:fetchHome', async (_, site) => {
    return await (0, catVodEngine_1.fetchHome)(site);
});
electron_1.ipcMain.handle('catvod:fetchCategory', async (_, site, tid, page, extend) => {
    return await (0, catVodEngine_1.fetchCategory)(site, tid, page, extend);
});
electron_1.ipcMain.handle('catvod:fetchDetail', async (_, site, vodId) => {
    return await (0, catVodEngine_1.fetchDetail)(site, vodId);
});
electron_1.ipcMain.handle('catvod:fetchPlayUrl', async (_, site, flag, playId) => {
    return await (0, catVodEngine_1.fetchPlayUrl)(site, flag, playId);
});
electron_1.ipcMain.handle('catvod:fetchSearch', async (_, site, keyword, quick) => {
    return await (0, catVodEngine_1.fetchSearch)(site, keyword, quick);
});
// Multi-Source Concurrent Aggregate Cross-Site Search
electron_1.ipcMain.handle('catvod:fetchAggregateSearch', async (_, sites, keyword) => {
    const promises = sites.map(async (site) => {
        try {
            const list = await (0, catVodEngine_1.fetchSearch)(site, keyword);
            return {
                siteKey: site.key,
                siteName: site.name,
                siteType: site.type,
                list: list || [],
            };
        }
        catch (err) {
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
        .filter((v) => v !== null && v.list.length > 0);
});
// Auto-repair truncated or damaged JSON files (e.g. unclosed strings/brackets in XPTV AV.json)
function tryRepairJSON(str) {
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
electron_1.ipcMain.handle('catvod:fetchSubscription', async (_, subUrl) => {
    let targetUrl = subUrl.trim();
    if (targetUrl.endsWith('.md5') || targetUrl.endsWith('.js') || targetUrl.includes('/cat/')) {
        try {
            console.log('[Subscription] Detected CatPaw JS Spider Bundle URL, starting CatPaw microservice...');
            return await (0, catVodEngine_1.startCatPawServer)(targetUrl);
        }
        catch (err) {
            console.error('[Subscription] CatPaw JS Bundle execution failed:', err.message);
            throw new Error(`猫源脚本启动失败: ${err.message}`);
        }
    }
    try {
        const resp = await axios_1.default.get(targetUrl, { timeout: 15000, responseType: 'text' });
        let data = resp.data;
        if (typeof data === 'string' && (data.includes('var ') || data.includes('const ') || data.includes('function'))) {
            try {
                console.log('[Subscription] Content appears to be JS code, starting CatPaw microservice...');
                return await (0, catVodEngine_1.startCatPawServer)(targetUrl);
            }
            catch (e) {
                // Fallback to JSON parse
            }
        }
        if (typeof data === 'string') {
            try {
                data = JSON.parse(data);
            }
            catch (e) {
                try {
                    data = tryRepairJSON(data);
                    console.log('[Subscription] Successfully auto-repaired truncated JSON file!');
                }
                catch (eRep) {
                    try {
                        const decoded = Buffer.from(data, 'base64').toString('utf-8');
                        try {
                            data = JSON.parse(decoded);
                        }
                        catch (eDec) {
                            data = tryRepairJSON(decoded);
                        }
                    }
                    catch (e2) {
                        throw new Error('订阅文件损坏无法解析：格式不符合 JSON 规范。');
                    }
                }
            }
        }
        if (!data || !Array.isArray(data.sites)) {
            throw new Error('订阅文件中未包含有效的 sites 站点列表');
        }
        return data;
    }
    catch (err) {
        console.error('[Subscription Fetch Failed]', err.message);
        throw new Error(`无法获取或解析订阅文件: ${err.message}`);
    }
});
// Netdisk & Port API Handlers
electron_1.ipcMain.handle('catvod:getCatPawPort', async () => {
    return await (0, catVodEngine_1.getCatPawPort)();
});
electron_1.ipcMain.handle('catvod:getNetdiskCredentials', async () => {
    return await (0, catVodEngine_1.getNetdiskCredentials)();
});
electron_1.ipcMain.handle('catvod:startNetdiskQrLogin', async (_, provider) => {
    return await (0, catVodEngine_1.startNetdiskQrLogin)(provider);
});
electron_1.ipcMain.handle('catvod:pollNetdiskQrLogin', async (_, provider, taskId) => {
    return await (0, catVodEngine_1.pollNetdiskQrLogin)(provider, taskId);
});
electron_1.ipcMain.handle('catvod:saveNetdiskCookie', async (_, provider, cookie) => {
    return await (0, catVodEngine_1.saveNetdiskCookie)(provider, cookie);
});
