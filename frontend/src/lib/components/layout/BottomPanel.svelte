<script lang="ts">
	import type { QueryExecutionResultState } from '$lib/types/query';
	import { onDestroy } from 'svelte';

	type Props = {
		result?: QueryExecutionResultState;
		executionTargetLabel?: string;
	};

	const DEFAULT_TABLE_HEIGHT = 220;
	const MIN_TABLE_HEIGHT = 120;
	const MAX_TABLE_HEIGHT = 640;

	let {
		result = { data: null, error: '', executing: false },
		executionTargetLabel = ''
	}: Props = $props();

	let tableHeights = $state<Record<number, number>>({});
	let resizingIndex = $state<number | null>(null);
	let resizeStartY = 0;
	let resizeStartHeight = DEFAULT_TABLE_HEIGHT;

	function formatCellValue(value: unknown) {
		if (value === null || value === undefined) return 'NULL';
		if (typeof value === 'object') return JSON.stringify(value);
		return String(value);
	}

	function clamp(value: number, min: number, max: number) {
		return Math.min(Math.max(value, min), max);
	}

	function getTableHeight(index: number) {
		return tableHeights[index] ?? DEFAULT_TABLE_HEIGHT;
	}

	function startResize(index: number, event: PointerEvent) {
		resizingIndex = index;
		resizeStartY = event.clientY;
		resizeStartHeight = getTableHeight(index);
		event.preventDefault();

		window.addEventListener('pointermove', handlePointerMove);
		window.addEventListener('pointerup', stopResize);
		window.addEventListener('pointercancel', stopResize);
	}

	function handlePointerMove(event: PointerEvent) {
		if (resizingIndex === null) return;

		const nextHeight = clamp(
			resizeStartHeight + (event.clientY - resizeStartY),
			MIN_TABLE_HEIGHT,
			MAX_TABLE_HEIGHT
		);

		tableHeights = {
			...tableHeights,
			[resizingIndex]: nextHeight
		};
	}

	function stopResize() {
		resizingIndex = null;
		if (typeof window === 'undefined') return;

		window.removeEventListener('pointermove', handlePointerMove);
		window.removeEventListener('pointerup', stopResize);
		window.removeEventListener('pointercancel', stopResize);
	}

	onDestroy(() => {
		stopResize();
	});
</script>

<section class="bottom-panel">
	<div class="panel-body">
		{#if result.executing}
			<p class="panel-message">Executing...</p>
		{/if}

		{#if result.error}
			<p class="panel-message error">{result.error}</p>
		{/if}

		{#if result.data}
			{#if result.data.resultSets.length}
				<div class="result-list">
					{#each result.data.resultSets as resultSet, index}
						<section class="result-block">
							{#if resultSet.message}
								<p class="panel-message result-message">{resultSet.message}</p>
							{/if}

							{#if resultSet.columns.length}
								<div class="result-table-shell">
									<div
										class="results-table-wrap"
										style={`height: ${getTableHeight(index)}px;`}
									>
										<table class="results-table">
											<thead>
												<tr>
													{#each resultSet.columns as column}
														<th>{column}</th>
													{/each}
												</tr>
											</thead>
											<tbody>
												{#each resultSet.rows as row}
													<tr>
														{#each resultSet.columns as column}
															<td>{formatCellValue(row[column])}</td>
														{/each}
													</tr>
												{/each}
											</tbody>
										</table>
									</div>

									<div
										class:active-resize={resizingIndex === index}
										class="resize-handle"
										role="separator"
										aria-orientation="horizontal"
										aria-label={`Resize result ${index + 1}`}
										onpointerdown={(event) => startResize(index, event)}
									></div>
								</div>
							{/if}
						</section>
					{/each}
				</div>
			{:else if !result.data.message}
				<p class="panel-message">Command completed.</p>
			{/if}
		{:else if !result.executing && !result.error}
			<p class="panel-message">Execute a query to see results.</p>
		{/if}
	</div>
</section>

<style>
	.bottom-panel {
		height: 100%;
		min-height: 0;
		display: flex;
		flex-direction: column;
		box-sizing: border-box;
		border-top: 1px solid var(--sqd-border);
		background: var(--sqd-panel-strong-bg);
		padding: 6px 8px 8px;
	}

	.panel-body {
		min-height: 0;
		flex: 1;
		overflow: auto;
	}

	.result-list {
		display: flex;
		flex-direction: column;
		gap: 8px;
	}

	.result-block {
		background: var(--sqd-panel-bg);
		border-bottom: 1px solid var(--sqd-border);
	}

	.panel-message {
		margin: 0 0 8px;
		font-size: 0.78rem;
		color: var(--sqd-app-fg);
	}

	.result-message {
		padding: 6px 0 4px;
	}

	.panel-message.error {
		color: #dc2626;
	}

	.result-table-shell {
		padding: 0 0 6px;
	}

	.results-table-wrap {
		overflow: auto;
		border: 1px solid var(--sqd-border);
		background: var(--sqd-panel-bg);
	}

	.resize-handle {
		height: 10px;
		cursor: row-resize;
		position: relative;
	}

	.resize-handle::before {
		content: '';
		position: absolute;
		left: 50%;
		top: 50%;
		transform: translate(-50%, -50%);
		width: 44px;
		height: 3px;
		border-radius: 999px;
		background: var(--sqd-border);
	}

	.active-resize::before {
		background: var(--sqd-muted-fg);
	}

	.results-table {
		width: 100%;
		border-collapse: collapse;
		font-size: 0.78rem;
	}

	.results-table th,
	.results-table td {
		padding: 6px 8px;
		border-right: 1px solid var(--sqd-border);
		border-bottom: 1px solid var(--sqd-border);
		text-align: left;
		vertical-align: top;
		white-space: nowrap;
	}

	.results-table th {
		position: sticky;
		top: 0;
		background: var(--sqd-panel-strong-bg);
		font-weight: 600;
	}

	.results-table td {
		font-family: Consolas, 'Cascadia Mono', 'Courier New', monospace;
	}
</style>
