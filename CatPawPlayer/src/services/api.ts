import { SiteSource, CategoryResult, VodItem, PlayResult, SubscriptionConfig } from '../types';
import { DEFAULT_SITES } from './defaultSources';

export interface AggregateSearchResult {
  siteKey: string;
  siteName: string;
  siteType: number;
  list: VodItem[];
}

declare global {
  interface Window {
    electronAPI?: {
      fetchHome: (site: SiteSource) => Promise<CategoryResult>;
      fetchCategory: (site: SiteSource, tid: string, page: number, extend?: any) => Promise<CategoryResult>;
      fetchDetail: (site: SiteSource, vodId: string) => Promise<VodItem | null>;
      fetchPlayUrl: (site: SiteSource, flag: string, playId: string) => Promise<PlayResult>;
      fetchSearch: (site: SiteSource, keyword: string, quick?: boolean) => Promise<VodItem[]>;
      fetchAggregateSearch: (sites: SiteSource[], keyword: string) => Promise<AggregateSearchResult[]>;
      fetchSubscription: (subUrl: string) => Promise<SubscriptionConfig>;

      getCatPawPort: () => Promise<number | null>;
      getNetdiskCredentials: () => Promise<any>;
      startNetdiskQrLogin: (provider: string) => Promise<any>;
      pollNetdiskQrLogin: (provider: string, taskId?: string) => Promise<any>;
      saveNetdiskCookie: (provider: string, cookie: string) => Promise<any>;

      windowControl: (action: 'minimize' | 'maximize' | 'close') => void;
    };
  }
}

export const api = {
  fetchHome: async (site: SiteSource): Promise<CategoryResult> => {
    if (window.electronAPI) {
      return await window.electronAPI.fetchHome(site);
    }
    return fetchCmsFallback(site.api, 'detail');
  },

  fetchCategory: async (site: SiteSource, tid: string, page: number, extend?: any): Promise<CategoryResult> => {
    if (window.electronAPI) {
      return await window.electronAPI.fetchCategory(site, tid, page, extend);
    }
    return fetchCmsFallback(site.api, 'detail', { t: tid, pg: page });
  },

  fetchDetail: async (site: SiteSource, vodId: string): Promise<VodItem | null> => {
    if (window.electronAPI) {
      return await window.electronAPI.fetchDetail(site, vodId);
    }
    const res = await fetchCmsFallback(site.api, 'detail', { ids: vodId });
    return res.list && res.list[0] ? res.list[0] : null;
  },

  fetchPlayUrl: async (site: SiteSource, flag: string, playId: string): Promise<PlayResult> => {
    if (window.electronAPI) {
      return await window.electronAPI.fetchPlayUrl(site, flag, playId);
    }
    return { parse: 0, url: playId };
  },

  fetchSearch: async (site: SiteSource, keyword: string, quick = false): Promise<VodItem[]> => {
    if (window.electronAPI) {
      return await window.electronAPI.fetchSearch(site, keyword, quick);
    }
    const res = await fetchCmsFallback(site.api, 'detail', { wd: keyword });
    return res.list || [];
  },

  fetchAggregateSearch: async (sites: SiteSource[], keyword: string): Promise<AggregateSearchResult[]> => {
    if (window.electronAPI) {
      return await window.electronAPI.fetchAggregateSearch(sites, keyword);
    }
    if (sites.length > 0) {
      const res = await api.fetchSearch(sites[0], keyword);
      return [{ siteKey: sites[0].key, siteName: sites[0].name, siteType: sites[0].type, list: res || [] }];
    }
    return [];
  },

  fetchSubscription: async (subUrl: string): Promise<SubscriptionConfig> => {
    if (window.electronAPI) {
      return await window.electronAPI.fetchSubscription(subUrl);
    }
    const res = await fetch(subUrl);
    return await res.json();
  },

  getCatPawPort: async (): Promise<number | null> => {
    if (window.electronAPI) {
      return await window.electronAPI.getCatPawPort();
    }
    return 9988;
  },

  getNetdiskCredentials: async (): Promise<any> => {
    if (window.electronAPI) {
      return await window.electronAPI.getNetdiskCredentials();
    }
    return null;
  },

  startNetdiskQrLogin: async (provider: string): Promise<any> => {
    if (window.electronAPI) {
      return await window.electronAPI.startNetdiskQrLogin(provider);
    }
    return null;
  },

  pollNetdiskQrLogin: async (provider: string, taskId?: string): Promise<any> => {
    if (window.electronAPI) {
      return await window.electronAPI.pollNetdiskQrLogin(provider, taskId);
    }
    return null;
  },

  saveNetdiskCookie: async (provider: string, cookie: string): Promise<any> => {
    if (window.electronAPI) {
      return await window.electronAPI.saveNetdiskCookie(provider, cookie);
    }
    return null;
  },

  windowControl: (action: 'minimize' | 'maximize' | 'close') => {
    if (window.electronAPI) {
      window.electronAPI.windowControl(action);
    }
  },
};

async function fetchCmsFallback(apiUrl: string, action: string, params: Record<string, any> = {}) {
  try {
    const url = new URL(apiUrl);
    url.searchParams.set('ac', action);
    Object.entries(params).forEach(([k, v]) => url.searchParams.set(k, String(v)));

    const res = await fetch(url.toString());
    return await res.json();
  } catch (e) {
    return { list: [], class: [], page: 1, pagecount: 1, total: 0 };
  }
}
