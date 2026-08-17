import { SiteSource } from '../types';

export const DEFAULT_SUBSCRIPTION_URLS = [
  {
    name: '猫源官方底层 Bundle',
    url: 'https://github.com/Carrottor/WWPlayer/raw/main/cat.js',
  },
  {
    name: '饭太硬 TVBox 优质源',
    url: 'http://饭太硬.top/tv',
  },
  {
    name: '肥猫优质主源',
    url: 'http://肥猫.com',
  },
];

export const DEFAULT_SITES: SiteSource[] = [
  {
    key: 'wogg',
    name: '玩偶4K',
    type: 3,
    api: 'csp_Wogg',
    searchable: 1,
    quickSearch: 1,
    filterable: 1,
  },
  {
    key: 'zhizhen',
    name: '至臻4K',
    type: 3,
    api: 'csp_Zhizhen',
    searchable: 1,
    quickSearch: 1,
    filterable: 1,
  },
  {
    key: 'douban',
    name: '豆瓣热映榜',
    type: 3,
    api: 'csp_Douban',
    searchable: 1,
    quickSearch: 1,
    filterable: 1,
  },
  {
    key: 'feisu',
    name: '飞速资源',
    type: 1,
    api: 'https://www.feisuzy.com/api.php/provide/vod/',
    searchable: 1,
    quickSearch: 1,
    filterable: 1,
  },
];
