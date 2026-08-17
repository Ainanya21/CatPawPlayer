import React from 'react';
import { CategoryItem } from '../types';

interface CategoryBarProps {
  categories: CategoryItem[];
  activeCategory: string;
  onSelectCategory: (typeId: string) => void;
  filters?: Record<string, any>;
  activeExtend?: Record<string, string>;
  onSelectFilter?: (key: string, value: string) => void;
}

export const CategoryBar: React.FC<CategoryBarProps> = ({
  categories,
  activeCategory,
  onSelectCategory,
  filters,
  activeExtend = {},
  onSelectFilter,
}) => {
  const currentFilters = activeCategory && filters ? filters[activeCategory] : null;

  return (
    <div className="w-full glass-panel border-b border-theme p-4 space-y-3 select-none">
      {/* Category Tabs Scrollbar */}
      <div className="flex items-center space-x-2 overflow-x-auto no-scrollbar">
        <button
          onClick={() => onSelectCategory('')}
          className={`px-4 py-2 rounded-xl text-xs font-bold transition-all flex-shrink-0 ${
            activeCategory === ''
              ? 'bg-accent text-white shadow-accent'
              : 'glass-card text-secondary hover:text-primary'
          }`}
        >
          全部分类
        </button>

        {categories.map((cat) => {
          const isActive = activeCategory === cat.type_id;
          return (
            <button
              key={cat.type_id}
              onClick={() => onSelectCategory(cat.type_id)}
              className={`px-4 py-2 rounded-xl text-xs font-bold transition-all flex-shrink-0 ${
                isActive
                  ? 'bg-accent text-white shadow-accent'
                  : 'glass-card text-secondary hover:text-primary'
              }`}
            >
              {cat.type_name}
            </button>
          );
        })}
      </div>

      {/* Category Filters Pills */}
      {currentFilters && Array.isArray(currentFilters) && currentFilters.length > 0 && (
        <div className="space-y-2 pt-2 border-t border-theme">
          {currentFilters.map((group: any) => (
            <div key={group.key} className="flex items-center space-x-2 overflow-x-auto no-scrollbar text-xs">
              <span className="text-secondary font-bold flex-shrink-0 min-w-12">{group.name}:</span>
              <div className="flex items-center space-x-1.5 overflow-x-auto no-scrollbar">
                {group.value?.map((opt: any) => {
                  const isOptActive = (activeExtend[group.key] || '') === opt.v;
                  return (
                    <button
                      key={opt.v}
                      onClick={() => onSelectFilter && onSelectFilter(group.key, opt.v)}
                      className={`px-2.5 py-1 rounded-lg text-[11px] font-semibold transition-all flex-shrink-0 ${
                        isOptActive
                          ? 'bg-accent-subtle text-accent border border-accent font-bold'
                          : 'text-secondary hover:text-primary hover:bg-slate-500/10'
                      }`}
                    >
                      {opt.n}
                    </button>
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
