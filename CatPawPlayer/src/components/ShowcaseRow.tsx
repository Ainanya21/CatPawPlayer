import React, { useRef } from 'react';
import { ChevronLeft, ChevronRight, LucideIcon } from 'lucide-react';
import { MediaCard } from './MediaCard';
import { VodItem } from '../types';

interface ShowcaseRowProps {
  title: string;
  icon: LucideIcon;
  items: VodItem[];
  isLoading: boolean;
  onSelectItem: (item: VodItem) => void;
}

export const ShowcaseRow: React.FC<ShowcaseRowProps> = ({
  title,
  icon: Icon,
  items,
  isLoading,
  onSelectItem,
}) => {
  const scrollRef = useRef<HTMLDivElement>(null);

  const scroll = (direction: 'left' | 'right') => {
    if (scrollRef.current) {
      const { scrollLeft, clientWidth } = scrollRef.current;
      const scrollAmount = clientWidth * 0.75;
      scrollRef.current.scrollTo({
        left: direction === 'left' ? scrollLeft - scrollAmount : scrollLeft + scrollAmount,
        behavior: 'smooth',
      });
    }
  };

  if (items.length === 0 && !isLoading) return null;

  return (
    <section className="space-y-3 relative group select-none">
      <div className="flex items-center justify-between px-1">
        <div className="flex items-center space-x-2">
          <div className="w-8 h-8 rounded-xl bg-accent-subtle text-accent flex items-center justify-center border border-theme">
            <Icon className="w-4 h-4" />
          </div>
          <h2 className="text-sm font-extrabold text-primary tracking-tight">
            {title}
          </h2>
        </div>

        <div className="flex items-center space-x-1 opacity-0 group-hover:opacity-100 transition-opacity duration-300">
          <button
            onClick={() => scroll('left')}
            className="p-1.5 rounded-xl glass-card text-secondary hover:text-primary border border-theme active:scale-95"
          >
            <ChevronLeft className="w-4 h-4" />
          </button>
          <button
            onClick={() => scroll('right')}
            className="p-1.5 rounded-xl glass-card text-secondary hover:text-primary border border-theme active:scale-95"
          >
            <ChevronRight className="w-4 h-4" />
          </button>
        </div>
      </div>

      <div
        ref={scrollRef}
        className="flex space-x-4 overflow-x-auto no-scrollbar scroll-smooth pb-2 pt-1 px-1"
        style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
      >
        {isLoading && items.length === 0
          ? Array.from({ length: 8 }).map((_, i) => (
              <div
                key={i}
                className="w-36 sm:w-44 flex-shrink-0 aspect-[2/3] rounded-2xl glass-card animate-pulse bg-slate-800/40"
              />
            ))
          : items.map((item) => (
              <div key={item.vod_id} className="w-36 sm:w-44 flex-shrink-0">
                <MediaCard item={item} onSelect={onSelectItem} />
              </div>
            ))}
      </div>
    </section>
  );
};
