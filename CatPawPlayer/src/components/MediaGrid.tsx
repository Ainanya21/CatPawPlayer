import React from 'react';
import { MediaCard } from './MediaCard';
import { VodItem } from '../types';
import { ChevronLeft, ChevronRight } from 'lucide-react';

interface MediaGridProps {
  items: VodItem[];
  isLoading: boolean;
  onSelectItem: (item: VodItem) => void;
  page?: number;
  pageCount?: number;
  onPageChange?: (newPage: number) => void;
}

export const MediaGrid: React.FC<MediaGridProps> = ({
  items,
  isLoading,
  onSelectItem,
  page = 1,
  pageCount = 1,
  onPageChange,
}) => {
  if (isLoading && items.length === 0) {
    return (
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4 p-4">
        {Array.from({ length: 18 }).map((_, i) => (
          <div key={i} className="aspect-[2/3] rounded-2xl glass-card animate-pulse bg-slate-800/40" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
        {items.map((item) => (
          <MediaCard key={item.vod_id} item={item} onSelect={onSelectItem} />
        ))}
      </div>

      {onPageChange && pageCount > 1 && (
        <div className="flex items-center justify-center space-x-4 pt-4 pb-8">
          <button
            onClick={() => onPageChange(Math.max(1, page - 1))}
            disabled={page <= 1}
            className="p-2.5 rounded-xl glass-card text-primary disabled:opacity-30 disabled:cursor-not-allowed border border-theme"
          >
            <ChevronLeft className="w-5 h-5" />
          </button>
          <span className="text-xs font-bold text-primary">
            第 {page} / {pageCount} 页
          </span>
          <button
            onClick={() => onPageChange(Math.min(pageCount, page + 1))}
            disabled={page >= pageCount}
            className="p-2.5 rounded-xl glass-card text-primary disabled:opacity-30 disabled:cursor-not-allowed border border-theme"
          >
            <ChevronRight className="w-5 h-5" />
          </button>
        </div>
      )}
    </div>
  );
};
