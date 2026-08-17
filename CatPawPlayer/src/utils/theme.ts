export type ThemeMode = 'light' | 'dark' | 'system';

export interface ThemeConfig {
  mode: ThemeMode;
  accentColor: string;
}

export const PRESET_ACCENT_COLORS = [
  { name: '紫罗兰', color: '#6366f1' },
  { name: '翡翠绿', color: '#10b981' },
  { name: '玫瑰红', color: '#f43f5e' },
  { name: '天空蓝', color: '#0ea5e9' },
  { name: '琥珀金', color: '#f59e0b' },
  { name: '暗影金', color: '#d97706' },
];

export function getSavedThemeMode(): ThemeMode {
  const saved = localStorage.getItem('catpaw_theme_mode');
  if (saved === 'light' || saved === 'dark' || saved === 'system') {
    return saved;
  }
  return 'light'; // Default Light mode as requested
}

export function getSavedAccentColor(): string {
  return localStorage.getItem('catpaw_theme_color') || '#6366f1';
}

export function saveThemeMode(mode: ThemeMode) {
  localStorage.setItem('catpaw_theme_mode', mode);
  applyTheme(mode, getSavedAccentColor());
}

export function saveAccentColor(color: string) {
  localStorage.setItem('catpaw_theme_color', color);
  applyTheme(getSavedThemeMode(), color);
}

export function applyTheme(mode: ThemeMode = getSavedThemeMode(), accentColor: string = getSavedAccentColor()) {
  let isDark = false;

  if (mode === 'system') {
    isDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
  } else {
    isDark = mode === 'dark';
  }

  // Toggle body theme class
  if (isDark) {
    document.body.classList.remove('theme-light');
    document.body.classList.add('theme-dark');
  } else {
    document.body.classList.remove('theme-dark');
    document.body.classList.add('theme-light');
  }

  // Set CSS Custom Accent Color variables
  document.documentElement.style.setProperty('--accent-color', accentColor);
  document.documentElement.style.setProperty('--accent-light', `${accentColor}26`);
  document.documentElement.style.setProperty('--accent-glow', `${accentColor}59`);
}

// System color scheme change listener
if (typeof window !== 'undefined' && window.matchMedia) {
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    if (getSavedThemeMode() === 'system') {
      applyTheme('system');
    }
  });
}
