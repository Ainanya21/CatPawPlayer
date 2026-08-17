import React from 'react';
import { Trash2, History, Heart, Play } from 'lucide-react';
import { HistoryItem, VodItem } from '../types';

interface HistoryFavoritesProps {
  type: 'history' | 'favorites';
  historyList: HistoryItem[];
  favoriteList: VodItem[];
  onSelectHistory: (item: HistoryItem) => void;
  onSelectFavorite: (item: VodItem) => void;
  onClearHistory: () => void;
}

export const HistoryFavorites: React.FC<HistoryFavoritesProps> = ({
  type,
  historyList,
  favoriteList,
  onSelectHistory,
  onSelectFavorite,
  onClearHistory,
}) => {
  if (type === 'history') {
    return (
      <div className="p-6 space-y-6 flex-1 max-w-7xl mx-auto w-full select-none">
        <div className="flex items-center justify-between border-b border-theme pb-4">
          <div className="flex items-center space-x-3">
            <div className="w-9 h-9 rounded-xl bg-accent text-white flex items-center justify-center shadow-accent">
              <History className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-base font-extrabold text-primary">播放历史记录</h2>
              <p className="text-[11px] text-secondary">自动记忆您上次观看的剧集与精确播放进度</p>
            </div>
          </div>

          {historyList.length > 0 && (
            <button
              onClick={onClearHistory}
              className="flex items-center space-x-1 px-3 py-1.5 rounded-xl glass-card text-xs font-bold text-rose-500 hover:bg-rose-500/10 border border-theme"
            >
              <Trash2 className="w-3.5 h-3.5" />
              <span>清空全部历史</span>
            </button>
          )}
        </div>

        {historyList.length === 0 ? (
          <div className="p-12 glass-card rounded-3xl text-center text-xs text-secondary space-y-2">
            <History className="w-10 h-10 mx-auto text-secondary" />
            <p className="font-bold text-primary">暂无播放历史记录</p>
            <p>去首页探索并播放感兴趣的影视作品吧！</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {historyList.map((h) => {
              const progressPct = h.duration > 0 ? Math.min(100, (h.progress / h.duration) * 100) : 0;
              return (
                <div
                  key={h.id}
                  onClick={() => onSelectHistory(h)}
                  className="p-3 rounded-2xl glass-card border border-theme cursor-pointer hover:border-accent transition-all flex space-x-3 group"
                >
                  <img
                    src={h.vodPic || 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=500&q=80'}
                    alt={h.vodName}
                    className="w-16 h-24 object-cover rounded-xl flex-shrink-0"
                  />
                  <div className="flex flex-col justify-between flex-1 min-w-0">
                    <div>
                      <h4 className="text-xs font-bold text-primary truncate group-hover:text-accent">
                        {h.vodName}
                      </h4>
                      <p className="text-[10px] text-accent font-extrabold mt-0.5">
                        上次看到: {h.epName}
                      </p>
                      <p className="text-[9px] text-secondary truncate mt-0.5">源站: {h.siteName}</p>
                    </div>

                    <div className="space-y-1 pt-1">
                      <div className="w-full h-1.5 bg-slate-700/40 rounded-full overflow-hidden">
                        <div className="h-full bg-accent rounded-full" style={{ width: `${progressPct}%` }} />
                      </div>
                      <div className="flex items-center justify-between text-[9px] text-secondary">
                        <span>进度 {Math.round(progressPct)}%</span>
                        <Play className="w-3 h-3 text-accent" />
                      </div>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6 flex-1 max-w-7xl mx-auto w-full select-none">
      <div className="flex items-center justify-between border-b border-theme pb-4">
        <div className="flex items-center space-x-3">
          <div className="w-9 h-9 rounded-xl bg-rose-500 text-white flex items-center justify-center shadow-md">
            <Heart className="w-5 h-5 fill-current" />
          </div>
          <div>
            <h2 className="text-base font-extrabold text-primary">我的收藏库</h2>
            <p className="text-[11px] text-secondary">您标记关注的优质影视作品集合</p>
          </div>
        </div>
      </div>

      {favoriteList.length === 0 ? (
        <div className="p-12 glass-card rounded-3xl text-center text-xs text-secondary space-y-2">
          <Heart className="w-10 h-10 mx-auto text-secondary" />
          <p className="font-bold text-primary">暂无收藏内容</p>
          <p>在剧集详情页中点击【加入收藏】即可将作品保存至此！</p>
        </div>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
          {favoriteList.map((item) => (
            <div
              key={item.vod_id}
              onClick={() => onSelectFavorite(item)}
              className="glass-card rounded-2xl p-2.5 cursor-pointer hover:border-rose-500 transition-all flex flex-col space-y-2 group"
            >
              <div className="aspect-[2/3] w-full rounded-xl overflow-hidden relative">
                <img src={item.vod_pic} alt={item.vod_name} className="w-full h-full object-cover group-hover:scale-105 transition-transform" />
              </div>
              <h4 className="text-xs font-bold text-primary truncate group-hover:text-rose-500">{item.vod_name}</h4>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
