<script lang="ts">
	import BottomPanel from '$lib/components/layout/BottomPanel.svelte';
	import ConnectModal from '$lib/components/layout/ConnectModal.svelte';
	import EditorArea from '$lib/components/layout/EditorArea.svelte';
	import HorizontalSplitter from '$lib/components/layout/HorizontalSplitter.svelte';
	import SideBar from '$lib/components/layout/SideBar.svelte';
	import TopBar from '$lib/components/layout/TopBar.svelte';
	import VerticalSplitter from '$lib/components/layout/VerticalSplitter.svelte';
	import type { ConnectionFormData, DatabaseNode, SavedConnection, TableNode } from '$lib/types/connections';
	import type { QueryExecutionResponse, QueryExecutionResultState } from '$lib/types/query';
	import { onDestroy, onMount } from 'svelte';

	const API_BASE = '';

	const DEFAULT_SIDEBAR_WIDTH = 260;
	const COLLAPSED_SIDEBAR_WIDTH = 0;
	const MIN_SIDEBAR_WIDTH = 24;
	const SIDEBAR_COLLAPSE_THRESHOLD = 6;
	const DEFAULT_BOTTOM_HEIGHT = 180;
	const COLLAPSED_BOTTOM_HEIGHT = 0;
	const MIN_BOTTOM_HEIGHT = 24;
	const BOTTOM_COLLAPSE_THRESHOLD = 6;
	const MIN_EDITOR_WIDTH = 320;
	const MIN_EDITOR_HEIGHT = 180;
	const TOP_BAR_HEIGHT = 44;
	const SPLITTER_SIZE = 6;

	type ResizeMode = 'sidebar' | 'bottom' | null;
	type EditorAreaHandle = {
		getSql: () => string;
		getSqlToExecute: () => string;
	};

	const SYSTEM_DATABASE_ORDER = ['master', 'model', 'msdb', 'tempdb'] as const;

	let shellElement: HTMLDivElement | null = null;
	let editorArea: EditorAreaHandle | null = null;
	let resizeMode = $state<ResizeMode>(null);
	let sidebarWidth = $state(DEFAULT_SIDEBAR_WIDTH);
	let bottomHeight = $state(DEFAULT_BOTTOM_HEIGHT);
	let showConnectModal = $state(false);
	let connections = $state<SavedConnection[]>([]);
	let sidebarErrorMessage = $state('');
	let connectErrorMessage = $state('');
	let loadingConnections = $state(false);
	let loadingDatabaseConnectionId = $state('');
	let connectionErrorsById = $state<Record<string, string>>({});
	let loadingTableNodeId = $state('');
	let tablesByDatabaseId = $state<Record<string, TableNode[]>>({});
	let databasesByConnectionId = $state<Record<string, DatabaseNode[]>>({});
	let savingConnection = $state(false);
	let activeConnectionId = $state('');
	let activeDatabaseName = $state('');
	let queryResult = $state<QueryExecutionResultState>({
		data: null,
		error: '',
		executing: false
	});

	let pendingSidebarWidth = DEFAULT_SIDEBAR_WIDTH;
	let pendingBottomHeight = DEFAULT_BOTTOM_HEIGHT;
	let frameId: number | null = null;

	function clamp(value: number, min: number, max: number) {
		return Math.min(Math.max(value, min), max);
	}

	function resolveSidebarWidth(rawWidth: number, maxWidth: number) {
		if (rawWidth <= MIN_SIDEBAR_WIDTH + SIDEBAR_COLLAPSE_THRESHOLD) {
			return COLLAPSED_SIDEBAR_WIDTH;
		}

		return clamp(rawWidth, MIN_SIDEBAR_WIDTH, maxWidth);
	}

	function resolveBottomHeight(rawHeight: number, maxHeight: number) {
		if (rawHeight <= MIN_BOTTOM_HEIGHT + BOTTOM_COLLAPSE_THRESHOLD) return COLLAPSED_BOTTOM_HEIGHT;
		return clamp(rawHeight, MIN_BOTTOM_HEIGHT, maxHeight);
	}

	function isCompactLayout() {
		return window.matchMedia('(max-width: 900px)').matches;
	}

	async function request<T>(path: string, init?: RequestInit): Promise<T> {
		const response = await fetch(`${API_BASE}${path}`, {
			headers: {
				'Content-Type': 'application/json',
				...(init?.headers ?? {})
			},
			...init
		});

		const text = await response.text();
		const contentType = response.headers.get('content-type') ?? '';
		const data = text && contentType.includes('application/json') ? JSON.parse(text) : null;

		if (!response.ok) {
			const message =
				data?.error ??
				data?.message ??
				(text ||
					`Request failed with status ${response.status}`);
			throw new Error(message);
		}

		return (data ?? text) as T;
	}

	async function loadConnections() {
		loadingConnections = true;
		sidebarErrorMessage = '';
		connectionErrorsById = {};
		databasesByConnectionId = {};

		try {
			connections = await request<SavedConnection[]>('/api/connections', {
				method: 'GET'
			});
		} catch (error) {
			sidebarErrorMessage =
				error instanceof Error ? error.message : 'Failed to load saved connections.';
		} finally {
			loadingConnections = false;
		}
	}

	function mapDatabases(names: string[]): DatabaseNode[] {
		const systemSet = new Set(SYSTEM_DATABASE_ORDER);

		const systemDatabases = SYSTEM_DATABASE_ORDER
			.filter((name) => names.includes(name))
			.map((name) => ({ name, isSystem: true }));

		const userDatabases = names
			.filter((name) => !systemSet.has(name as (typeof SYSTEM_DATABASE_ORDER)[number]))
			.sort((left, right) => left.localeCompare(right))
			.map((name) => ({ name, isSystem: false }));

		return [...systemDatabases, ...userDatabases];
	}

	async function loadDatabases(connection: SavedConnection) {
		if (databasesByConnectionId[connection.id]) {
			return;
		}

		loadingDatabaseConnectionId = connection.id;
		sidebarErrorMessage = '';
		connectionErrorsById = {
			...connectionErrorsById,
			[connection.id]: ''
		};

		try {
			const response = await request<{ name: string }[]>(`/api/connections/${connection.id}/databases`, {
				method: 'GET'
			});

			databasesByConnectionId = {
				...databasesByConnectionId,
				[connection.id]: mapDatabases(response.map((item) => item.name))
			};
			connectionErrorsById = {
				...connectionErrorsById,
				[connection.id]: ''
			};
		} catch (error) {
			const message =
				error instanceof Error ? error.message : 'Failed to load databases.';
			connectionErrorsById = {
				...connectionErrorsById,
				[connection.id]: message
			};
		} finally {
			loadingDatabaseConnectionId = '';
		}
	}

	async function loadTables(connection: SavedConnection, databaseName: string) {
		const databaseNodeId = `${connection.id}:${databaseName}`;

		if (tablesByDatabaseId[databaseNodeId]) {
			return;
		}

		loadingTableNodeId = databaseNodeId;
		connectionErrorsById = {
			...connectionErrorsById,
			[connection.id]: ''
		};

		try {
			const response = await request<TableNode[]>(
				`/api/connections/${connection.id}/databases/${encodeURIComponent(databaseName)}/tables`,
				{ method: 'GET' }
			);

			tablesByDatabaseId = {
				...tablesByDatabaseId,
				[databaseNodeId]: response
			};
		} catch (error) {
			connectionErrorsById = {
				...connectionErrorsById,
				[connection.id]: error instanceof Error ? error.message : 'Failed to load tables.'
			};
		} finally {
			loadingTableNodeId = '';
		}
	}

	function openConnectModal() {
		connectErrorMessage = '';
		showConnectModal = true;
	}

	function closeConnectModal() {
		if (savingConnection) return;

		connectErrorMessage = '';
		showConnectModal = false;
	}

	function disconnectConnections() {
		sidebarErrorMessage = '';
		databasesByConnectionId = {};
		tablesByDatabaseId = {};
		connectionErrorsById = {};
		connections = [];
		activeConnectionId = '';
		activeDatabaseName = '';
		queryResult = {
			data: null,
			error: '',
			executing: false
		};
	}

	function handleSelectDatabase(connection: SavedConnection, databaseName: string) {
		activeConnectionId = connection.id;
		activeDatabaseName = databaseName;
	}

	function getActiveConnection() {
		return connections.find((connection) => connection.id === activeConnectionId) ?? null;
	}

	function getExecutionTargetLabel() {
		const connection = getActiveConnection();
		if (!connection || !activeDatabaseName) return '';
		return `${connection.displayName} / ${activeDatabaseName}`;
	}

	async function executeQuery() {
		const connection = getActiveConnection();
		const sql = editorArea?.getSqlToExecute().trim() ?? '';

		if (!connection || !activeDatabaseName || !sql) {
			return;
		}

		queryResult = {
			...queryResult,
			error: '',
			executing: true
		};

		try {
			const response = await request<QueryExecutionResponse>('/api/query/execute', {
				method: 'POST',
				body: JSON.stringify({
					connectionId: connection.id,
					database: activeDatabaseName,
					sql
				})
			});

			queryResult = {
				data: response,
				error: '',
				executing: false
			};
		} catch (error) {
			queryResult = {
				...queryResult,
				error: error instanceof Error ? error.message : 'Query execution failed.',
				executing: false
			};
		}
	}

	onMount(() => {
		loadConnections();
	});

	function commitPendingResize() {
		if (resizeMode === 'sidebar') {
			sidebarWidth = pendingSidebarWidth;
		}

		if (resizeMode === 'bottom') {
			bottomHeight = pendingBottomHeight;
		}
	}

	function scheduleResizeCommit() {
		if (frameId !== null) return;

		frameId = requestAnimationFrame(() => {
			frameId = null;
			commitPendingResize();
		});
	}

	function handlePointerMove(event: PointerEvent) {
		if (!shellElement || !resizeMode) return;

		const rect = shellElement.getBoundingClientRect();

		if (resizeMode === 'sidebar') {
			const maxSidebarWidth = Math.max(COLLAPSED_SIDEBAR_WIDTH, rect.width - MIN_EDITOR_WIDTH - SPLITTER_SIZE);
			const rawSidebarWidth = event.clientX - rect.left;
			pendingSidebarWidth = resolveSidebarWidth(rawSidebarWidth, maxSidebarWidth);
		}

		if (resizeMode === 'bottom') {
			const maxBottomHeight = Math.max(
				COLLAPSED_BOTTOM_HEIGHT,
				rect.height - TOP_BAR_HEIGHT - MIN_EDITOR_HEIGHT - SPLITTER_SIZE
			);

			const rawBottomHeight = rect.bottom - event.clientY;
			pendingBottomHeight = resolveBottomHeight(rawBottomHeight, maxBottomHeight);
		}

		scheduleResizeCommit();
	}

	function stopResize() {
		if (typeof document === 'undefined' || typeof window === 'undefined') {
			resizeMode = null;
			return;
		}

		commitPendingResize();
		resizeMode = null;
		document.body.classList.remove('sqd-resizing');
		document.body.style.cursor = '';
		window.removeEventListener('pointermove', handlePointerMove);
		window.removeEventListener('pointerup', stopResize);
		window.removeEventListener('pointercancel', stopResize);
	}

	function startResize(mode: Exclude<ResizeMode, null>, event: PointerEvent) {
		if (isCompactLayout()) return;

		resizeMode = mode;
		document.body.classList.add('sqd-resizing');
		document.body.style.cursor = mode === 'sidebar' ? 'col-resize' : 'row-resize';
		event.preventDefault();
		window.addEventListener('pointermove', handlePointerMove);
		window.addEventListener('pointerup', stopResize);
		window.addEventListener('pointercancel', stopResize);
	}

	async function handleConnectionSubmit(payload: ConnectionFormData) {
		savingConnection = true;
		connectErrorMessage = '';

		try {
			const savedConnection = await request<SavedConnection>('/api/connections', {
				method: 'POST',
				body: JSON.stringify(payload)
			});

			connections = [
				...connections.filter((connection) => connection.id !== savedConnection.id),
				savedConnection
			];
			sidebarErrorMessage = '';
			showConnectModal = false;
		} catch (error) {
			connectErrorMessage =
				error instanceof Error ? error.message : 'Failed to save connection.';
		} finally {
			savingConnection = false;
		}
	}

	onDestroy(() => {
		if (frameId !== null) cancelAnimationFrame(frameId);
		stopResize();
	});
