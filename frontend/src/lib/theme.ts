export const THEMES = ['terminus', 'wintry'] as const;

export type ThemeName = (typeof THEMES)[number];

export const DEFAULT_THEME: ThemeName = 'terminus';

const STORAGE_KEY = 'sqldeck-theme';

function isThemeName(value: string | null): value is ThemeName {
	return value !== null && THEMES.includes(value as ThemeName);
}

export function applyTheme(theme: ThemeName): ThemeName {
	if (typeof document === 'undefined') {
		return theme;
	}

	document.documentElement.dataset.theme = theme;
	localStorage.setItem(STORAGE_KEY, theme);

	return theme;
}

export function getStoredTheme(): ThemeName | null {
	if (typeof localStorage === 'undefined') {
		return null;
	}

	const storedTheme = localStorage.getItem(STORAGE_KEY);
	return isThemeName(storedTheme) ? storedTheme : null;
}

export function initializeTheme(): ThemeName {
	return applyTheme(getStoredTheme() ?? DEFAULT_THEME);
}

export function getActiveTheme(): ThemeName {
	const currentTheme = document.documentElement.dataset.theme ?? null;

	return isThemeName(currentTheme) ? currentTheme : DEFAULT_THEME;
}

export function toggleTheme(): ThemeName {
	const currentTheme = getActiveTheme();
	return applyTheme(currentTheme === 'terminus' ? 'wintry' : 'terminus');
}
