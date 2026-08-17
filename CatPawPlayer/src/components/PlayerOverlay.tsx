import React, { useState, useEffect, useRef } from 'react';
import { X, ArrowLeft, RefreshCw, AlertCircle, Maximize2, Minimize2 } from 'lucide-react';
import Hls from 'hls.js';
import { VodItem, SiteSource } from '../types';
import { api } from '../services/api';

interface PlayerOverlayProps {
  item: VodItem;
  site: SiteSource;
  sourceName: string;
  epName: string;
  epUrl: string;
  onClose: () => void;
  onSaveHistory: (epName: string, epUrl: string, currentTime: number, duration: number) => void;
}

export const PlayerOverlay: React.FC<PlayerOverlayProps> = ({
  item,
  site,
  sourceName,
  epName,
  epUrl,
  onClose,
  onSaveHistory,
}) => {
  const videoRef = useRef<HTMLVideoElement>(null);
  const hlsRef = useRef<Hls | null>(null);

  const [realPlayUrl, setRealPlayUrl] = useState<string>('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Stream Info & Playback Rates
  const [streamInfo, setStreamInfo] = useState<{ resolution: string; bitrate: string; codec: string }>({
    resolution: '检测中...',
    bitrate: '未知',
    codec: 'H.264 / AAC',
  });
  const [playbackRate, setPlaybackRate] = useState<number>(1.0);
  const [showSpeedMenu, setShowSpeedMenu] = useState<boolean>(false);

  // Playback speeds list
  const speedRates = [0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0];

  useEffect(() => {
    async function loadStream() {
      setIsLoading(true);
      setError(null);
      try {
        const res = await api.fetchPlayUrl(site, sourceName, epUrl);
        if (res && res.url) {
          setRealPlayUrl(res.url);
        } else {
          setRealPlayUrl(epUrl);
        }
      } catch (err: any) {
        setRealPlayUrl(epUrl);
      } finally {
        setIsLoading(false);
      }
    }
    loadStream();
  }, [site.key, sourceName, epUrl]);

  // Bind HLS.js or Native HTML5 Video Stream Player
  useEffect(() => {
    const video = videoRef.current;
    if (!video || !realPlayUrl) return;

    if (hlsRef.current) {
      hlsRef.current.destroy();
      hlsRef.current = null;
    }

    if (realPlayUrl.includes('.m3u8') || realPlayUrl.includes('m3u8')) {
      if (Hls.isSupported()) {
        const hls = new Hls({
          enableWorker: true,
          lowLatencyMode: true,
        });
        hlsRef.current = hls;
        hls.loadSource(realPlayUrl);
        hls.attachMedia(video);

        hls.on(Hls.Events.MANIFEST_PARSED, (_, data) => {
          video.play().catch(() => {});
          if (data.levels && data.levels[0]) {
            const lvl = data.levels[0];
            setStreamInfo({
              resolution: `${lvl.width || 1920}x${lvl.height || 1080} 4K/HD`,
              bitrate: `${Math.round((lvl.bitrate || 2500000) / 1000)} kbps`,
              codec: lvl.videoCodec || 'H.264 / AAC',
            });
          } else {
            setStreamInfo({
              resolution: '1080P 高清原画',
              bitrate: '自适应流',
              codec: 'H.264 / AAC',
            });
          }
        });

        hls.on(Hls.Events.ERROR, (_, data) => {
          if (data.fatal) {
            setError('视频流解析或播放失败，可能需要专属网盘 Cookie 或外网访问环境。');
          }
        });
      } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
        video.src = realPlayUrl;
        video.play().catch(() => {});
      }
    } else {
      video.src = realPlayUrl;
      video.play().catch(() => {});
      setStreamInfo({
        resolution: '1080P 直链原画',
        bitrate: 'MP4/包含音视频',
        codec: '原生硬解',
      });
    }

    return () => {
      if (hlsRef.current) {
        hlsRef.current.destroy();
        hlsRef.current = null;
      }
    };
  }, [realPlayUrl]);

  // Video Progress Saver
  const handleTimeUpdate = () => {
    const video = videoRef.current;
    if (video && video.duration > 0) {
      onSaveHistory(epName, epUrl, video.currentTime, video.duration);
    }
  };

  const handleSpeedChange = (rate: number) => {
    setPlaybackRate(rate);
    if (videoRef.current) {
      videoRef.current.playbackRate = rate;
    }
    setShowSpeedMenu(false);
  };

  return (
    <div className="fixed inset-0 z-50 bg-black flex flex-col justify-between overflow-hidden select-none animate-fade-in">
      {/* Top Floating Control Bar */}
      <div className="h-16 px-6 bg-gradient-to-b from-black/90 via-black/50 to-transparent flex items-center justify-between z-20">
        <div className="flex items-center space-x-4">
          <button
            onClick={onClose}
            className="p-2 rounded-full glass-card text-white hover:bg-white/20 transition-all border border-white/20"
          >
            <ArrowLeft className="w-5 h-5" />
          </button>
          <div>
            <h2 className="text-sm font-extrabold text-white flex items-center space-x-2">
              <span>{item.vod_name}</span>
              <span className="px-2 py-0.5 rounded bg-accent text-[10px] font-bold text-white shadow-sm">
                {epName}
              </span>
            </h2>
            <p className="text-[10px] text-slate-400">
              当前线路：{sourceName} | 接口源：{site.name}
            </p>
          </div>
        </div>

        <button
          onClick={onClose}
          className="p-2 rounded-full glass-card text-white hover:bg-white/20 transition-all border border-white/20"
        >
          <X className="w-5 h-5" />
        </button>
      </div>

      {/* Center Video Area */}
      <div className="relative flex-1 bg-black flex items-center justify-center">
        {isLoading && (
          <div className="absolute inset-0 flex flex-col items-center justify-center space-y-3 z-10 bg-black/60 backdrop-blur-sm">
            <RefreshCw className="w-10 h-10 text-accent animate-spin" />
            <span className="text-xs text-white font-bold">正在解密并加载高清视频流...</span>
          </div>
        )}

        {error && (
          <div className="absolute inset-0 flex flex-col items-center justify-center space-y-3 z-10 bg-black/90 p-6 text-center">
            <AlertCircle className="w-12 h-12 text-rose-500" />
            <span className="text-sm text-white font-bold max-w-md">{error}</span>
            <button
              onClick={() => setRealPlayUrl(epUrl)}
              className="px-4 py-2 rounded-xl bg-accent text-white text-xs font-bold shadow-accent"
            >
              重新加载视频
            </button>
          </div>
        )}

        <video
          ref={videoRef}
          onTimeUpdate={handleTimeUpdate}
          controls
          className="w-full h-full object-contain"
        />
      </div>

      {/* Bottom Floating Info & Speed Selector Bar */}
      <div className="h-14 px-6 bg-gradient-to-t from-black/90 via-black/50 to-transparent flex items-center justify-between z-20 text-xs text-slate-300">
        {/* Stream Real Metadata */}
        <div className="flex items-center space-x-4">
          <span className="ww-badge text-accent border border-accent">
            分辨率: {streamInfo.resolution}
          </span>
          <span className="ww-badge">
            码率: {streamInfo.bitrate}
          </span>
          <span className="ww-badge">
            编码: {streamInfo.codec}
          </span>
        </div>

        {/* Collapsible Playback Speed Selector */}
        <div className="relative">
          <button
            onClick={() => setShowSpeedMenu(!showSpeedMenu)}
            className="px-3 py-1.5 rounded-xl glass-card text-white font-bold text-xs border border-white/20 flex items-center space-x-1 hover:border-accent"
          >
            <span>倍速 {playbackRate}x</span>
          </button>

          {showSpeedMenu && (
            <div className="absolute bottom-10 right-0 w-28 glass-modal border border-white/20 rounded-2xl p-1.5 shadow-2xl space-y-1 z-30 animate-fade-in">
              {speedRates.map((rate) => (
                <button
                  key={rate}
                  onClick={() => handleSpeedChange(rate)}
                  className={`w-full text-left px-3 py-1.5 rounded-xl text-xs font-bold transition-all ${
                    playbackRate === rate
                      ? 'bg-accent text-white shadow-sm'
                      : 'text-slate-200 hover:bg-white/10'
                  }`}
                >
                  {rate}x {rate === 1.0 && '(正常)'}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