</script>

<svelte:head>
	<title>SQLDeck</title>
</svelte:head>

<div
	bind:this={shellElement}
	class="shell"
	style={`--sidebar-width: ${sidebarWidth}px; --bottom-height: ${bottomHeight}px;`}
>
	<div class="shell-top">
		<TopBar
			canExecute={!!getActiveConnection() && !!activeDatabaseName}
			executing={queryResult.executing}
			executionTargetLabel={getExecutionTargetLabel()}
			onExecute={executeQuery}
		/>
	</div>

	<div class="shell-sidebar">
		<SideBar
			activeConnectionId={activeConnectionId}
			activeDatabaseName={activeDatabaseName}
			{connections}
			{databasesByConnectionId}
			{tablesByDatabaseId}
			{connectionErrorsById}
			errorMessage={sidebarErrorMessage}
			loadingDatabaseConnectionId={loadingDatabaseConnectionId}
			loadingTableNodeId={loadingTableNodeId}
			onConnect={openConnectModal}
			onDisconnect={disconnectConnections}
			onRefresh={loadConnections}
			onLoadDatabases={loadDatabases}
			onLoadTables={loadTables}
			onSelectDatabase={handleSelectDatabase}
		/>
	</div>

	<div class="shell-vertical-splitter">
		<VerticalSplitter active={resizeMode === 'sidebar'} onpointerdown={(event) => startResize('sidebar', event)} />
	</div>

	<div class="shell-editor">
		<EditorArea bind:this={editorArea} />
	</div>

	<div class="shell-horizontal-splitter">
		<HorizontalSplitter active={resizeMode === 'bottom'} onpointerdown={(event) => startResize('bottom', event)} />
	</div>

	<div class="shell-bottom">
		<BottomPanel
			result={queryResult}
			executionTargetLabel={getExecutionTargetLabel()}
		/>
	</div>
