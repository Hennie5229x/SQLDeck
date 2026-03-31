<script lang="ts">
	import AppIcon from '$lib/components/ui/AppIcon.svelte';
	import type { DatabaseNode, SavedConnection, TableNode } from '$lib/types/connections';

	type Props = {
		connections?: SavedConnection[];
		databasesByConnectionId?: Record<string, DatabaseNode[]>;
		tablesByDatabaseId?: Record<string, TableNode[]>;
		activeConnectionId?: string;
		activeDatabaseName?: string;
		connectionErrorsById?: Record<string, string>;
		errorMessage?: string;
		loadingDatabaseConnectionId?: string;
		loadingTableNodeId?: string;
		onConnect?: () => void;
		onDisconnect?: () => void;
		onRefresh?: () => void;
		onLoadDatabases?: (connection: SavedConnection) => void | Promise<void>;
		onLoadTables?: (connection: SavedConnection, databaseName: string) => void | Promise<void>;
		onSelectDatabase?: (connection: SavedConnection, databaseName: string) => void;
	};

	const SYSTEM_DATABASE_ORDER = ['master', 'model', 'msdb', 'tempdb'] as const;

	let {
		connections = [],
		databasesByConnectionId = {},
		tablesByDatabaseId = {},
		activeConnectionId = '',
		activeDatabaseName = '',
		connectionErrorsById = {},
		errorMessage = '',
		loadingDatabaseConnectionId = '',
		loadingTableNodeId = '',
		onConnect,
		onDisconnect,
		onRefresh,
		onLoadDatabases,
		onLoadTables,
		onSelectDatabase
	}: Props = $props();

	let expandedServerIds = $state<string[]>([]);
	let expandedDatabaseFolderIds = $state<string[]>([]);
	let expandedSystemFolderIds = $state<string[]>([]);
	let expandedDatabaseIds = $state<string[]>([]);
	let expandedTableFolderIds = $state<string[]>([]);
</script>

