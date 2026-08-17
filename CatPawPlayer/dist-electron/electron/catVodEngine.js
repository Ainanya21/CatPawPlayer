"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.getCatPawPort = getCatPawPort;
exports.startCatPawServer = startCatPawServer;
exports.fetchHome = fetchHome;
exports.fetchCategory = fetchCategory;
exports.fetchDetail = fetchDetail;
exports.fetchPlayUrl = fetchPlayUrl;
exports.fetchSearch = fetchSearch;
exports.getNetdiskCredentials = getNetdiskCredentials;
exports.startNetdiskQrLogin = startNetdiskQrLogin;
exports.pollNetdiskQrLogin = pollNetdiskQrLogin;
exports.saveNetdiskCookie = saveNetdiskCookie;
const axios_1 = require("axios");
let currentCatPawPort = 9988;
async function getCatPawPort() {
    return currentCatPawPort;
}
async function startCatPawServer(spiderUrl) {
    try {
        const res = await axios_1.default.post(`http://127.0.0.1:${currentCatPawPort}/init`, { url: spiderUrl }, { timeout: 15000 });
        return res.data;
    }
    catch (e) {
        // If microservice isn't running, return default fallback config
        return {
            sites: [
                { key: 'wogg', name: '玩偶4K', type: 3, api: 'csp_Wogg' },
                { key: 'zhizhen', name: '至臻4K', type: 3, api: 'csp_Zhizhen' },
                { key: 'feisu', name: '飞速资源', type: 1, api: 'https://www.feisuzy.com/api.php/provide/vod/' },
            ],
        };
    }
}
async function fetchHome(site) {
    if (site.type === 3) {
        try {
            const res = await axios_1.default.post(`http://127.0.0.1:${currentCatPawPort}/spider/${site.key}/3/home`, {}, { timeout: 15000 });
            return res.data;
        }
        catch (e) {
            return { list: [], class: [] };
        }
    }
    // Standard CMS Type 0/1/2
    try {
        const res = await axios_1.default.get(site.api, {
            params: { ac: 'detail' },
            timeout: 10000,
        });
        return res.data;
    }
    catch (e) {
        return { list: [], class: [] };
    }
}
async function fetchCategory(site, tid, page, extend) {
    if (site.type === 3) {
        try {
            const res = await axios_1.default.post(`http://127.0.0.1:${currentCatPawPort}/spider/${site.key}/3/category`, { tid, pg: page, extend }, { timeout: 15000 });
            return res.data;
        }
        catch (e) {
            return { list: [] };
        }
    }
    try {
        const res = await axios_1.default.get(site.api, {
            params: { ac: 'detail', t: tid, pg: page, ...extend },
            timeout: 10000,
        });
        return res.data;
    }
    catch (e) {
        return { list: [] };
    }
}
async function fetchDetail(site, vodId) {
    if (site.type === 3) {
        try {
            const res = await axios_1.default.post(`http://127.0.0.1:${currentCatPawPort}/spider/${site.key}/3/detail`, { ids: [vodId] }, { timeout: 15000 });
            if (res.data && res.data.list && res.data.list[0]) {
                return res.data.list[0];
            }
            return null;
        }
        catch (e) {
            return null;
        }
    }
    try {
        const res = await axios_1.default.get(site.api, {
            params: { ac: 'detail', ids: vodId },
            timeout: 10000,
        });
        return res.data?.list?.[0] || null;
    }
    catch (e) {
        return null;
    }
}
async function fetchPlayUrl(site, flag, playId) {
    if (site.type === 3) {
        try {
            const res = await axios_1.default.post(`http://127.0.0.1:${currentCatPawPort}/spider/${site.key}/3/player`, { flag, id: playId }, { timeout: 15000 });
            return res.data;
        }
        catch (e) {
            return { parse: 0, url: playId };
        }
    }
    return { parse: 0, url: playId };
}
async function fetchSearch(site, keyword, quick) {
    if (site.type === 3) {
        try {
            const res = await axios_1.default.post(`http://127.0.0.1:${currentCatPawPort}/spider/${site.key}/3/search`, { key: keyword, quick: quick ? 1 : 0 }, { timeout: 15000 });
            return res.data?.list || [];
        }
        catch (e) {
            return [];
        }
    }
    try {
        const res = await axios_1.default.get(site.api, {
            params: { ac: 'detail', wd: keyword },
            timeout: 10000,
        });
        return res.data?.list || [];
    }
    catch (e) {
        return [];
    }
}
async function getNetdiskCredentials() {
    try {
        const res = await axios_1.default.get(`http://127.0.0.1:${currentCatPawPort}/netdisk/credentials`, { timeout: 5000 });
        return res.data;
    }
    catch (e) {
        return null;
    }
}
async function startNetdiskQrLogin(provider) {
    try {
        const res = await axios_1.default.post(`http://127.0.0.1:${currentCatPawPort}/netdisk/qr/start`, { provider }, { timeout: 10000 });
        return res.data;
    }
    catch (e) {
        return { code: -1, msg: '微服务网络未挂载' };
    }
}
async function pollNetdiskQrLogin(provider, taskId) {
    try {
        const res = await axios_1.default.post(`http://127.0.0.1:${currentCatPawPort}/netdisk/qr/poll`, { provider, taskId }, { timeout: 10000 });
        return res.data;
    }
    catch (e) {
        return { code: -1, msg: '轮询轮退' };
    }
}
async function saveNetdiskCookie(provider, cookie) {
    try {
        const res = await axios_1.default.post(`http://127.0.0.1:${currentCatPawPort}/netdisk/cookie/save`, { provider, cookie }, { timeout: 5000 });
        return res.data;
    }
    catch (e) {
        return null;
    }
}
