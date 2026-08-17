import React, { useState } from 'react';
import { Play } from 'lucide-react';
import { VodItem } from '../types';

interface MediaCardProps {
  item: VodItem;
  onSelect: (item: VodItem) => void;
}

export const MediaCard: React.FC<MediaCardProps> = ({ item, onSelect }) => {
  const [imgError, setImgError] = useState(false);

  const fallbackImg = 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=500&q=80';

  return (
    <div
      onClick={() => onSelect(item)}
      className="group relative rounded-2xl overflow-hidden glass-card cursor-pointer flex flex-col transform transition-all duration-300 hover:-translate-y-1.5 hover:shadow-2xl select-none"
    >
      {/* Aspect Ratio 2:3 Cover Poster */}
      <div className="relative aspect-[2/3] w-full overflow-hidden bg-slate-800">
        <img
          src={imgError || !item.vod_pic ? fallbackImg : item.vod_pic}
          alt={item.vod_name}
          onError={() => setImgError(true)}
          loading="lazy"
          className="w-full h-full object-cover transform transition-transform duration-500 group-hover:scale-105"
        />

        {/* WWPlayer Corner Remarks / Stream Quality Badge */}
        {item.vod_remarks && (
          <div className="absolute top-2 right-2 z-10">
            <span className="ww-badge shadow-md">
              {item.vod_remarks}
            </span>
          </div>
        )}

        {/* Hover Play Overlay Icon - Optimized per user request */}
        <div className="absolute inset-0 bg-slate-950/40 opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex items-center justify-center">
          <div className="w-12 h-12 rounded-full bg-accent text-white flex items-center justify-center shadow-accent transform scale-75 group-hover:scale-100 transition-all duration-300">
            <Play className="w-6 h-6 fill-current ml-1" />
          </div>
        </div>
      </div>

      {/* Title & Subtitle Metadata */}
      <div className="p-3 flex flex-col justify-between flex-1 space-y-1">
        <h3 className="font-bold text-xs text-primary line-clamp-1 group-hover:text-accent transition-colors">
          {item.vod_name}
        </h3>
        <p className="text-[10px] text-secondary line-clamp-1 font-medium">
          {item.type_name || item.vod_year || '高清视频'}
        </p>
      </div>
    </div>
  );
};