<section class="side-bar">
	<header class="sidebar-header">
		<h2>Object Explorer</h2>
	</header>

	<div class="sidebar-toolbar" aria-label="Object Explorer actions">
		<button class="toolbar-button" type="button" title="Connect" aria-label="Connect" onclick={onConnect}>
			<AppIcon icon="mdi:database-plus-outline" width="0.9rem" height="0.9rem" />
		</button>

		<button class="toolbar-button" type="button" title="Disconnect" aria-label="Disconnect" onclick={onDisconnect}>
			<AppIcon icon="mdi:database-remove-outline" width="0.9rem" height="0.9rem" />
		</button>

		<button class="toolbar-button" type="button" title="Refresh" aria-label="Refresh" onclick={onRefresh}>
			<AppIcon icon="mdi:refresh" width="0.9rem" height="0.9rem" />
		</button>
	</div>

	<div class="sidebar-tree-placeholder">
		{#if errorMessage && !connections.length}
			<p class="tree-message error">{errorMessage}</p>
		{:else if connections.length}
			<div class="tree-list">
				{#each connections as connection}
					{@const serverExpanded = expandedServerIds.includes(connection.id)}
					{@const databaseFolderExpanded = expandedDatabaseFolderIds.includes(connection.id)}
					{@const systemFolderExpanded = expandedSystemFolderIds.includes(connection.id)}
					{@const databases = databasesByConnectionId[connection.id] ?? []}
					{@const connectionError = connectionErrorsById[connection.id] ?? ''}
					{@const systemDatabases = SYSTEM_DATABASE_ORDER
						.filter((name) => databases.some((database) => database.name === name))
						.map((name) => databases.find((database) => database.name === name))
						.filter(Boolean)}
					{@const userDatabases = databases.filter((database) => !database.isSystem)}

					<div class="server-tree">
						<button
							class="tree-button server-node"
							type="button"
							onclick={async () => {
								if (serverExpanded) {
									expandedServerIds = expandedServerIds.filter((id) => id !== connection.id);
									return;
								}

								expandedServerIds = [...expandedServerIds, connection.id];
							}}
						>
							<div class="tree-node-main">
								<AppIcon
									icon={serverExpanded ? 'mdi:chevron-down' : 'mdi:chevron-right'}
									width="0.9rem"
									height="0.9rem"
									class="tree-chevron-icon"
								/>
								<AppIcon icon="mdi:server-outline" width="0.9rem" height="0.9rem" class="tree-node-icon" />
								<span class="tree-node-label">{connection.displayName}</span>
							</div>
							<span class:status-error={!!connectionError} class="tree-node-status">
								{connectionError ? 'Connection Failed' : 'Connected'}
							</span>
						</button>

						{#if serverExpanded}
							<div class="server-tree-body">
								<div class="tree-children">
									<button
										class="tree-button tree-row"
										type="button"
										onclick={async () => {
											if (databaseFolderExpanded) {
												expandedDatabaseFolderIds = expandedDatabaseFolderIds.filter((id) => id !== connection.id);
												return;
											}

											expandedDatabaseFolderIds = [...expandedDatabaseFolderIds, connection.id];
											await onLoadDatabases?.(connection);
										}}
									>
										<div class="tree-node-main">
											<AppIcon
												icon={databaseFolderExpanded ? 'mdi:chevron-down' : 'mdi:chevron-right'}
												width="0.9rem"
												height="0.9rem"
												class="tree-chevron-icon"
											/>
											<AppIcon icon="mdi:folder-outline" width="0.9rem" height="0.9rem" class="tree-node-icon" />
											<span class="tree-node-label">Databases</span>
										</div>
									</button>

									{#if databaseFolderExpanded}
										<div class="tree-children">
											{#if connectionError}
												<p class="tree-message error">{connectionError}</p>
											{:else if loadingDatabaseConnectionId === connection.id}
												<p class="tree-message">Loading databases...</p>
											{:else}
												<button
													class="tree-button tree-row"
													type="button"
													onclick={() => {
														expandedSystemFolderIds = systemFolderExpanded
															? expandedSystemFolderIds.filter((id) => id !== connection.id)
															: [...expandedSystemFolderIds, connection.id];
													}}
												>
													<div class="tree-node-main">
														<AppIcon
															icon={systemFolderExpanded ? 'mdi:chevron-down' : 'mdi:chevron-right'}
															width="0.9rem"
															height="0.9rem"
															class="tree-chevron-icon"
														/>
														<AppIcon icon="mdi:folder-outline" width="0.9rem" height="0.9rem" class="tree-node-icon" />
														<span class="tree-node-label">System Databases</span>
													</div>
												</button>

												{#if systemFolderExpanded}
													<div class="tree-children">
														{#each systemDatabases as database}
															{#if database}
																{@render databaseNode(database, connection.id)}
															{/if}
														{/each}
													</div>
												{/if}

												{#each userDatabases as database}
													{@render databaseNode(database, connection.id)}
												{/each}
											{/if}
										</div>
									{/if}
								</div>
							</div>
						{/if}
					</div>
				{/each}
			</div>
			{#if errorMessage}
				<p class="tree-message error">{errorMessage}</p>
			{/if}
		{:else}
			<p class="tree-message">No connected servers.</p>
		{/if}
	</div>
</section>

{#snippet databaseNode(database: DatabaseNode, connectionId: string)}
	{@const databaseNodeId = `${connectionId}:${database.name}`}
	{@const expanded = expandedDatabaseIds.includes(databaseNodeId)}
	{@const tablesExpanded = expandedTableFolderIds.includes(databaseNodeId)}
	{@const tables = tablesByDatabaseId[databaseNodeId] ?? []}
	{@const isActive = activeConnectionId === connectionId && activeDatabaseName === database.name}

	<div class="tree-block">
		<button
			class:active-database={isActive}
			class="tree-button tree-row"
			type="button"
			onclick={() => {
				const connection = connections.find((item) => item.id === connectionId);
				if (connection) {
					onSelectDatabase?.(connection, database.name);
				}

				expandedDatabaseIds = expanded
					? expandedDatabaseIds.filter((id) => id !== databaseNodeId)
					: [...expandedDatabaseIds, databaseNodeId];
			}}
		>
			<div class="tree-node-main">
				<AppIcon
					icon={expanded ? 'mdi:chevron-down' : 'mdi:chevron-right'}
					width="0.9rem"
					height="0.9rem"
					class="tree-chevron-icon"
				/>
				<AppIcon icon="mdi:database-outline" width="0.9rem" height="0.9rem" class="tree-node-icon" />
				<span class="tree-node-label">{database.name}</span>
			</div>
		</button>

		{#if expanded}
			<div class="tree-children">
				<button
					class="tree-button tree-row"
					type="button"
					onclick={async () => {
						if (tablesExpanded) {
							expandedTableFolderIds = expandedTableFolderIds.filter((id) => id !== databaseNodeId);
							return;
						}

						expandedTableFolderIds = [...expandedTableFolderIds, databaseNodeId];
						const connection = connections.find((item) => item.id === connectionId);
						if (connection) {
							await onLoadTables?.(connection, database.name);
						}
					}}
				>
					<div class="tree-node-main">
						<AppIcon
							icon={tablesExpanded ? 'mdi:chevron-down' : 'mdi:chevron-right'}
							width="0.9rem"
							height="0.9rem"
							class="tree-chevron-icon"
						/>
						<AppIcon icon="mdi:folder-outline" width="0.9rem" height="0.9rem" class="tree-node-icon" />
						<span class="tree-node-label">Tables</span>
					</div>
				</button>

				{#if tablesExpanded}
					<div class="tree-children">
						{#if loadingTableNodeId === databaseNodeId}
							<p class="tree-message">Loading tables...</p>
						{:else}
							{#each tables as table}
								<div class="tree-row static-row">
									<div class="tree-node-main">
										<AppIcon icon="mdi:table-large" width="0.9rem" height="0.9rem" class="tree-node-icon" />
										<span class="tree-node-label">{table.schema}.{table.name}</span>
									</div>
								</div>
							{/each}
						{/if}
					</div>
				{/if}
			</div>
		{/if}
	</div>
{/snippet}

<style>
	.side-bar {
		height: 100%;
		display: grid;
		grid-template-rows: auto auto minmax(0, 1fr);
		box-sizing: border-box;
		border-right: 1px solid var(--sqd-border);
		border-bottom: 1px solid var(--sqd-border);
		background: var(--sqd-panel-strong-bg);
	}

	.sidebar-header,
	.sidebar-toolbar {
		display: flex;
		align-items: center;
		min-width: 0;
		padding: 8px 10px;
		border-bottom: 1px solid var(--sqd-border);
	}

	.sidebar-header h2 {
		margin: 0;
		color: var(--sqd-muted-fg);
		font-size: 0.76rem;
		font-weight: 600;
		letter-spacing: 0.04em;
		text-transform: uppercase;
	}

	.sidebar-toolbar {
		gap: 6px;
		padding-top: 6px;
		padding-bottom: 6px;
	}

	.toolbar-button {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 28px;
		height: 28px;
		padding: 0;
		border: 1px solid transparent;
		border-radius: 4px;
		background: transparent;
		color: var(--sqd-muted-fg);
		cursor: pointer;
	}

	.toolbar-button:hover {
		border-color: var(--sqd-border);
		background: var(--sqd-button-bg-hover);
		color: var(--sqd-app-fg);
	}

	.toolbar-button:focus-visible {
		outline: 1px solid var(--sqd-app-fg);
		outline-offset: -1px;
	}

	.sidebar-tree-placeholder {
		min-height: 0;
		padding: 10px;
		background: var(--sqd-panel-bg);
		overflow: auto;
	}

	.tree-list {
		display: grid;
		gap: 4px;
	}

	.tree-block {
		display: grid;
		gap: 2px;
	}

	.server-tree {
		border: 1px solid var(--sqd-border);
		background: var(--sqd-panel-strong-bg);
	}

	.server-tree-body {
		padding: 0 6px 6px;
	}

	.tree-button {
		width: 100%;
		padding: 4px 0;
		border: 0;
		background: transparent;
		text-align: left;
		cursor: pointer;
	}

	.active-database {
		background: var(--sqd-button-bg-hover);
	}

	.server-node {
		padding: 4px 6px;
	}

	.tree-row,
	.static-row {
		padding: 2px 0;
	}

	.tree-button:hover,
	.tree-button:focus-visible {
		background: var(--sqd-button-bg);
		outline: none;
	}

	.tree-node-main {
		display: flex;
		align-items: center;
		gap: 6px;
		min-width: 0;
	}

	.tree-children {
		display: grid;
		gap: 4px;
		padding-left: 12px;
	}

	:global(.tree-chevron-icon),
	:global(.tree-node-icon) {
		flex: 0 0 auto;
		color: var(--sqd-muted-fg);
	}

	:global(.tree-chevron-icon) {
		color: var(--sqd-app-fg);
	}

	.tree-node-label,
	.tree-node-status,
	.tree-message {
		margin: 0;
		opacity: 0.72;
		font-size: 0.78rem;
	}

	.tree-node-status {
		color: var(--color-success-500);
	}

	.tree-node-status.status-error {
		color: var(--color-error-500);
	}

	.tree-message.error {
		color: var(--color-error-500);
	}
</style>
