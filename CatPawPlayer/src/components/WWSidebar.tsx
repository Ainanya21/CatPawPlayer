import React from 'react';
import { Home, Grid, Heart, Clock, RefreshCw, Settings, Layers, ArrowLeft, Cat } from 'lucide-react';
import { TabType } from './Sidebar';

interface WWSidebarProps {
  activeTab: TabType;
  onTabChange: (tab: TabType) => void;
  onRefresh: () => void;
  canGoBack?: boolean;
  onGoBack?: () => void;
}

export const WWSidebar: React.FC<WWSidebarProps> = ({
  activeTab,
  onTabChange,
  onRefresh,
  canGoBack = false,
  onGoBack,
}) => {
  const mainNavItems = [
    { id: 'subscriptions' as TabType, label: '订阅源管理', icon: Layers },
    { id: 'home' as TabType, label: '首页推荐', icon: Home },
    { id: 'category' as TabType, label: '全量分类', icon: Grid },
    { id: 'history' as TabType, label: '观看历史', icon: Clock },
    { id: 'favorites' as TabType, label: '我的收藏', icon: Heart },
  ];

  return (
    <aside className="w-16 h-full glass-panel border-r border-theme flex flex-col items-center justify-between py-4 z-40 select-none gpu-accel">
      {/* Top Stack: Logo & Back Button */}
      <div className="flex flex-col items-center space-y-4">
        {/* CatPaw Brand Logo Button */}
        <div
          onClick={() => onTabChange('home')}
          className="relative group cursor-pointer"
          title="CatPaw 聚合播放器"
        >
          <div className="w-10 h-10 rounded-2xl bg-accent text-white flex items-center justify-center shadow-accent transform transition-transform group-hover:scale-105">
            <Cat className="w-5 h-5 transform -rotate-6" />
          </div>
          <div className="absolute -bottom-1 -right-1 w-3.5 h-3.5 rounded-full bg-emerald-500 border-2 border-slate-900" />
        </div>

        {/* Optional Back Arrow */}
        {canGoBack && (
          <button
            onClick={onGoBack}
            className="p-2.5 rounded-xl glass-card text-primary transition-all shadow-sm active:scale-95"
            title="返回前一页"
          >
            <ArrowLeft className="w-4 h-4" />
          </button>
        )}
      </div>

      {/* Center Nav Stack (Icon buttons with active indicator dot) */}
      <nav className="flex flex-col items-center space-y-2.5 w-full px-2">
        {mainNavItems.map((item) => {
          const Icon = item.icon;
          const isActive = activeTab === item.id;

          return (
            <button
              key={item.id}
              onClick={() => onTabChange(item.id)}
              className={`relative group w-11 h-11 rounded-2xl flex items-center justify-center transition-all duration-300 ${
                isActive
                  ? 'bg-accent text-white shadow-accent border border-white/20'
                  : 'text-secondary hover:text-primary hover:bg-accent-subtle border border-transparent'
              }`}
              title={item.label}
            >
              <Icon className="w-5 h-5 transition-transform group-hover:scale-110" />

              {/* Active Indicator Bar */}
              {isActive && (
                <span className="absolute -left-1.5 top-1/2 -translate-y-1/2 w-1.5 h-4 bg-accent rounded-r-full shadow-accent" />
              )}
            </button>
          );
        })}
      </nav>

      {/* Bottom Action Stack */}
      <div className="flex flex-col items-center space-y-2.5">
        <button
          onClick={onRefresh}
          className="p-2.5 rounded-xl text-secondary hover:text-primary hover:bg-accent-subtle border border-transparent transition-all active:rotate-180"
          title="刷新全部数据"
        >
          <RefreshCw className="w-4 h-4" />
        </button>

        <button
          onClick={() => onTabChange('settings')}
          className={`relative group w-11 h-11 rounded-2xl flex items-center justify-center transition-all duration-300 ${
            activeTab === 'settings'
              ? 'bg-accent text-white shadow-accent border border-white/20'
              : 'text-secondary hover:text-accent hover:bg-accent-subtle border border-transparent'
          }`}
          title="系统与网盘配置中心"
        >
          <Settings className="w-5 h-5" />
          {activeTab === 'settings' && (
            <span className="absolute -left-1.5 top-1/2 -translate-y-1/2 w-1.5 h-4 bg-accent rounded-r-full shadow-accent" />
          )}
        </button>
      </div>
    </aside>
  );
};
