import React from 'react';
import { Home, Grid, Heart, Clock, Layers, Settings } from 'lucide-react';

export type TabType = 'home' | 'category' | 'history' | 'favorites' | 'search' | 'subscriptions' | 'settings';

interface SidebarProps {
  activeTab: TabType;
  onTabChange: (tab: TabType) => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ activeTab, onTabChange }) => {
  const items = [
    { id: 'subscriptions' as TabType, label: '订阅管理', icon: Layers },
    { id: 'home' as TabType, label: '首页推荐', icon: Home },
    { id: 'category' as TabType, label: '全量分类', icon: Grid },
    { id: 'history' as TabType, label: '观看历史', icon: Clock },
    { id: 'favorites' as TabType, label: '我的收藏', icon: Heart },
    { id: 'settings' as TabType, label: '设置中心', icon: Settings },
  ];

  return (
    <aside className="w-16 h-full glass-panel border-r border-theme flex flex-col items-center py-4 z-40">
      <div className="flex-1 space-y-3">
        {items.map((item) => {
          const Icon = item.icon;
          const isActive = activeTab === item.id;
          return (
            <button
              key={item.id}
              onClick={() => onTabChange(item.id)}
              className={`w-11 h-11 rounded-2xl flex items-center justify-center transition-all ${
                isActive
                  ? 'bg-accent text-white shadow-accent'
                  : 'text-secondary hover:text-primary hover:bg-accent-subtle'
              }`}
              title={item.label}
            >
              <Icon className="w-5 h-5" />
            </button>
          );
        })}
      </div>
    </aside>
  );
};
