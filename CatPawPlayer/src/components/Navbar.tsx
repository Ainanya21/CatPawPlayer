import React, { useState } from 'react';
import { Search, Minus, Square, X, Sparkles } from 'lucide-react';
import { SiteSource } from '../types';
import { api } from '../services/api';

interface NavbarProps {
  sites: SiteSource[];
  activeSite: SiteSource | null;
  onSelectSite: (site: SiteSource) => void;
  onSearch: (keyword: string) => void;
  onOpenSubscription: () => void;
  onOpenNetdiskConfig: () => void;
  onRefresh: () => void;
  isLoading: boolean;
}

export const Navbar: React.FC<NavbarProps> = ({
  sites,
  activeSite,
  onSelectSite,
  onSearch,
}) => {
  const [showSearchModal, setShowSearchModal] = useState(false);
  const [searchInput, setSearchInput] = useState('');

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchInput.trim()) {
      onSearch(searchInput.trim());
      setShowSearchModal(false);
    }
  };

  const formatCleanName = (name: string) => {
    return name.replace(/\[猫源JS\]|\[CMS\]|\[猫源\]|\[JS\]/g, '').trim();
  };

  return (
    <header className="h-12 w-full flex items-center justify-between px-4 z-40 titlebar-drag select-none absolute top-0 left-0 right-0 pointer-events-none">
      {/* Left Source Selector Dropdown */}
      <div className="flex items-center space-x-2 titlebar-nodrag pointer-events-auto pl-16">
        <div className="relative group">
          <select
            value={activeSite?.key || ''}
            onChange={(e) => {
              const selected = sites.find((s) => s.key === e.target.value);
              if (selected) onSelectSite(selected);
            }}
            className="appearance-none glass-panel text-[11px] font-bold text-primary rounded-xl px-3 py-1.5 pr-7 border border-theme focus:outline-none cursor-pointer transition-all shadow-sm"
          >
            {sites.map((site) => (
              <option key={site.key} value={site.key} className="glass-card text-primary bg-slate-900">
                {formatCleanName(site.name)}
              </option>
            ))}
          </select>
          <div className="absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none">
            <Sparkles className="w-3 h-3 text-accent" />
          </div>
        </div>
      </div>

      {/* Right Action Icons & WWPlayer Round Window Control Buttons */}
      <div className="flex items-center space-x-2 titlebar-nodrag pointer-events-auto">
        {/* Search Modal Trigger Button */}
        <button
          onClick={() => setShowSearchModal(!showSearchModal)}
          className="w-8 h-8 rounded-full glass-panel border border-theme text-primary flex items-center justify-center transition-all shadow-sm active:scale-95 hover:border-accent"
          title="全网海量影视搜索"
        >
          <Search className="w-3.5 h-3.5 text-accent" />
        </button>

        {/* WWPlayer Floating Search Input Bar */}
        {showSearchModal && (
          <form
            onSubmit={handleSearchSubmit}
            className="absolute top-12 right-24 w-80 glass-modal border border-theme rounded-2xl p-2 shadow-2xl animate-fade-in z-50"
          >
            <div className="relative">
              <input
                type="text"
                autoFocus
                placeholder="搜索全网海量影视、动漫、综艺..."
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                className="w-full input-bg text-xs text-primary placeholder-slate-400 rounded-xl pl-9 pr-3 py-2 border border-theme focus:border-accent focus:outline-none"
              />
              <Search className="w-4 h-4 text-slate-400 absolute left-2.5 top-1/2 -translate-y-1/2" />
            </div>
          </form>
        )}

        {/* Windows Round Control Buttons */}
        <div className="flex items-center space-x-1.5 pl-2">
          <button
            onClick={() => api.windowControl('minimize')}
            className="w-7 h-7 rounded-full glass-panel border border-theme text-primary flex items-center justify-center transition-all shadow-sm"
            title="最小化"
          >
            <Minus className="w-3 h-3" />
          </button>
          <button
            onClick={() => api.windowControl('maximize')}
            className="w-7 h-7 rounded-full glass-panel border border-theme text-primary flex items-center justify-center transition-all shadow-sm"
            title="最大化"
          >
            <Square className="w-3 h-3" />
          </button>
          <button
            onClick={() => api.windowControl('close')}
            className="w-7 h-7 rounded-full glass-panel hover:bg-red-600/80 hover:text-white border border-theme text-primary flex items-center justify-center transition-all shadow-sm"
            title="关闭"
          >
            <X className="w-3 h-3" />
          </button>
        </div>
      </div>
    </header>
  );
};
