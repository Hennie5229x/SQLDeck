<script lang="ts">
	import { onMount } from 'svelte';
	import type { editor } from 'monaco-editor';

	import { ensureTsqlLanguage } from '$lib/monaco/tsql';

	const DEFAULT_QUERY = `SELECT name
FROM sys.databases
ORDER BY name;`;

	type QueryTab = {
		id: string;
		title: string;
		model: editor.ITextModel | null;
		viewState: editor.ICodeEditorViewState | null;
		isDirty: boolean;
	};

	let container: HTMLDivElement | null = null;
	let monacoEditor: editor.IStandaloneCodeEditor | null = null;
	let monaco: typeof import('monaco-editor') | null = null;
	let themeObserver: MutationObserver | null = null;
	let colorProbe: HTMLDivElement | null = null;
	let tabs = $state<QueryTab[]>([]);
	let activeTabId = $state('');
	let nextTabNumber = 1;
	let suppressDirtyTracking = false;
	let draggingTabId = $state('');
	let dragOverTabId = $state('');

	function createTabId() {
		return `query-tab-${crypto.randomUUID()}`;
	}

	function getDefaultTabTitle() {
		const title = `Query ${nextTabNumber}`;
		nextTabNumber += 1;
		return title;
	}

	function getActiveTab() {
		return tabs.find((tab) => tab.id === activeTabId) ?? null;
	}

	function createTab(initialContent = DEFAULT_QUERY) {
		if (!monaco) return;

		const tab: QueryTab = {
			id: createTabId(),
			title: getDefaultTabTitle(),
			model: monaco.editor.createModel(initialContent, 'tsql'),
			viewState: null,
			isDirty: false
		};

		tabs = [...tabs, tab];
		activateTab(tab.id);
	}

	function activateTab(tabId: string) {
		if (!monacoEditor) {
			activeTabId = tabId;
			return;
		}

		const currentTab = getActiveTab();
		if (currentTab) {
			currentTab.viewState = monacoEditor.saveViewState();
		}

		const nextTab = tabs.find((tab) => tab.id === tabId);
		if (!nextTab?.model) return;

		activeTabId = nextTab.id;
		suppressDirtyTracking = true;
		monacoEditor.setModel(nextTab.model);
		suppressDirtyTracking = false;

		if (nextTab.viewState) {
			monacoEditor.restoreViewState(nextTab.viewState);
		}

		monacoEditor.focus();
	}

	function closeTab(tabId: string) {
		if (tabs.length <= 1) {
			return;
		}

		const closingIndex = tabs.findIndex((tab) => tab.id === tabId);
		if (closingIndex < 0) return;

		const nextTabs = [...tabs];
		const [closingTab] = nextTabs.splice(closingIndex, 1);
		closingTab.model?.dispose();
		tabs = nextTabs;

		if (activeTabId !== tabId) {
			return;
		}

		const nextActiveTab = tabs[Math.max(0, closingIndex - 1)] ?? tabs[0];
		if (nextActiveTab) {
			activateTab(nextActiveTab.id);
		}
	}

	function markActiveTabDirty() {
		if (suppressDirtyTracking) return;

		const activeTab = getActiveTab();
		if (!activeTab || activeTab.isDirty) return;

		activeTab.isDirty = true;
		tabs = [...tabs];
	}

	function moveTab(tabId: string, targetTabId: string) {
		if (tabId === targetTabId) return;

		const sourceIndex = tabs.findIndex((tab) => tab.id === tabId);
		const targetIndex = tabs.findIndex((tab) => tab.id === targetTabId);

		if (sourceIndex < 0 || targetIndex < 0) return;

		const nextTabs = [...tabs];
		const [movedTab] = nextTabs.splice(sourceIndex, 1);
		nextTabs.splice(targetIndex, 0, movedTab);
		tabs = nextTabs;
	}

	function handleTabDragStart(tabId: string, event: DragEvent) {
		draggingTabId = tabId;
		dragOverTabId = tabId;

		if (event.dataTransfer) {
			event.dataTransfer.effectAllowed = 'move';
			event.dataTransfer.setData('text/plain', tabId);
		}
	}

	function handleTabDragEnter(tabId: string) {
		if (!draggingTabId || draggingTabId === tabId) return;
		dragOverTabId = tabId;
	}

	function handleTabDragOver(event: DragEvent) {
		if (!draggingTabId) return;
		event.preventDefault();

		if (event.dataTransfer) {
			event.dataTransfer.dropEffect = 'move';
		}
	}

	function handleTabDrop(tabId: string, event: DragEvent) {
		if (!draggingTabId) return;

		event.preventDefault();
		moveTab(draggingTabId, tabId);
		dragOverTabId = '';
		draggingTabId = '';
	}

	function clearTabDragState() {
		draggingTabId = '';
		dragOverTabId = '';
	}

	function resolveMonacoThemeName(theme: string | null) {
		return theme === 'wintry' ? 'sqldeck-light' : 'sqldeck-dark';
	}

	function componentToHex(component: number) {
		return component.toString(16).padStart(2, '0');
	}

	function normalizeMonacoColor(value: string, fallback: string) {
		if (typeof document === 'undefined') {
			return fallback;
		}

		colorProbe ??= document.createElement('div');
		colorProbe.style.color = fallback;
		colorProbe.style.color = value;

		const resolvedColor = colorProbe.style.color || fallback;
		const match = resolvedColor.match(
			/rgba?\(\s*(\d+(?:\.\d+)?)\s+(\d+(?:\.\d+)?)\s+(\d+(?:\.\d+)?)(?:\s*\/\s*(\d+(?:\.\d+)?%?))?\s*\)|rgba?\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)(?:\s*,\s*(\d+(?:\.\d+)?))?\s*\)/
		);

		if (!match) {
			return fallback;
		}

		const red = Number(match[1] ?? match[5]);
		const green = Number(match[2] ?? match[6]);
		const blue = Number(match[3] ?? match[7]);
		const alphaSource = match[4] ?? match[8];

		if (!alphaSource) {
			return `#${componentToHex(red)}${componentToHex(green)}${componentToHex(blue)}`;
		}

		const alpha = alphaSource.endsWith('%')
			? Math.round((Number.parseFloat(alphaSource) / 100) * 255)
			: Math.round(Number(alphaSource) * 255);

		return `#${componentToHex(red)}${componentToHex(green)}${componentToHex(blue)}${componentToHex(alpha)}`;
	}

	function applyMonacoTheme(monaco: typeof import('monaco-editor')) {
		const rootStyles = getComputedStyle(document.documentElement);
		const themeName = resolveMonacoThemeName(document.documentElement.dataset.theme ?? null);
		const muted = normalizeMonacoColor(rootStyles.getPropertyValue('--sqd-muted-fg').trim(), '#6b7280');
		const foreground = normalizeMonacoColor(rootStyles.getPropertyValue('--sqd-app-fg').trim(), '#e5e7eb');
		const background = normalizeMonacoColor(rootStyles.getPropertyValue('--sqd-panel-bg').trim(), '#111827');
		const lineHighlight = normalizeMonacoColor(rootStyles.getPropertyValue('--sqd-button-bg').trim(), '#1f2937');
		const selection = normalizeMonacoColor(
			rootStyles.getPropertyValue('--sqd-button-bg-hover').trim(),
			'#374151'
		);

		monaco.editor.defineTheme(themeName, {
			base: themeName === 'sqldeck-light' ? 'vs' : 'vs-dark',
			inherit: true,
			rules: [],
			colors: {
				'editor.background': background,
				'editor.foreground': foreground,
				'editorCursor.foreground': foreground,
				'editorLineNumber.foreground': muted,
				'editorLineNumber.activeForeground': foreground,
				'editor.lineHighlightBackground': lineHighlight,
				'editor.selectionBackground': selection,
				'editor.inactiveSelectionBackground': selection
			}
		});

		monaco.editor.setTheme(themeName);
	}

	onMount(() => {
		let cancelled = false;
		let modelChangeListener: { dispose: () => void } | null = null;

		async function mountEditor() {
			if (!container) return;

			const [{ default: EditorWorker }, loadedMonaco] = await Promise.all([
				import('monaco-editor/esm/vs/editor/editor.worker?worker'),
				import('monaco-editor')
			]);

			if (cancelled || !container) return;

			monaco = loadedMonaco;

			(
				self as typeof self & {
					MonacoEnvironment?: {
						getWorker: (_workerId: string, _label: string) => Worker;
					};
				}
			).MonacoEnvironment = {
				getWorker() {
					return new EditorWorker();
				}
			};

			ensureTsqlLanguage(loadedMonaco);
			applyMonacoTheme(loadedMonaco);

			monacoEditor = loadedMonaco.editor.create(container, {
				model: null,
				ariaLabel: 'SQL query editor',
				automaticLayout: true,
				minimap: { enabled: false },
				scrollBeyondLastLine: false,
				fontSize: 14,
				lineHeight: 20,
				tabSize: 4,
				insertSpaces: true,
				wordWrap: 'off',
				renderLineHighlight: 'all',
				padding: { top: 12, bottom: 12 },
				fontFamily: "Consolas, 'Cascadia Mono', 'Courier New', monospace"
			});

			modelChangeListener = monacoEditor.onDidChangeModelContent(() => {
				markActiveTabDirty();
			});

			createTab(DEFAULT_QUERY);

			themeObserver = new MutationObserver(() => applyMonacoTheme(loadedMonaco));
			themeObserver.observe(document.documentElement, {
				attributes: true,
				attributeFilter: ['data-theme']
			});
		}

		void mountEditor();

		return () => {
			cancelled = true;
			themeObserver?.disconnect();
			modelChangeListener?.dispose();
			for (const tab of tabs) {
				tab.model?.dispose();
			}
			monacoEditor?.dispose();
		};
	});

	export function getSql() {
		return getActiveTab()?.model?.getValue() ?? DEFAULT_QUERY;
	}

	export function getSqlToExecute() {
		const activeTab = getActiveTab();
		if (!monacoEditor || !activeTab?.model) {
			return DEFAULT_QUERY;
		}

		const selection = monacoEditor.getSelection();
		if (!selection || selection.isEmpty()) {
			return activeTab.model.getValue();
		}

		const selectedSql = activeTab.model.getValueInRange(selection).trim();
		return selectedSql || activeTab.model.getValue();
	}
