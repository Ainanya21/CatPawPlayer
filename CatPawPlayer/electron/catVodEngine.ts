import axios from 'axios';
import { SiteSource, CategoryResult, VodItem, PlayResult } from '../src/types';

let currentCatPawPort: number = 9988;

export async function getCatPawPort(): Promise<number> {
  return currentCatPawPort;
}

export async function startCatPawServer(spiderUrl: string): Promise<any> {
  try {
    const res = await axios.post(`http://127.0.0.1:${currentCatPawPort}/init`, { url: spiderUrl }, { timeout: 15000 });
    return res.data;
  } catch (e: any) {
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

export async function fetchHome(site: SiteSource): Promise<CategoryResult> {
  if (site.type === 3) {
    try {
      const res = await axios.post(`http://127.0.0.1:${currentCatPawPort}/spider/${site.key}/3/home`, {}, { timeout: 15000 });
      return res.data;
    } catch (e) {
      return { list: [], class: [] };
    }
  }

  // Standard CMS Type 0/1/2
  try {
    const res = await axios.get(site.api, {
      params: { ac: 'detail' },
      timeout: 10000,
    });
    return res.data;
  } catch (e) {
    return { list: [], class: [] };
  }
}

export async function fetchCategory(site: SiteSource, tid: string, page: number, extend?: any): Promise<CategoryResult> {
  if (site.type === 3) {
    try {
      const res = await axios.post(
        `http://127.0.0.1:${currentCatPawPort}/spider/${site.key}/3/category`,
        { tid, pg: page, extend },
        { timeout: 15000 }
      );
      return res.data;
    } catch (e) {
      return { list: [] };
    }
  }

  try {
    const res = await axios.get(site.api, {
      params: { ac: 'detail', t: tid, pg: page, ...extend },
      timeout: 10000,
    });
    return res.data;
  } catch (e) {
    return { list: [] };
  }
}

export async function fetchDetail(site: SiteSource, vodId: string): Promise<VodItem | null> {
  if (site.type === 3) {
    try {
      const res = await axios.post(
        `http://127.0.0.1:${currentCatPawPort}/spider/${site.key}/3/detail`,
        { ids: [vodId] },
        { timeout: 15000 }
      );
      if (res.data && res.data.list && res.data.list[0]) {
        return res.data.list[0];
      }
      return null;
    } catch (e) {
      return null;
    }
  }

  try {
    const res = await axios.get(site.api, {
      params: { ac: 'detail', ids: vodId },
      timeout: 10000,
    });
    return res.data?.list?.[0] || null;
  } catch (e) {
    return null;
  }
}

export async function fetchPlayUrl(site: SiteSource, flag: string, playId: string): Promise<PlayResult> {
  if (site.type === 3) {
    try {
      const res = await axios.post(
        `http://127.0.0.1:${currentCatPawPort}/spider/${site.key}/3/player`,
        { flag, id: playId },
        { timeout: 15000 }
      );
      return res.data;
    } catch (e) {
      return { parse: 0, url: playId };
    }
  }

  return { parse: 0, url: playId };
}

export async function fetchSearch(site: SiteSource, keyword: string, quick?: boolean): Promise<VodItem[]> {
  if (site.type === 3) {
    try {
      const res = await axios.post(
        `http://127.0.0.1:${currentCatPawPort}/spider/${site.key}/3/search`,
        { key: keyword, quick: quick ? 1 : 0 },
        { timeout: 15000 }
      );
      return res.data?.list || [];
    } catch (e) {
      return [];
    }
  }

  try {
    const res = await axios.get(site.api, {
      params: { ac: 'detail', wd: keyword },
      timeout: 10000,
    });
    return res.data?.list || [];
  } catch (e) {
    return [];
  }
}

export async function getNetdiskCredentials(): Promise<any> {
  try {
    const res = await axios.get(`http://127.0.0.1:${currentCatPawPort}/netdisk/credentials`, { timeout: 5000 });
    return res.data;
  } catch (e) {
    return null;
  }
}

export async function startNetdiskQrLogin(provider: string): Promise<any> {
  try {
    const res = await axios.post(`http://127.0.0.1:${currentCatPawPort}/netdisk/qr/start`, { provider }, { timeout: 10000 });
    return res.data;
  } catch (e) {
    return { code: -1, msg: '微服务网络未挂载' };
  }
}

export async function pollNetdiskQrLogin(provider: string, taskId?: string): Promise<any> {
  try {
    const res = await axios.post(`http://127.0.0.1:${currentCatPawPort}/netdisk/qr/poll`, { provider, taskId }, { timeout: 10000 });
    return res.data;
  } catch (e) {
    return { code: -1, msg: '轮询轮退' };
  }
}

export async function saveNetdiskCookie(provider: string, cookie: string): Promise<any> {
  try {
    const res = await axios.post(`http://127.0.0.1:${currentCatPawPort}/netdisk/cookie/save`, { provider, cookie }, { timeout: 5000 });
    return res.data;
  } catch (e) {
    return null;
  }
}
