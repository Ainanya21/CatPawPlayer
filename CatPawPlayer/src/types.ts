export interface SiteSource {
  key: string;
  name: string;
  type: number;
  api: string;
  searchable?: number;
  quickSearch?: number;
  filterable?: number;
  ext?: string;
}

export interface SubscriptionConfig {
  sites: SiteSource[];
  urls?: Array<{ name: string; url: string }>;
}

export interface VodItem {
  vod_id: string;
  vod_name: string;
  vod_pic: string;
  vod_remarks?: string;
  vod_actor?: string;
  vod_director?: string;
  vod_content?: string;
  vod_play_from?: string;
  vod_play_url?: string;
  vod_year?: string;
  vod_area?: string;
  vod_douban_rate?: string;
  type_name?: string;
}

export interface CategoryItem {
  type_id: string;
  type_name: string;
}

export interface CategoryResult {
  list: VodItem[];
  class?: CategoryItem[];
  page?: number;
  pagecount?: number;
  total?: number;
  filters?: Record<string, any>;
}

export interface PlayResult {
  parse?: number;
  url: string;
  header?: Record<string, string>;
  jx?: number;
}

export interface HistoryItem {
  id: string;
  siteKey: string;
  siteName: string;
  vodId: string;
  vodName: string;
  vodPic: string;
  epName: string;
  url: string;
  progress: number;
  duration: number;
  updatedAt: number;
}
