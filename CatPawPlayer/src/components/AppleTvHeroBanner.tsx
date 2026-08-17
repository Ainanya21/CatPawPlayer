import React, { useState, useEffect } from 'react';
import { Play, Info, Star } from 'lucide-react';
import { VodItem } from '../types';

interface AppleTvHeroBannerProps {
  items: VodItem[];
  onSelectItem: (item: VodItem) => void;
}

export const AppleTvHeroBanner: React.FC<AppleTvHeroBannerProps> = ({ items, onSelectItem }) => {
  const [currentIndex, setCurrentIndex] = useState(0);

  const heroItems = items.slice(0, 5);

  useEffect(() => {
    if (heroItems.length === 0) return;
    const timer = setInterval(() => {
      setCurrentIndex((prev) => (prev + 1) % heroItems.length);
    }, 6000);
    return () => clearInterval(timer);
  }, [heroItems.length]);

  if (heroItems.length === 0) return null;

  const activeItem = heroItems[currentIndex] || heroItems[0];
  const fallbackCover = 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=1200&q=80';

  return (
    <div className="relative w-full h-80 sm:h-96 rounded-3xl overflow-hidden glass-card shadow-2xl border border-theme group select-none">
      {/* Background High-Definition Banner Image */}
      <img
        src={activeItem.vod_pic || fallbackCover}
        alt={activeItem.vod_name}
        className="w-full h-full object-cover transform transition-transform duration-1000 group-hover:scale-105"
      />

      {/* Gradient Mask Overlays */}
      <div className="absolute inset-0 bg-gradient-to-t from-slate-950 via-slate-950/60 to-transparent" />
      <div className="absolute inset-0 bg-gradient-to-r from-slate-950/90 via-slate-950/40 to-transparent" />

      {/* Banner Content Meta */}
      <div className="absolute bottom-6 left-8 max-w-xl space-y-3 z-10">
        <div className="flex items-center space-x-2">
          {activeItem.vod_douban_rate && (
            <span className="px-2.5 py-0.5 rounded-md bg-amber-500/20 text-amber-400 text-xs font-bold border border-amber-500/30 flex items-center space-x-1">
              <Star className="w-3.5 h-3.5 fill-current" />
              <span>豆瓣 {activeItem.vod_douban_rate}</span>
            </span>
          )}
          {activeItem.vod_remarks && (
            <span className="ww-badge">
              {activeItem.vod_remarks}
            </span>
          )}
        </div>

        <h1 className="text-2xl sm:text-4xl font-extrabold text-white tracking-tight drop-shadow-md line-clamp-1">
          {activeItem.vod_name}
        </h1>

        <p className="text-xs text-slate-300 line-clamp-2 leading-relaxed max-w-lg">
          {activeItem.vod_content || 'Apple TV 质感半屏海报，极致沉浸式影音播放体验。'}
        </p>

        <div className="flex items-center space-x-3 pt-2">
          <button
            onClick={() => onSelectItem(activeItem)}
            className="px-5 py-2.5 rounded-xl bg-accent hover:bg-accent text-white font-bold text-xs shadow-accent flex items-center space-x-2 transition-all active:scale-95"
          >
            <Play className="w-4 h-4 fill-current ml-0.5" />
            <span>立即播放</span>
          </button>
          <button
            onClick={() => onSelectItem(activeItem)}
            className="px-5 py-2.5 rounded-xl glass-card text-white font-bold text-xs flex items-center space-x-2 border border-white/20 active:scale-95"
          >
            <Info className="w-4 h-4" />
            <span>详情介绍</span>
          </button>
        </div>
      </div>

      {/* Hero Banner Slide Indicators */}
      <div className="absolute bottom-6 right-8 flex items-center space-x-1.5 z-10">
        {heroItems.map((_, idx) => (
          <button
            key={idx}
            onClick={() => setCurrentIndex(idx)}
            className={`h-1.5 rounded-full transition-all duration-300 ${
              currentIndex === idx ? 'w-6 bg-accent' : 'w-1.5 bg-white/40 hover:bg-white/70'
            }`}
          />
        ))}
      </div>
    </div>
  );
};