</div>

{#if showConnectModal}
	<ConnectModal
		errorMessage={connectErrorMessage}
		saving={savingConnection}
		onCancel={closeConnectModal}
		onSubmit={handleConnectionSubmit}
	/>
{/if}

<style>
	:global(body) {
		background: var(--sqd-app-bg);
		color: var(--sqd-app-fg);
	}

	:global(#svelte) {
		min-height: 100vh;
	}

	.shell {
		display: grid;
		grid-template-columns: var(--sidebar-width) 6px minmax(0, 1fr);
		grid-template-rows: 44px minmax(0, 1fr) 6px var(--bottom-height);
		grid-template-areas:
			'top top top'
			'side vsplit editor'
			'side vsplit hsplit'
			'side vsplit bottom';
		height: 100vh;
		overflow: hidden;
		background: var(--sqd-shell-bg);
	}

	.shell-top {
		grid-area: top;
		min-width: 0;
	}

	.shell-sidebar {
		grid-area: side;
		min-width: 0;
		overflow: hidden;
		min-height: 0;
	}

	.shell-vertical-splitter {
		grid-area: vsplit;
		min-width: 0;
		min-height: 0;
	}

	.shell-editor {
		grid-area: editor;
		min-width: 0;
		min-height: 0;
	}

	.shell-horizontal-splitter {
		grid-area: hsplit;
		min-width: 0;
		min-height: 0;
	}

	.shell-bottom {
		grid-area: bottom;
		min-width: 0;
		min-height: 0;
	}

	:global(body.sqd-resizing) {
		user-select: none;
	}

	@media (max-width: 900px) {
		.shell {
			grid-template-columns: minmax(0, 1fr);
			grid-template-rows: 44px 180px minmax(0, 1fr) 160px;
			grid-template-areas:
				'top'
				'side'
				'editor'
				'bottom';
		}

		.shell-vertical-splitter,
		.shell-horizontal-splitter {
			display: none;
		}

		.shell-sidebar,
		.shell-editor,
		.shell-bottom {
			min-height: 0;
		}
	}
</style>
