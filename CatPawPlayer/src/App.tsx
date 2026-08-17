import React, { useState, useEffect, useCallback, useTransition } from 'react';
import { Navbar } from './components/Navbar';
import { WWSidebar } from './components/WWSidebar';
import { TabType } from './components/Sidebar';
import { MediaGrid } from './components/MediaGrid';
import { WWDetailHero } from './components/WWDetailHero';
import { PlayerOverlay } from './components/PlayerOverlay';
import { SubscriptionModal, SubscriptionItem } from './components/SubscriptionModal';
import { NetdiskConfigModal } from './components/NetdiskConfigModal';
import { HistoryFavorites } from './components/HistoryFavorites';
import { CategoryBar } from './components/CategoryBar';
import { AppleTvHeroBanner } from './components/AppleTvHeroBanner';
import { ShowcaseRow } from './components/ShowcaseRow';
import { SiteSource, VodItem, CategoryItem, HistoryItem } from './types';
import { DEFAULT_SITES, DEFAULT_SUBSCRIPTION_URLS } from './services/defaultSources';
import { api, AggregateSearchResult } from './services/api';
import { applyTheme } from './utils/theme';
import { Flame, Star, Film, RefreshCw, AlertCircle, Layers, Search, Globe } from 'lucide-react';

export const App: React.FC = () => {
  const [isPending, startTransition] = useTransition();

  useEffect(() => {
    applyTheme();
  }, []);

  const [savedSubscriptions, setSavedSubscriptions] = useState<SubscriptionItem[]>(() => {
    const saved = localStorage.getItem('catpaw_saved_subscriptions');
    if (saved) return JSON.parse(saved);
    return DEFAULT_SUBSCRIPTION_URLS.map((item, i) => ({
      id: `default_${i}`,
      name: item.name,
      url: item.url,
      siteCount: 0,
      updatedAt: Date.now(),
    }));
  });

  const [activeSubUrl, setActiveSubUrl] = useState<string>(() => {
    return localStorage.getItem('catpaw_active_sub_url') || DEFAULT_SUBSCRIPTION_URLS[0].url;
  });

  const [sites, setSites] = useState<SiteSource[]>(() => {
    const saved = localStorage.getItem('catpaw_sites');
    return saved ? JSON.parse(saved) : DEFAULT_SITES;
  });

  const [activeSite, setActiveSite] = useState<SiteSource | null>(() => {
    const savedKey = localStorage.getItem('catpaw_active_site_key');
    const matched = sites.find((s) => s.key === savedKey);
    return matched || sites[0] || null;
  });

  const [siteHomeCache, setSiteHomeCache] = useState<Record<string, { list: VodItem[]; class: CategoryItem[]; filters: any }>>({});

  const [activeTab, setActiveTab] = useState<TabType>('home');
  const [items, setItems] = useState<VodItem[]>([]);
  const [categories, setCategories] = useState<CategoryItem[]>([]);
  const [filters, setFilters] = useState<Record<string, any>>({});
  const [activeCategory, setActiveCategory] = useState<string>('');
  const [activeExtend, setActiveExtend] = useState<Record<string, string>>({});
  const [page, setPage] = useState(1);
  const [pageCount, setPageCount] = useState(1);
  const [isLoading, setIsLoading] = useState(false);

  const [doubanHotMovies, setDoubanHotMovies] = useState<VodItem[]>([]);
  const [doubanTopMovies, setDoubanTopMovies] = useState<VodItem[]>([]);

  const [searchKeyword, setSearchKeyword] = useState('');
  const [aggregateSearchResults, setAggregateSearchResults] = useState<AggregateSearchResult[]>([]);
  const [isSearching, setIsSearching] = useState(false);

  const [selectedVod, setSelectedVod] = useState<VodItem | null>(null);
  const [playingState, setPlayingState] = useState<{
    item: VodItem;
    sourceName: string;
    epName: string;
    epUrl: string;
  } | null>(null);
  const [netdiskModalDefaultMode, setNetdiskModalDefaultMode] = useState<'native' | 'web' | 'theme'>('native');

  const [historyList, setHistoryList] = useState<HistoryItem[]>(() => {
    const saved = localStorage.getItem('catpaw_history');
    return saved ? JSON.parse(saved) : [];
  });
  const [favoriteList, setFavoriteList] = useState<VodItem[]>(() => {
    const saved = localStorage.getItem('catpaw_favorites');
    return saved ? JSON.parse(saved) : [];
  });

  useEffect(() => {
    localStorage.setItem('catpaw_saved_subscriptions', JSON.stringify(savedSubscriptions));
  }, [savedSubscriptions]);

  useEffect(() => {
    localStorage.setItem('catpaw_active_sub_url', activeSubUrl);
  }, [activeSubUrl]);

  useEffect(() => {
    localStorage.setItem('catpaw_sites', JSON.stringify(sites));
  }, [sites]);

  useEffect(() => {
    if (activeSite) {
      localStorage.setItem('catpaw_active_site_key', activeSite.key);
    }
  }, [activeSite?.key]);

  useEffect(() => {
    localStorage.setItem('catpaw_history', JSON.stringify(historyList));
  }, [historyList]);

  useEffect(() => {
    localStorage.setItem('catpaw_favorites', JSON.stringify(favoriteList));
  }, [favoriteList]);

  useEffect(() => {
    async function restoreActiveSubscription() {
      if (!activeSubUrl) return;
      try {
        const config = await api.fetchSubscription(activeSubUrl);
        if (config && Array.isArray(config.sites) && config.sites.length > 0) {
          setSites(config.sites);
          const savedKey = localStorage.getItem('catpaw_active_site_key');
          const matched = config.sites.find((s) => s.key === savedKey);
          setActiveSite(matched || config.sites[0]);
        }
      } catch (e) {
        console.warn('[Restore Subscription Failed, fallback to default]', e);
      }
    }
    restoreActiveSubscription();
  }, []);

  useEffect(() => {
    async function loadDoubanTrends() {
      try {
        const res1 = await fetch('https://movie.douban.com/j/search_subjects?type=movie&tag=%E7%83%AD%E9%97%A8&sort=recommend&page_limit=10&page_start=0');
        const d1 = await res1.json();
        if (d1.subjects) {
          setDoubanHotMovies(d1.subjects.map((s: any) => ({
            vod_id: s.id,
            vod_name: s.title,
            vod_pic: s.cover,
            vod_remarks: `${s.rate || 8.0} 分`,
            vod_douban_rate: s.rate,
            type_name: '豆瓣热门',
          })));
        }

        const res2 = await fetch('https://movie.douban.com/j/search_subjects?type=movie&tag=%E8%B1%86%E7%93%A3%E9%AB%98%E5%88%86&sort=rank&page_limit=10&page_start=0');
        const d2 = await res2.json();
        if (d2.subjects) {
          setDoubanTopMovies(d2.subjects.map((s: any) => ({
            vod_id: s.id,
            vod_name: s.title,
            vod_pic: s.cover,
            vod_remarks: `${s.rate || 9.0} 分`,
            vod_douban_rate: s.rate,
            type_name: '豆瓣高分',
          })));
        }
      } catch (e) {}
    }
    loadDoubanTrends();
  }, []);

  const loadHomeData = useCallback(async (site: SiteSource) => {
    if (siteHomeCache[site.key]) {
      const cached = siteHomeCache[site.key];
      setItems(cached.list);
      setCategories(cached.class);
      setFilters(cached.filters);
      if (cached.class.length > 0) setActiveCategory(cached.class[0].type_id);
    } else {
      setIsLoading(true);
    }

    setPage(1);
    setActiveExtend({});

    try {
      const res = await api.fetchHome(site);
      const fetchedItems = res.list || [];
      const fetchedClasses = res.class || [];
      const fetchedFilters = res.filters || {};

      setItems(fetchedItems);
      setCategories(fetchedClasses);
      setFilters(fetchedFilters);
      setPageCount(res.pagecount || 1);

      if (fetchedClasses.length > 0) {
        setActiveCategory(fetchedClasses[0].type_id);
      }

      setSiteHomeCache((prev) => ({
        ...prev,
        [site.key]: { list: fetchedItems, class: fetchedClasses, filters: fetchedFilters },
      }));
    } catch (err) {
      console.error('[Load Home Failed]', err);
      if (!siteHomeCache[site.key]) setItems([]);
    } finally {
      setIsLoading(false);
    }
  }, [siteHomeCache]);

  useEffect(() => {
    if (activeSite) {
      if (activeSite.name.includes('配置') || activeSite.key === 'nodejs_push' || activeSite.key.includes('config')) {
        setNetdiskModalDefaultMode('web');
        setActiveTab('settings');
      } else {
        loadHomeData(activeSite);
      }
    }
  }, [activeSite?.key, loadHomeData]);

  const loadCategoryData = useCallback(async (
    site: SiteSource,
    tid: string,
    pageNum = 1,
    extend: Record<string, string> = activeExtend
  ) => {
    setIsLoading(true);
    try {
      const res = await api.fetchCategory(site, tid, pageNum, extend);
      setItems(res.list || []);
      setPage(res.page || pageNum);
      setPageCount(res.pagecount || 1);
      if (res.class && res.class.length > 0 && categories.length === 0) {
        setCategories(res.class);
      }
      if (res.filters && Object.keys(res.filters).length > 0) {
        setFilters((prev) => ({ ...prev, ...res.filters }));
      }
    } catch (err) {
      console.error('[Load Category Failed]', err);
      setItems([]);
    } finally {
      setIsLoading(false);
    }
  }, [activeExtend, categories.length]);

  const handleSelectCategory = (typeId: string) => {
    setActiveCategory(typeId);
    setActiveExtend({});
    if (!activeSite) return;
    if (typeId === '') {
      loadHomeData(activeSite);
    } else {
      loadCategoryData(activeSite, typeId, 1, {});
    }
  };

  const handleSelectFilter = (key: string, value: string) => {
    const updated = { ...activeExtend, [key]: value };
    setActiveExtend(updated);
    if (!activeSite) return;
    loadCategoryData(activeSite, activeCategory, 1, updated);
  };

  const handleSearch = async (keyword: string) => {
    const trimmed = keyword.trim();
    if (!trimmed) return;

    setSearchKeyword(trimmed);
    startTransition(() => {
      setActiveTab('search');
      setSelectedVod(null);
    });

    setIsSearching(true);
    setAggregateSearchResults([]);

    try {
      const results = await api.fetchAggregateSearch(sites, trimmed);
      setAggregateSearchResults(results || []);
    } catch (err) {
      console.error('[Aggregate Search Failed]', err);
      setAggregateSearchResults([]);
    } finally {
      setIsSearching(false);
    }
  };

  const handlePageChange = (newPage: number) => {
    if (!activeSite) return;
    if (activeCategory) {
      loadCategoryData(activeSite, activeCategory, newPage, activeExtend);
    } else {
      setPage(newPage);
    }
  };

  const handleSelectItem = async (item: VodItem, siteSource?: SiteSource) => {
    if (item.vod_name.includes('配置') || item.vod_id.includes('website') || item.vod_name.includes('扫码')) {
      setNetdiskModalDefaultMode('web');
      startTransition(() => setActiveTab('settings'));
      return;
    }

    const targetSite = siteSource || activeSite;

    if (targetSite) {
      setIsLoading(true);
      const detail = await api.fetchDetail(targetSite, item.vod_id);
      setSelectedVod(detail || item);
      setIsLoading(false);
    } else {
      setSelectedVod(item);
    }
  };

  const handlePlayEpisode = (
    item: VodItem,
    sourceName: string,
    epName: string,
    epUrl: string
  ) => {
    setPlayingState({ item, sourceName, epName, epUrl });
  };

  const saveHistory = (
    epName: string,
    epUrl: string,
    currentTime: number,
    duration: number
  ) => {
    if (!playingState || !activeSite) return;
    const { item } = playingState;
    const historyId = `${activeSite.key}_${item.vod_id}`;

    setHistoryList((prev) => {
      const filtered = prev.filter((h) => h.id !== historyId);
      const newEntry: HistoryItem = {
        id: historyId,
        siteKey: activeSite.key,
        siteName: activeSite.name,
        vodId: item.vod_id,
        vodName: item.vod_name,
        vodPic: item.vod_pic || '',
        epName,
        url: epUrl,
        progress: currentTime,
        duration,
        updatedAt: Date.now(),
      };
      return [newEntry, ...filtered];
    });
  };

  const toggleFavorite = (vod: VodItem) => {
    setFavoriteList((prev) => {
      const exists = prev.some((f) => f.vod_id === vod.vod_id);
      if (exists) {
        return prev.filter((f) => f.vod_id !== vod.vod_id);
      }
      return [vod, ...prev];
    });
  };

  const isVodFavorite = (vodId: string) => {
    return favoriteList.some((f) => f.vod_id === vodId);
  };

  const handleSelectSubscription = (subUrl: string, fetchedSites: SiteSource[], subName?: string) => {
    setActiveSubUrl(subUrl);
    setSites(fetchedSites);
    if (fetchedSites.length > 0) {
      setActiveSite(fetchedSites[0]);
    }
  };

  const totalAggregateCount = aggregateSearchResults.reduce((sum, res) => sum + res.list.length, 0);

  return (
    <div className="h-screen w-screen flex app-bg text-primary font-sans overflow-hidden relative">
      <Navbar
        sites={sites}
        activeSite={activeSite}
        onSelectSite={(site) => {
          setActiveSite(site);
          if (site.name.includes('配置') || site.key === 'nodejs_push' || site.key.includes('config')) {
            setNetdiskModalDefaultMode('web');
            startTransition(() => setActiveTab('settings'));
          } else {
            loadHomeData(site);
          }
        }}
        onSearch={handleSearch}
        onOpenSubscription={() => {
          setSelectedVod(null);
          startTransition(() => setActiveTab('subscriptions'));
        }}
        onOpenNetdiskConfig={() => {
          setSelectedVod(null);
          setNetdiskModalDefaultMode('native');
          startTransition(() => setActiveTab('settings'));
        }}
        onRefresh={() => activeSite && loadHomeData(activeSite)}
        isLoading={isLoading}
      />

      <WWSidebar
        activeTab={activeTab}
        onTabChange={(tab) => {
          startTransition(() => {
            setActiveTab(tab);
            setSelectedVod(null);
          });
        }}
        onRefresh={() => activeSite && loadHomeData(activeSite)}
        canGoBack={!!selectedVod}
        onGoBack={() => setSelectedVod(null)}
      />

      <main className="flex-1 h-full overflow-y-auto relative gpu-accel">
        {selectedVod && activeSite ? (
          <WWDetailHero
            item={selectedVod}
            site={activeSite}
            onPlayEpisode={handlePlayEpisode}
            onToggleFavorite={toggleFavorite}
            isFavorite={isVodFavorite(selectedVod.vod_id)}
            onClose={() => setSelectedVod(null)}
          />
        ) : (
          <div className="pt-14 flex flex-col h-full">
            {activeTab === 'home' && (
              <div className="p-6 space-y-6 flex-1 max-w-7xl mx-auto w-full">
                {items.length > 0 ? (
                  <>
                    <AppleTvHeroBanner items={items} onSelectItem={handleSelectItem} />

                    <ShowcaseRow
                      title="🔥 TMDB / 豆瓣热搜爆款榜"
                      icon={Flame}
                      items={doubanHotMovies.length > 0 ? doubanHotMovies : items.slice(0, 10)}
                      isLoading={isLoading}
                      onSelectItem={handleSelectItem}
                    />

                    <ShowcaseRow
                      title="⭐ 豆瓣 9.0+ 殿堂级高分推荐"
                      icon={Star}
                      items={doubanTopMovies.length > 0 ? doubanTopMovies : items.slice(5, 15)}
                      isLoading={isLoading}
                      onSelectItem={handleSelectItem}
                    />

                    <ShowcaseRow
                      title={`⚡ ${activeSite?.name || '当前源'} - 最新剧集到库速递`}
                      icon={Film}
                      items={items.slice(5, 20)}
                      isLoading={isLoading}
                      onSelectItem={handleSelectItem}
                    />
                  </>
                ) : !isLoading ? (
                  <div className="p-12 glass-card rounded-3xl text-center space-y-4 my-8 flex flex-col items-center justify-center animate-fade-in">
                    <div className="w-14 h-14 rounded-2xl bg-indigo-500/20 text-indigo-500 flex items-center justify-center border border-indigo-500/30">
                      <AlertCircle className="w-7 h-7" />
                    </div>
                    <div>
                      <h3 className="text-base font-extrabold text-primary">
                        站点 [{activeSite?.name || '当前源'}] 暂无可用视频资源
                      </h3>
                      <p className="text-xs text-secondary max-w-md mt-1">
                        可能该源站需要外网访问环境、接口暂时维护或配置参数不同。您可直接在顶部下拉菜单或侧栏【订阅管理】中快速切换其它优质源站！
                      </p>
                    </div>
                    <div className="flex items-center space-x-3 pt-2">
                      <button
                        onClick={() => activeSite && loadHomeData(activeSite)}
                        className="px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-bold flex items-center space-x-1.5 shadow-md"
                      >
                        <RefreshCw className="w-3.5 h-3.5" />
                        <span>重新加载此站点</span>
                      </button>
                      <button
                        onClick={() => setActiveTab('subscriptions')}
                        className="px-4 py-2 rounded-xl glass-card text-xs font-bold flex items-center space-x-1.5 border border-theme"
                      >
                        <Layers className="w-3.5 h-3.5 text-indigo-500" />
                        <span>进入订阅源管理</span>
                      </button>
                    </div>
                  </div>
                ) : null}
              </div>
            )}

            {activeTab === 'category' && (
              <div className="flex-1 flex flex-col">
                {categories.length > 0 && (
                  <CategoryBar
                    categories={categories}
                    activeCategory={activeCategory}
                    onSelectCategory={handleSelectCategory}
                    filters={filters}
                    activeExtend={activeExtend}
                    onSelectFilter={handleSelectFilter}
                  />
                )}
                <div className="p-4 flex-1">
                  <MediaGrid
                    items={items}
                    isLoading={isLoading}
                    onSelectItem={handleSelectItem}
                    page={page}
                    pageCount={pageCount}
                    onPageChange={handlePageChange}
                  />
                </div>
              </div>
            )}

            {activeTab === 'search' && (
              <div className="p-6 space-y-6 flex-1 max-w-7xl mx-auto w-full">
                <div className="flex items-center justify-between border-b border-theme pb-4">
                  <div className="flex items-center space-x-3">
                    <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-indigo-600 to-violet-600 text-white flex items-center justify-center shadow-md">
                      <Search className="w-5 h-5" />
                    </div>
                    <div>
                      <h2 className="text-base font-extrabold text-primary">
                        全源并发聚合搜索：<span className="text-indigo-500">"{searchKeyword}"</span>
                      </h2>
                      <p className="text-xs text-secondary mt-0.5">
                        正在跨 {sites.length} 个站点实时并发检索匹配资源
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center space-x-2">
                    <span className="px-3 py-1 rounded-xl glass-card text-xs font-bold text-indigo-500 border border-theme">
                      {isSearching ? '正在并发检索中...' : `共命中 ${totalAggregateCount} 条结果`}
                    </span>
                  </div>
                </div>

                {isSearching ? (
                  <div className="h-64 flex flex-col items-center justify-center space-y-3 text-secondary text-xs animate-pulse">
                    <RefreshCw className="w-8 h-8 text-indigo-500 animate-spin" />
                    <span>正在同时请求多接口源站点数据...</span>
                  </div>
                ) : aggregateSearchResults.length > 0 ? (
                  <div className="space-y-6">
                    {aggregateSearchResults.map((group) => {
                      const matchedSite = sites.find((s) => s.key === group.siteKey);
                      return (
                        <ShowcaseRow
                          key={group.siteKey}
                          title={`${group.siteName} (${group.list.length} 条数据)`}
                          icon={Globe}
                          items={group.list}
                          isLoading={false}
                          onSelectItem={(item) => handleSelectItem(item, matchedSite)}
                        />
                      );
                    })}
                  </div>
                ) : (
                  <div className="p-12 glass-card rounded-3xl text-center space-y-3 my-8 flex flex-col items-center justify-center">
                    <AlertCircle className="w-8 h-8 text-secondary" />
                    <h3 className="text-sm font-bold text-primary">未搜索到匹配 "{searchKeyword}" 的视频内容</h3>
                    <p className="text-xs text-secondary">请尝试更换简短关键词或在【订阅管理】中切换更多订阅接口源</p>
                  </div>
                )}
              </div>
            )}

            {activeTab === 'history' && (
              <div className="flex-1">
                <HistoryFavorites
                  type="history"
                  historyList={historyList}
                  favoriteList={favoriteList}
                  onSelectHistory={(h) => {
                    if (activeSite) {
                      handleSelectItem({
                        vod_id: h.vodId,
                        vod_name: h.vodName,
                        vod_pic: h.vodPic,
                      });
                    }
                  }}
                  onSelectFavorite={() => {}}
                  onClearHistory={() => setHistoryList([])}
                />
              </div>
            )}

            {activeTab === 'favorites' && (
              <div className="flex-1">
                <HistoryFavorites
                  type="favorites"
                  historyList={historyList}
                  favoriteList={favoriteList}
                  onSelectHistory={() => {}}
                  onSelectFavorite={handleSelectItem}
                  onClearHistory={() => {}}
                />
              </div>
            )}

            {activeTab === 'subscriptions' && (
              <div className="p-6 h-full w-full max-w-6xl mx-auto flex-1">
                <SubscriptionModal
                  isEmbedded
                  currentSites={sites}
                  activeSubUrl={activeSubUrl}
                  savedSubscriptions={savedSubscriptions}
                  onSelectSubscription={handleSelectSubscription}
                  onSaveSubscriptions={setSavedSubscriptions}
                />
              </div>
            )}

            {activeTab === 'settings' && (
              <div className="p-6 h-full w-full max-w-6xl mx-auto flex-1">
                <NetdiskConfigModal
                  isEmbedded
                  defaultMode={netdiskModalDefaultMode}
                />
              </div>
            )}
          </div>
        )}
      </main>

      {playingState && activeSite && (
        <PlayerOverlay
          item={playingState.item}
          site={activeSite}
          sourceName={playingState.sourceName}
          epName={playingState.epName}
          epUrl={playingState.epUrl}
          onClose={() => setPlayingState(null)}
          onSaveHistory={saveHistory}
        />
      )}
    </div>
  );
};
