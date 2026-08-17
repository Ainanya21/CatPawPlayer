"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const electron_1 = require("electron");
electron_1.contextBridge.exposeInMainWorld('electronAPI', {
    fetchHome: (site) => electron_1.ipcRenderer.invoke('catvod:fetchHome', site),
    fetchCategory: (site, tid, page, extend) => electron_1.ipcRenderer.invoke('catvod:fetchCategory', site, tid, page, extend),
    fetchDetail: (site, vodId) => electron_1.ipcRenderer.invoke('catvod:fetchDetail', site, vodId),
    fetchPlayUrl: (site, flag, playId) => electron_1.ipcRenderer.invoke('catvod:fetchPlayUrl', site, flag, playId),
    fetchSearch: (site, keyword, quick) => electron_1.ipcRenderer.invoke('catvod:fetchSearch', site, keyword, quick),
    fetchAggregateSearch: (sites, keyword) => electron_1.ipcRenderer.invoke('catvod:fetchAggregateSearch', sites, keyword),
    fetchSubscription: (subUrl) => electron_1.ipcRenderer.invoke('catvod:fetchSubscription', subUrl),
    getCatPawPort: () => electron_1.ipcRenderer.invoke('catvod:getCatPawPort'),
    getNetdiskCredentials: () => electron_1.ipcRenderer.invoke('catvod:getNetdiskCredentials'),
    startNetdiskQrLogin: (provider) => electron_1.ipcRenderer.invoke('catvod:startNetdiskQrLogin', provider),
    pollNetdiskQrLogin: (provider, taskId) => electron_1.ipcRenderer.invoke('catvod:pollNetdiskQrLogin', provider, taskId),
    saveNetdiskCookie: (provider, cookie) => electron_1.ipcRenderer.invoke('catvod:saveNetdiskCookie', provider, cookie),
    windowControl: (action) => electron_1.ipcRenderer.invoke('catvod:windowControl', action),
});
