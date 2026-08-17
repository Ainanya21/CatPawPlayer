import React, { useState } from 'react';
import { Play, Heart, ArrowLeft, Star, Monitor, Film, Tag, Sparkles } from 'lucide-react';
import { VodItem, SiteSource } from '../types';

interface WWDetailHeroProps {
  item: VodItem;
  site: SiteSource;
  onPlayEpisode: (item: VodItem, sourceName: string, epName: string, epUrl: string) => void;
  onToggleFavorite: (item: VodItem) => void;
  isFavorite: boolean;
  onClose: () => void;
}

export const WWDetailHero: React.FC<WWDetailHeroProps> = ({
  item,
  site,
  onPlayEpisode,
  onToggleFavorite,
  isFavorite,
  onClose,
}) => {
  const [activeSourceIndex, setActiveSourceIndex] = useState(0);

  // Parse Play Sources & Episodes
  const playFromList = item.vod_play_from ? item.vod_play_from.split('$$$') : ['默认播放源'];
  const playUrlList = item.vod_play_url ? item.vod_play_url.split('$$$') : [];

  const parseEpisodes = (urlStr: string) => {
    if (!urlStr) return [];
    return urlStr.split('#').map((ep) => {
      const parts = ep.split('$');
      if (parts.length >= 2) {
        return { name: parts[0].trim(), url: parts[1].trim() };
      }
      return { name: '正片', url: ep.trim() };
    });
  };

  const currentEpisodes = parseEpisodes(playUrlList[activeSourceIndex] || playUrlList[0] || '');

  return (
    <div className="relative w-full h-full min-h-screen overflow-y-auto app-bg text-primary p-6 space-y-6 animate-fade-in select-none">
      {/* Top Floating Back Header */}
      <div className="flex items-center justify-between pb-2 pt-10">
        <button
          onClick={onClose}
          className="flex items-center space-x-2 px-4 py-2 rounded-xl glass-card text-xs font-bold border border-theme text-primary active:scale-95 transition-transform"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>返回大厅</span>
        </button>

        <div className="flex items-center space-x-2">
          <button
            onClick={() => onToggleFavorite(item)}
            className={`flex items-center space-x-1.5 px-4 py-2 rounded-xl text-xs font-bold border transition-all ${
              isFavorite
                ? 'bg-rose-500 text-white border-rose-400 shadow-md'
                : 'glass-card text-primary border-theme'
            }`}
          >
            <Heart className={`w-4 h-4 ${isFavorite ? 'fill-current' : ''}`} />
            <span>{isFavorite ? '已收藏' : '加入收藏'}</span>
          </button>
        </div>
      </div>

      {/* WWPlayer Apple TV Full Hero Header Layout */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-8 glass-panel p-6 rounded-3xl border border-theme shadow-2xl">
        {/* Cover Poster */}
        <div className="aspect-[2/3] w-full max-w-sm mx-auto rounded-2xl overflow-hidden glass-card shadow-xl relative">
          <img
            src={item.vod_pic || 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=500&q=80'}
            alt={item.vod_name}
            className="w-full h-full object-cover"
          />
          {item.vod_remarks && (
            <span className="ww-badge absolute top-3 right-3 shadow-md">
              {item.vod_remarks}
            </span>
          )}
        </div>

        {/* Detail Information */}
        <div className="md:col-span-2 space-y-4 flex flex-col justify-between">
          <div className="space-y-3">
            <div className="flex items-center space-x-2 flex-wrap gap-y-2">
              <span className="px-2.5 py-0.5 rounded-md bg-accent-subtle text-accent text-xs font-bold border border-accent">
                {site.name}
              </span>
              {item.type_name && (
                <span className="px-2.5 py-0.5 rounded-md glass-card text-secondary text-xs font-semibold border border-theme flex items-center space-x-1">
                  <Tag className="w-3 h-3 text-secondary" />
                  <span>{item.type_name}</span>
                </span>
              )}
              {item.vod_douban_rate && (
                <span className="px-2.5 py-0.5 rounded-md bg-amber-500/20 text-amber-500 text-xs font-bold border border-amber-500/30 flex items-center space-x-1">
                  <Star className="w-3.5 h-3.5 fill-current" />
                  <span>豆瓣 {item.vod_douban_rate}</span>
                </span>
              )}
            </div>

            <h1 className="text-2xl sm:text-4xl font-extrabold text-primary tracking-tight">
              {item.vod_name}
            </h1>

            <div className="text-xs text-secondary space-y-1 font-medium">
              {item.vod_director && <p><span className="text-primary font-bold">导演：</span>{item.vod_director}</p>}
              {item.vod_actor && <p><span className="text-primary font-bold">主演：</span>{item.vod_actor}</p>}
              {item.vod_year && <p><span className="text-primary font-bold">年份：</span>{item.vod_year} / {item.vod_area || '未知'}</p>}
            </div>

            <div className="pt-2">
              <h3 className="text-xs font-bold text-primary mb-1">剧情简介：</h3>
              <p className="text-xs text-secondary leading-relaxed line-clamp-4 glass-card p-3 rounded-xl">
                {item.vod_content ? item.vod_content.replace(/<[^>]+>/g, '') : '暂无详细剧情介绍。'}
              </p>
            </div>
          </div>

          {/* Quick Play First Episode Button */}
          {currentEpisodes.length > 0 && (
            <div className="pt-4">
              <button
                onClick={() =>
                  onPlayEpisode(
                    item,
                    playFromList[activeSourceIndex] || '默认源',
                    currentEpisodes[0].name,
                    currentEpisodes[0].url
                  )
                }
                className="w-full sm:w-auto px-8 py-3 rounded-xl bg-accent hover:bg-accent text-white font-extrabold text-xs shadow-accent flex items-center justify-center space-x-2 transition-all active:scale-95"
              >
                <Play className="w-4 h-4 fill-current ml-1" />
                <span>立即播放第一集 ({currentEpisodes[0].name})</span>
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Episode Selection Section */}
      <div className="space-y-4 glass-panel p-6 rounded-3xl border border-theme shadow-xl">
        {/* Source Selector Tabs */}
        <div className="flex items-center space-x-2 border-b border-theme pb-3 overflow-x-auto no-scrollbar">
          <span className="text-xs font-extrabold text-primary flex items-center space-x-1.5 mr-2">
            <Monitor className="w-4 h-4 text-accent" />
            <span>线路源选择：</span>
          </span>

          {playFromList.map((source, idx) => (
            <button
              key={idx}
              onClick={() => setActiveSourceIndex(idx)}
              className={`px-4 py-2 rounded-xl text-xs font-bold transition-all flex-shrink-0 ${
                activeSourceIndex === idx
                  ? 'bg-accent text-white shadow-accent'
                  : 'glass-card text-secondary hover:text-primary'
              }`}
            >
              {source} ({parseEpisodes(playUrlList[idx] || '').length}集)
            </button>
          ))}
        </div>

        {/* Episode Grid */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-primary flex items-center space-x-1">
              <Film className="w-3.5 h-3.5 text-accent" />
              <span>选集列表 ({currentEpisodes.length} 个剧集片段):</span>
            </span>
          </div>

          <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 lg:grid-cols-8 gap-2.5 max-h-72 overflow-y-auto pr-1">
            {currentEpisodes.map((ep, idx) => (
              <button
                key={idx}
                onClick={() =>
                  onPlayEpisode(
                    item,
                    playFromList[activeSourceIndex] || '默认源',
                    ep.name,
                    ep.url
                  )
                }
                className="p-2.5 rounded-xl glass-card text-xs font-bold text-primary hover:border-accent hover:text-accent transition-all truncate text-center shadow-sm active:scale-95"
                title={ep.name}
              >
                {ep.name}
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