</script>

<section class="editor-area">
	<div class="tab-strip" aria-label="Open query tabs">
		<div class="tab-list">
			{#each tabs as tab}
				<div
					class:active-tab={tab.id === activeTabId}
					class:dragging-tab={tab.id === draggingTabId}
					class:drag-over-tab={tab.id === dragOverTabId && tab.id !== draggingTabId}
					class="tab-shell"
					draggable="true"
					role="tab"
					aria-selected={tab.id === activeTabId}
					tabindex={tab.id === activeTabId ? 0 : -1}
					ondragstart={(event) => handleTabDragStart(tab.id, event)}
					ondragenter={() => handleTabDragEnter(tab.id)}
					ondragover={handleTabDragOver}
					ondrop={(event) => handleTabDrop(tab.id, event)}
					ondragend={clearTabDragState}
				>
					<button class="tab-button" type="button" onclick={() => activateTab(tab.id)}>
						<span class="tab-title">
							{tab.title}{tab.isDirty ? ' *' : ''}
						</span>
					</button>
					<button
						class="tab-close"
						type="button"
						disabled={tabs.length <= 1}
						aria-label={`Close ${tab.title}`}
						onclick={() => closeTab(tab.id)}
					>
						×
					</button>
				</div>
			{/each}
		</div>

		<button class="new-tab-button" type="button" aria-label="New query tab" onclick={() => createTab('')}>
			+
		</button>
	</div>

	<div bind:this={container} class="editor-surface" aria-label="SQL query editor"></div>
</section>

<style>
	.editor-area {
		height: 100%;
		min-height: 0;
		display: flex;
		flex-direction: column;
		box-sizing: border-box;
		border-bottom: 1px solid var(--sqd-border);
		background: var(--sqd-panel-bg);
		padding: 0;
		overflow: hidden;
	}

	.tab-strip {
		display: flex;
		align-items: stretch;
		min-height: 34px;
		border-bottom: 1px solid var(--sqd-border);
		background: var(--sqd-panel-strong-bg);
	}

	.tab-list {
		display: flex;
		align-items: stretch;
		min-width: 0;
		flex: 1;
		overflow-x: auto;
		overflow-y: hidden;
	}

	.tab-shell {
		display: inline-flex;
		align-items: center;
		min-width: 0;
		max-width: 220px;
		border-right: 1px solid var(--sqd-border);
		border-bottom: 2px solid transparent;
		background: transparent;
		color: var(--sqd-muted-fg);
		flex: 0 0 auto;
	}

	.tab-button {
		min-width: 0;
		flex: 1;
		padding: 0 10px;
		border: 0;
		background: transparent;
		color: inherit;
		cursor: pointer;
		font-size: 0.78rem;
		text-align: left;
	}

	.tab-button:hover,
	.tab-close:hover {
		background: var(--sqd-button-bg);
		color: var(--sqd-app-fg);
	}

	.active-tab {
		background: var(--sqd-panel-bg);
		color: var(--sqd-app-fg);
		border-bottom-color: var(--sqd-app-fg);
	}

	.dragging-tab {
		opacity: 0.6;
	}

	.drag-over-tab {
		box-shadow: inset 2px 0 0 var(--sqd-app-fg);
	}

	.tab-title {
		display: block;
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.tab-close,
	.new-tab-button {
		border: 0;
		background: transparent;
		color: inherit;
		cursor: pointer;
		font: inherit;
	}

	.tab-close {
		padding: 0 8px 0 0;
		line-height: 1;
		opacity: 0.75;
	}

	.tab-close:disabled {
		opacity: 0.35;
		cursor: default;
	}

	.new-tab-button {
		width: 34px;
		border-left: 1px solid var(--sqd-border);
		background: var(--sqd-panel-strong-bg);
		flex: 0 0 auto;
	}

	.new-tab-button:hover {
		color: var(--sqd-app-fg);
	}

	.editor-surface {
		min-height: 0;
		flex: 1;
	}
</style>
