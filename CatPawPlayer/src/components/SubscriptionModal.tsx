import React, { useState, useEffect } from 'react';
import { X, Link, Check, Plus, Trash2, RefreshCw, AlertCircle, Sparkles, CheckCircle2, Circle } from 'lucide-react';
import { SiteSource } from '../types';
import { api } from '../services/api';
import { DEFAULT_SUBSCRIPTION_URLS } from '../services/defaultSources';

export interface SubscriptionItem {
  id: string;
  name: string;
  url: string;
  siteCount?: number;
  updatedAt: number;
}

interface SubscriptionModalProps {
  onClose?: () => void;
  currentSites: SiteSource[];
  activeSubUrl: string;
  savedSubscriptions: SubscriptionItem[];
  onSelectSubscription: (subUrl: string, sites: SiteSource[], subName?: string) => void;
  onSaveSubscriptions: (subs: SubscriptionItem[]) => void;
  isEmbedded?: boolean;
}

export const SubscriptionModal: React.FC<SubscriptionModalProps> = ({
  onClose,
  currentSites,
  activeSubUrl,
  savedSubscriptions,
  onSelectSubscription,
  onSaveSubscriptions,
  isEmbedded = false,
}) => {
  const [subInput, setSubInput] = useState('');
  const [loadingUrl, setLoadingUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const [subs, setSubs] = useState<SubscriptionItem[]>(() => {
    if (savedSubscriptions && savedSubscriptions.length > 0) {
      return savedSubscriptions;
    }
    return DEFAULT_SUBSCRIPTION_URLS.map((item, i) => ({
      id: `default_${i}`,
      name: item.name,
      url: item.url,
      siteCount: 0,
      updatedAt: Date.now(),
    }));
  });

  useEffect(() => {
    onSaveSubscriptions(subs);
  }, [subs]);

  const formatCleanName = (name: string) => {
    return name.replace(/\[猫源JS\]|\[CMS\]|\[猫源\]|\[JS\]/g, '').trim();
  };

  const handleImportAndSelect = async (urlToImport: string, customName?: string) => {
    const trimmed = urlToImport.trim();
    if (!trimmed) return;
    setLoadingUrl(trimmed);
    setError(null);
    setSuccessMsg(null);

    try {
      const config = await api.fetchSubscription(trimmed);
      if (config && Array.isArray(config.sites) && config.sites.length > 0) {
        const fetchedSites = config.sites;
        const name = customName || (trimmed.endsWith('.md5') ? '猫源官方 JS MD5' : `订阅源 ${trimmed.slice(-15)}`);

        const existingIdx = subs.findIndex((s) => s.url === trimmed);
        let updatedSubs: SubscriptionItem[];

        if (existingIdx >= 0) {
          updatedSubs = [...subs];
          updatedSubs[existingIdx] = {
            ...updatedSubs[existingIdx],
            siteCount: fetchedSites.length,
            updatedAt: Date.now(),
          };
        } else {
          const newSub: SubscriptionItem = {
            id: `sub_${Date.now()}`,
            name,
            url: trimmed,
            siteCount: fetchedSites.length,
            updatedAt: Date.now(),
          };
          updatedSubs = [newSub, ...subs];
        }

        setSubs(updatedSubs);
        onSelectSubscription(trimmed, fetchedSites, name);
        setSuccessMsg(`成功加载并切换至订阅 [${name}]，共 ${fetchedSites.length} 个站点！`);
      } else {
        setError('订阅解析失败：格式不符合 TVBox/CatPaw 规范或站点列表为空。');
      }
    } catch (err: any) {
      setError(`导入失败: ${err.message}`);
    } finally {
      setLoadingUrl(null);
    }
  };

  const handleDeleteSub = (subId: string, subUrl: string) => {
    const filtered = subs.filter((s) => s.id !== subId);
    setSubs(filtered);
    if (activeSubUrl === subUrl && filtered.length > 0) {
      handleImportAndSelect(filtered[0].url, filtered[0].name);
    }
  };

  const contentUI = (
    <div className={`relative w-full ${isEmbedded ? 'h-full' : 'max-w-3xl max-h-[85vh]'} glass-panel rounded-3xl overflow-hidden shadow-2xl flex flex-col border border-theme p-6 space-y-5 select-none text-primary`}>
      {/* Top bar */}
      <div className="flex items-center justify-between border-b border-theme pb-4">
        <div className="flex items-center space-x-3">
          <div className="w-9 h-9 rounded-xl bg-accent text-white flex items-center justify-center shadow-accent">
            <Link className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-base font-extrabold text-primary tracking-tight">订阅管理与源站记忆中心</h2>
            <p className="text-[11px] text-secondary">勾选切换订阅源，系统自动记忆并在下次启动时无缝恢复使用</p>
          </div>
        </div>

        {onClose && !isEmbedded && (
          <button
            onClick={onClose}
            className="p-2 rounded-xl glass-card text-secondary transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        )}
      </div>

      {/* Input Sub URL */}
      <div className="space-y-2">
        <label className="text-xs font-semibold text-primary">添加新的订阅源地址 (JS MD5 / JSON):</label>
        <div className="flex items-center space-x-2">
          <input
            type="text"
            placeholder="请输入 TVBox / CatPaw 格式的订阅接口 URL (支持 .js.md5 或 .json)..."
            value={subInput}
            onChange={(e) => setSubInput(e.target.value)}
            className="flex-1 input-bg text-xs text-primary placeholder-slate-400 rounded-xl px-4 py-2.5 border border-theme focus:border-accent focus:outline-none"
          />
          <button
            onClick={() => handleImportAndSelect(subInput)}
            disabled={!!loadingUrl}
            className="flex items-center space-x-1.5 px-5 py-2.5 rounded-xl bg-accent text-white font-bold text-xs shadow-accent transition-all disabled:opacity-50"
          >
            {loadingUrl === subInput.trim() ? <RefreshCw className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
            <span>保存并选定</span>
          </button>
        </div>

        {error && (
          <div className="flex items-center space-x-2 p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-xs text-red-400 animate-fade-in">
            <AlertCircle className="w-4 h-4 flex-shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {successMsg && (
          <div className="flex items-center space-x-2 p-3 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-xs text-emerald-500 font-semibold animate-fade-in">
            <Check className="w-4 h-4 flex-shrink-0" />
            <span>{successMsg}</span>
          </div>
        )}
      </div>

      {/* Saved Subscription List */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-xs font-extrabold text-primary flex items-center space-x-1">
            <Sparkles className="w-3.5 h-3.5 text-accent" />
            <span>已保存的订阅源列表 (点击可勾选激活并记忆):</span>
          </span>
          <span className="text-[11px] text-secondary">共 {subs.length} 条</span>
        </div>

        <div className="max-h-48 overflow-y-auto space-y-2 pr-1">
          {subs.map((sub) => {
            const isActive = activeSubUrl === sub.url;
            const isLoadingThis = loadingUrl === sub.url;

            return (
              <div
                key={sub.id}
                onClick={() => !isLoadingThis && handleImportAndSelect(sub.url, sub.name)}
                className={`p-3 rounded-2xl border cursor-pointer transition-all flex items-center justify-between ${
                  isActive
                    ? 'bg-accent-subtle border-accent shadow-accent'
                    : 'glass-card hover:border-accent'
                }`}
              >
                <div className="flex items-center space-x-3 min-w-0 pr-2">
                  <div className="text-accent flex-shrink-0">
                    {isActive ? (
                      <CheckCircle2 className="w-5 h-5 fill-current text-accent" />
                    ) : (
                      <Circle className="w-5 h-5 text-secondary hover:text-primary" />
                    )}
                  </div>

                  <div className="min-w-0">
                    <div className="flex items-center space-x-2">
                      <span className="font-bold text-xs truncate text-primary">
                        {formatCleanName(sub.name)}
                      </span>
                      {isActive && (
                        <span className="px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-[9px] font-extrabold border border-emerald-500/30 flex-shrink-0">
                          正在生效
                        </span>
                      )}
                    </div>
                    <p className="text-[10px] text-secondary truncate font-mono mt-0.5">{sub.url}</p>
                  </div>
                </div>

                <div className="flex items-center space-x-2 flex-shrink-0">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      handleImportAndSelect(sub.url, sub.name);
                    }}
                    disabled={isLoadingThis}
                    className="p-1.5 rounded-lg glass-card text-secondary hover:text-primary transition-colors"
                    title="重新拉取此源"
                  >
                    <RefreshCw className={`w-3.5 h-3.5 ${isLoadingThis ? 'animate-spin text-accent' : ''}`} />
                  </button>

                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      handleDeleteSub(sub.id, sub.url);
                    }}
                    className="p-1.5 rounded-lg glass-card text-secondary hover:text-red-500 transition-colors"
                    title="删除此订阅源"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Current Active Sites List */}
      <div className="space-y-2 pt-3 border-t border-theme flex-1 overflow-hidden flex flex-col">
        <div className="flex items-center justify-between">
          <span className="text-xs font-bold text-primary">
            当前激活源包含站点列表 ({currentSites.length} 个):
          </span>
        </div>

        <div className="flex-1 overflow-y-auto space-y-1.5 pr-1 max-h-48">
          {currentSites.map((site) => (
            <div
              key={site.key}
              className="flex items-center justify-between p-2.5 rounded-xl glass-card text-xs"
            >
              <div className="flex items-center space-x-2 min-w-0 pr-2">
                <span className="font-semibold text-primary truncate">{formatCleanName(site.name)}</span>
              </div>
              <span className="text-[10px] text-secondary truncate max-w-xs font-mono">{site.api}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );

  if (isEmbedded) {
    return contentUI;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/70 backdrop-blur-md animate-fade-in">
      {contentUI}
    </div>
  );
};
