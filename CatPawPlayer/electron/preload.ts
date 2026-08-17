import { contextBridge, ipcRenderer } from 'electron';
import { SiteSource } from '../src/types';

contextBridge.exposeInMainWorld('electronAPI', {
  fetchHome: (site: SiteSource) => ipcRenderer.invoke('catvod:fetchHome', site),
  fetchCategory: (site: SiteSource, tid: string, page: number, extend?: any) =>
    ipcRenderer.invoke('catvod:fetchCategory', site, tid, page, extend),
  fetchDetail: (site: SiteSource, vodId: string) => ipcRenderer.invoke('catvod:fetchDetail', site, vodId),
  fetchPlayUrl: (site: SiteSource, flag: string, playId: string) =>
    ipcRenderer.invoke('catvod:fetchPlayUrl', site, flag, playId),
  fetchSearch: (site: SiteSource, keyword: string, quick?: boolean) =>
    ipcRenderer.invoke('catvod:fetchSearch', site, keyword, quick),
  fetchAggregateSearch: (sites: SiteSource[], keyword: string) =>
    ipcRenderer.invoke('catvod:fetchAggregateSearch', sites, keyword),
  fetchSubscription: (subUrl: string) => ipcRenderer.invoke('catvod:fetchSubscription', subUrl),

  getCatPawPort: () => ipcRenderer.invoke('catvod:getCatPawPort'),
  getNetdiskCredentials: () => ipcRenderer.invoke('catvod:getNetdiskCredentials'),
  startNetdiskQrLogin: (provider: string) => ipcRenderer.invoke('catvod:startNetdiskQrLogin', provider),
  pollNetdiskQrLogin: (provider: string, taskId?: string) => ipcRenderer.invoke('catvod:pollNetdiskQrLogin', provider, taskId),
  saveNetdiskCookie: (provider: string, cookie: string) => ipcRenderer.invoke('catvod:saveNetdiskCookie', provider, cookie),

  windowControl: (action: 'minimize' | 'maximize' | 'close') => ipcRenderer.invoke('catvod:windowControl', action),
});
