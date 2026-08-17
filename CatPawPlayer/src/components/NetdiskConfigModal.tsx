import React, { useState, useEffect } from 'react';
import { X, Cloud, QrCode, Key, ShieldCheck, Globe, Sliders, ExternalLink, Palette, Sun, Moon, Laptop, Check } from 'lucide-react';
import { api } from '../services/api';
import {
  ThemeMode,
  PRESET_ACCENT_COLORS,
  getSavedThemeMode,
  getSavedAccentColor,
  saveThemeMode,
  saveAccentColor,
  applyTheme,
} from '../utils/theme';

interface NetdiskConfigModalProps {
  onClose?: () => void;
  defaultMode?: 'native' | 'web' | 'theme';
  isEmbedded?: boolean;
}

export const NetdiskConfigModal: React.FC<NetdiskConfigModalProps> = ({
  onClose,
  defaultMode = 'native',
  isEmbedded = false,
}) => {
  const [viewMode, setViewMode] = useState<'native' | 'web' | 'theme'>(defaultMode);
  const [catPawPort, setCatPawPort] = useState<number | null>(null);

  // Theme states
  const [themeMode, setThemeMode] = useState<ThemeMode>(getSavedThemeMode());
  const [accentColor, setAccentColor] = useState<string>(getSavedAccentColor());

  const [credentials, setCredentials] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [activeProvider, setActiveProvider] = useState<string>('quark');
  const [cookieInput, setCookieInput] = useState<string>('');

  // QR Code Login state
  const [qrData, setQrData] = useState<{ qrImage?: string; qrUrl?: string; taskId?: string; msg?: string } | null>(null);
  const [qrLoading, setQrLoading] = useState(false);
  const [qrStatusText, setQrStatusText] = useState<string>('');

  useEffect(() => {
    loadCredentials();
    loadPort();
    applyTheme(themeMode, accentColor);
  }, []);

  const loadPort = async () => {
    const p = await api.getCatPawPort();
    setCatPawPort(p);
  };

  const loadCredentials = async () => {
    setLoading(true);
    try {
      const res = await api.getNetdiskCredentials();
      if (res) {
        setCredentials(res);
        if (activeProvider === 'quark' && res.quark?.cookie) setCookieInput(res.quark.cookie);
        if (activeProvider === 'baidu' && res.baidu?.cookie) setCookieInput(res.baidu.cookie);
        if (activeProvider === 'pan115' && res.pan115?.cookie) setCookieInput(res.pan115.cookie);
      }
    } catch (e) {}
    setLoading(false);
  };

  const handleStartQrLogin = async (provider: string) => {
    setQrLoading(true);
    setQrData(null);
    setQrStatusText('正在请求官方网盘扫码 Token...');
    try {
      const res = await api.startNetdiskQrLogin(provider);
      if (res && res.code === 0) {
        setQrData({
          qrImage: res.qrImage,
          qrUrl: res.qrUrl,
          taskId: res.taskId,
          msg: res.msg || '请使用 App 扫码登录',
        });
        setQrStatusText(res.msg || '请打开网盘 App 扫描二维码');
        if (res.taskId) {
          pollQrStatus(provider, res.taskId);
        }
      } else {
        setQrStatusText(res?.msg || '扫码初始化失败');
      }
    } catch (e: any) {
      setQrStatusText(`错误: ${e.message}`);
    } finally {
      setQrLoading(false);
    }
  };

  const pollQrStatus = async (provider: string, taskId: string) => {
    let attempts = 0;
    const interval = setInterval(async () => {
      attempts++;
      if (attempts > 40) {
        clearInterval(interval);
        setQrStatusText('二维码已过期，请重新刷新');
        return;
      }

      try {
        const pollRes = await api.pollNetdiskQrLogin(provider, taskId);
        if (pollRes && pollRes.code === 0 && pollRes.status === 'success') {
          clearInterval(interval);
          setQrStatusText('🎉 扫码登录成功！网络凭证已同步保存');
          await loadCredentials();
        } else if (pollRes && pollRes.msg) {
          setQrStatusText(pollRes.msg);
        }
      } catch (e) {}
    }, 3000);
  };

  const handleSaveCookie = async () => {
    if (!cookieInput.trim()) return;
    try {
      await api.saveNetdiskCookie(activeProvider, cookieInput.trim());
      await loadCredentials();
    } catch (e) {}
  };

  const handleThemeModeChange = (mode: ThemeMode) => {
    setThemeMode(mode);
    saveThemeMode(mode);
  };

  const handleAccentColorChange = (color: string) => {
    setAccentColor(color);
    saveAccentColor(color);
  };

  const providers = [
    { id: 'quark', name: '夸克网盘', icon: '⚡', desc: '支持夸克原画/极速 4K 播放', hasQr: true },
    { id: 'baidu', name: '百度网盘', icon: '📦', desc: '支持百度原画视频防盗链', hasQr: true },
    { id: 'pan115', name: '115 云盘', icon: '🚀', desc: '支持 115 磁力与大文件直链', hasQr: false },
    { id: 'pan123', name: '123 云盘', icon: '☁️', desc: '支持 123 盘高清解包', hasQr: false },
    { id: 'pan189', name: '天翼云盘', icon: '🕊️', desc: '支持天翼云盘一键解析', hasQr: true },
  ];

  const catPawWebUrl = catPawPort ? `http://127.0.0.1:${catPawPort}/website` : 'http://127.0.0.1:9988/website';

  const contentUI = (
    <div className={`relative w-full ${isEmbedded ? 'h-full' : 'max-w-4xl h-[85vh]'} glass-panel rounded-3xl overflow-hidden shadow-2xl flex flex-col border border-theme select-none text-primary`}>
      {/* Modal Header */}
      <div className="px-6 py-3.5 border-b border-theme flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <div className="flex items-center space-x-2.5">
            <div className="w-9 h-9 rounded-xl bg-accent text-white flex items-center justify-center shadow-accent">
              <Cloud className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-base font-extrabold text-primary tracking-tight">系统、主题与网盘配置中心</h2>
              <p className="text-[11px] text-secondary font-medium">配置主题深浅模式、自定义主调色彩以及夸克/百度网盘凭证</p>
            </div>
          </div>

          {/* Mode Switch Pills */}
          <div className="flex items-center space-x-1 input-bg p-1 rounded-xl border border-theme">
            <button
              onClick={() => setViewMode('native')}
              className={`px-3 py-1 rounded-lg text-xs font-bold transition-all ${
                viewMode === 'native'
                  ? 'bg-accent text-white shadow-sm'
                  : 'text-secondary hover:text-primary'
              }`}
            >
              <span className="flex items-center space-x-1">
                <Sliders className="w-3.5 h-3.5" />
                <span>快捷网盘绑定</span>
              </span>
            </button>

            <button
              onClick={() => setViewMode('theme')}
              className={`px-3 py-1 rounded-lg text-xs font-bold transition-all ${
                viewMode === 'theme'
                  ? 'bg-accent text-white shadow-sm'
                  : 'text-secondary hover:text-primary'
              }`}
            >
              <span className="flex items-center space-x-1">
                <Palette className="w-3.5 h-3.5" />
                <span>主题与外观</span>
              </span>
            </button>

            <button
              onClick={() => setViewMode('web')}
              className={`px-3 py-1 rounded-lg text-xs font-bold transition-all ${
                viewMode === 'web'
                  ? 'bg-accent text-white shadow-sm'
                  : 'text-secondary hover:text-primary'
              }`}
            >
              <span className="flex items-center space-x-1">
                <Globe className="w-3.5 h-3.5" />
                <span>猫源内置网页配置</span>
              </span>
            </button>
          </div>
        </div>

        <div className="flex items-center space-x-2">
          {viewMode === 'web' && (
            <a
              href={catPawWebUrl}
              target="_blank"
              rel="noreferrer"
              className="px-2.5 py-1 rounded-lg glass-card text-secondary text-xs font-medium border border-theme flex items-center space-x-1"
              title="在默认浏览器中打开网页版"
            >
              <ExternalLink className="w-3.5 h-3.5" />
              <span>浏览器打开</span>
            </a>
          )}

          {onClose && !isEmbedded && (
            <button
              onClick={onClose}
              className="p-2 rounded-full glass-card text-secondary border border-theme transition-colors"
            >
              <X className="w-4 h-4" />
            </button>
          )}
        </div>
      </div>

      {/* Modal Body */}
      {viewMode === 'web' ? (
        <div className="flex-1 w-full h-full app-bg relative">
          <iframe
            src={catPawWebUrl}
            className="w-full h-full border-none"
            title="CatPaw Web Configuration"
          />
        </div>
      ) : viewMode === 'theme' ? (
        /* Appearance & Theme Settings Section */
        <div className="flex-1 p-8 overflow-y-auto space-y-8 glass-panel">
          {/* Theme Mode Selector (Light Default, Dark, System) */}
          <div className="space-y-3">
            <h3 className="text-sm font-extrabold text-primary flex items-center space-x-2">
              <Sun className="w-4 h-4 text-amber-500" />
              <span>色彩显示模式</span>
            </h3>
            <p className="text-xs text-secondary">默认提供极清明亮的浅色白皙模式，支持深色暗夜模式或随 Windows 系统切换</p>

            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-1">
              {/* Light Mode */}
              <div
                onClick={() => handleThemeModeChange('light')}
                className={`p-4 rounded-2xl cursor-pointer transition-all border flex flex-col items-center justify-center space-y-2.5 ${
                  themeMode === 'light'
                    ? 'glass-card border-accent shadow-accent ring-2 ring-accent'
                    : 'glass-card hover:border-slate-400'
                }`}
              >
                <div className="w-10 h-10 rounded-xl bg-amber-500/10 text-amber-500 flex items-center justify-center border border-amber-500/20">
                  <Sun className="w-5 h-5" />
                </div>
                <div className="text-center">
                  <div className="flex items-center space-x-1 justify-center">
                    <span className="text-xs font-bold text-primary">浅色明亮模式</span>
                    <span className="px-1.5 py-0.5 rounded bg-accent-subtle text-accent text-[9px] font-bold">默认</span>
                  </div>
                  <span className="text-[10px] text-secondary">清爽明亮白皙高对比度</span>
                </div>
              </div>

              {/* Dark Mode */}
              <div
                onClick={() => handleThemeModeChange('dark')}
                className={`p-4 rounded-2xl cursor-pointer transition-all border flex flex-col items-center justify-center space-y-2.5 ${
                  themeMode === 'dark'
                    ? 'glass-card border-accent shadow-accent ring-2 ring-accent'
                    : 'glass-card hover:border-slate-400'
                }`}
              >
                <div className="w-10 h-10 rounded-xl bg-accent-subtle text-accent flex items-center justify-center border border-theme">
                  <Moon className="w-5 h-5" />
                </div>
                <div className="text-center">
                  <span className="text-xs font-bold text-primary block">深色暗夜模式</span>
                  <span className="text-[10px] text-secondary">夜间护眼防刺眼质感</span>
                </div>
              </div>

              {/* Follow System */}
              <div
                onClick={() => handleThemeModeChange('system')}
                className={`p-4 rounded-2xl cursor-pointer transition-all border flex flex-col items-center justify-center space-y-2.5 ${
                  themeMode === 'system'
                    ? 'glass-card border-accent shadow-accent ring-2 ring-accent'
                    : 'glass-card hover:border-slate-400'
                }`}
              >
                <div className="w-10 h-10 rounded-xl glass-panel text-primary flex items-center justify-center border border-theme">
                  <Laptop className="w-5 h-5" />
                </div>
                <div className="text-center">
                  <span className="text-xs font-bold text-primary block">跟随 Windows 系统</span>
                  <span className="text-[10px] text-secondary">自动同步操作系统深浅偏好</span>
                </div>
              </div>
            </div>
          </div>

          {/* Accent Color Customization */}
          <div className="space-y-3 pt-4 border-t border-theme">
            <h3 className="text-sm font-extrabold text-primary flex items-center space-x-2">
              <Palette className="w-4 h-4 text-accent" />
              <span>自定义主调色彩 (Accent Color)</span>
            </h3>
            <p className="text-xs text-secondary">选择预设的主主题发光色彩或通过画板自由选取任意 hex 颜色</p>

            <div className="flex flex-wrap items-center gap-3 pt-2">
              {PRESET_ACCENT_COLORS.map((preset) => {
                const isSelected = accentColor.toLowerCase() === preset.color.toLowerCase();
                return (
                  <button
                    key={preset.color}
                    onClick={() => handleAccentColorChange(preset.color)}
                    className={`flex items-center space-x-2 px-3.5 py-2 rounded-xl text-xs font-bold border transition-all ${
                      isSelected
                        ? 'glass-card border-accent text-primary shadow-accent ring-1 ring-accent'
                        : 'glass-card hover:border-slate-400'
                    }`}
                  >
                    <span
                      className="w-4 h-4 rounded-full shadow-inner flex items-center justify-center"
                      style={{ backgroundColor: preset.color }}
                    >
                      {isSelected && <Check className="w-3 h-3 text-white drop-shadow" />}
                    </span>
                    <span>{preset.name}</span>
                  </button>
                );
              })}

              {/* Custom Color Picker Input */}
              <div className="flex items-center space-x-2 pl-2">
                <span className="text-xs font-semibold text-secondary">自定义颜色:</span>
                <input
                  type="color"
                  value={accentColor}
                  onChange={(e) => handleAccentColorChange(e.target.value)}
                  className="w-9 h-9 rounded-xl glass-card border border-theme cursor-pointer p-1"
                />
              </div>
            </div>
          </div>
        </div>
      ) : (
        <div className="flex flex-1 overflow-hidden">
          {/* Provider Selection Sidebar */}
          <div className="w-56 border-r border-theme glass-panel p-3 space-y-1.5 overflow-y-auto">
            <div className="px-2 py-1 text-[10px] font-bold text-secondary uppercase tracking-wider">网盘类型</div>
            {providers.map((p) => {
              const isConnected =
                (p.id === 'quark' && credentials?.quark?.cookie) ||
                (p.id === 'baidu' && credentials?.baidu?.cookie) ||
                (p.id === 'pan115' && credentials?.pan115?.cookie) ||
                (p.id === 'pan123' && credentials?.pan123?.account);
              const isActive = activeProvider === p.id;

              return (
                <button
                  key={p.id}
                  onClick={() => {
                    setActiveProvider(p.id);
                    setQrData(null);
                    if (p.id === 'quark') setCookieInput(credentials?.quark?.cookie || '');
                    if (p.id === 'baidu') setCookieInput(credentials?.baidu?.cookie || '');
                    if (p.id === 'pan115') setCookieInput(credentials?.pan115?.cookie || '');
                  }}
                  className={`w-full flex items-center justify-between p-2.5 rounded-xl font-medium text-xs transition-all ${
                    isActive
                      ? 'bg-accent text-white shadow-accent'
                      : 'text-primary hover:bg-slate-500/10'
                  }`}
                >
                  <div className="flex items-center space-x-2.5">
                    <span className="text-base">{p.icon}</span>
                    <span>{p.name}</span>
                  </div>

                  {isConnected ? (
                    <span className="px-1.5 py-0.5 rounded bg-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-[9px] font-bold border border-emerald-500/30">
                      已关联
                    </span>
                  ) : (
                    <span className="px-1.5 py-0.5 rounded bg-slate-500/10 text-secondary text-[9px]">
                      未配置
                    </span>
                  )}
                </button>
              );
            })}
          </div>

          {/* Main Config Area */}
          <div className="flex-1 p-6 overflow-y-auto space-y-5">
            {loading ? (
              <div className="h-48 flex items-center justify-center text-xs text-secondary animate-pulse">
                正在检索网盘凭证关联状态...
              </div>
            ) : (
              <>
                {/* Active Provider Info */}
                <div className="p-4 rounded-2xl glass-card border border-theme flex items-center justify-between">
                  <div className="flex items-center space-x-3">
                    <span className="text-2xl">
                      {providers.find((p) => p.id === activeProvider)?.icon}
                    </span>
                    <div>
                      <h3 className="text-sm font-bold text-primary">
                        {providers.find((p) => p.id === activeProvider)?.name} 关联设置
                      </h3>
                      <p className="text-xs text-secondary mt-0.5">
                        {providers.find((p) => p.id === activeProvider)?.desc}
                      </p>
                    </div>
                  </div>

                  {providers.find((p) => p.id === activeProvider)?.hasQr && (
                    <button
                      onClick={() => handleStartQrLogin(activeProvider)}
                      disabled={qrLoading}
                      className="px-3.5 py-2 rounded-xl bg-accent text-white text-xs font-bold shadow-accent flex items-center space-x-1.5 transition-all"
                    >
                      <QrCode className="w-4 h-4" />
                      <span>{qrLoading ? '拉取中...' : '扫码一键绑定'}</span>
                    </button>
                  )}
                </div>

                {/* QR Code Login Display Container */}
                {qrData && (
                  <div className="p-5 rounded-2xl bg-accent-subtle border border-accent flex flex-col items-center justify-center space-y-3 animate-fade-in">
                    {qrData.qrImage ? (
                      <div className="p-2 bg-white rounded-xl shadow-xl">
                        <img src={qrData.qrImage} alt="QR Code" className="w-40 h-40" />
                      </div>
                    ) : (
                      <div className="p-4 glass-card rounded-xl text-xs text-secondary">
                        网页扫码链接：<a href={qrData.qrUrl} target="_blank" rel="noreferrer" className="text-accent underline">{qrData.qrUrl}</a>
                      </div>
                    )}

                    <div className="text-center space-y-1">
                      <span className="text-xs font-bold text-accent flex items-center justify-center space-x-1">
                        <ShieldCheck className="w-4 h-4 text-emerald-500" />
                        <span>{qrStatusText || qrData.msg}</span>
                      </span>
                      <p className="text-[10px] text-secondary">请使用对应的手机 App 扫描屏幕上方二维码完成确认</p>
                    </div>
                  </div>
                )}

                {/* Manual Cookie Input Area */}
                <div className="space-y-3 pt-2">
                  <label className="text-xs font-semibold text-primary flex items-center space-x-1.5">
                    <Key className="w-3.5 h-3.5 text-accent" />
                    <span>手动粘贴 Cookie / Token 凭证</span>
                  </label>

                  <textarea
                    rows={4}
                    value={cookieInput}
                    onChange={(e) => setCookieInput(e.target.value)}
                    placeholder={`请在此处粘贴 ${providers.find((p) => p.id === activeProvider)?.name} 的完整的 Cookie 字符串（例如 pudv=...; _tb_token_=...）`}
                    className="w-full input-bg text-xs text-primary placeholder-slate-400 rounded-xl p-3 border border-theme focus:border-accent focus:outline-none font-mono transition-all"
                  />

                  <div className="flex items-center justify-between">
                    <span className="text-[11px] text-secondary">
                      提示：凭证仅加密保存在您本地电脑的 Electron 运行环境中
                    </span>

                    <button
                      onClick={handleSaveCookie}
                      className="px-4 py-2 rounded-xl bg-accent text-white text-xs font-bold transition-all shadow-accent"
                    >
                      保存设置
                    </button>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );

  if (isEmbedded) {
    return contentUI;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/70 backdrop-blur-md animate-fade-in select-none">
      {contentUI}
    </div>
  );
};
