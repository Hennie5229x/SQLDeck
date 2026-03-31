<script lang="ts">
	import { onMount } from 'svelte';

	import AppIcon from '$lib/components/ui/AppIcon.svelte';
	import { getActiveTheme, toggleTheme, type ThemeName } from '$lib/theme';

	type Props = {
		onExecute?: () => void | Promise<void>;
		canExecute?: boolean;
		executing?: boolean;
		executionTargetLabel?: string;
	};

	let {
		onExecute,
		canExecute = false,
		executing = false,
		executionTargetLabel = ''
	}: Props = $props();

	let currentTheme = $state<ThemeName>('terminus');

	onMount(() => {
		currentTheme = getActiveTheme();
	});

	function handleThemeToggle() {
		currentTheme = toggleTheme();
	}
</script>

<div class="top-bar">
	<div class="top-bar-title">
		<AppIcon icon="mdi:database-outline" class="top-bar-icon" />
		<div class="top-bar-copy">
			<span>SQLDeck</span>
			{#if executionTargetLabel}
				<span class="top-bar-target">{executionTargetLabel}</span>
			{/if}
		</div>
	</div>
	<div class="top-bar-actions">
		<button
			class="execute-button"
			type="button"
			disabled={!canExecute || executing}
			onclick={onExecute}
		>
			<AppIcon icon="mdi:play" width="0.9rem" height="0.9rem" />
			<span>{executing ? 'Executing...' : 'Execute'}</span>
		</button>
		<button class="theme-toggle" type="button" onclick={handleThemeToggle}
			>{currentTheme === 'terminus' ? 'Light Mode' : 'Dark Mode'}</button
		>
	</div>
</div>

<style>
	.top-bar {
		height: 100%;
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 0 12px;
		box-sizing: border-box;
		border-bottom: 1px solid var(--sqd-border);
		background: var(--sqd-chrome-bg);
		color: var(--sqd-app-fg);
		font-size: 0.78rem;
		font-weight: 600;
		letter-spacing: 0.04em;
		text-transform: uppercase;
	}

	.top-bar-title {
		display: flex;
		align-items: center;
		gap: 8px;
		min-width: 0;
	}

	.top-bar-copy {
		display: flex;
		align-items: baseline;
		gap: 10px;
		min-width: 0;
	}

	.top-bar-target {
		color: var(--sqd-muted-fg);
		font-size: 0.7rem;
		font-weight: 500;
		letter-spacing: 0;
		text-transform: none;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.top-bar-actions {
		display: flex;
		align-items: center;
		gap: 8px;
	}

	:global(.top-bar-icon) {
		flex: 0 0 auto;
		color: var(--sqd-muted-fg);
	}

	.execute-button,
	.theme-toggle {
		border: 1px solid var(--sqd-border);
		background: var(--sqd-button-bg);
		color: inherit;
		padding: 4px 8px;
		border-radius: 4px;
		cursor: pointer;
		font-size: 0.72rem;
		text-transform: none;
		letter-spacing: 0;
	}

	.execute-button {
		display: inline-flex;
		align-items: center;
		gap: 6px;
	}

	.execute-button:hover,
	.theme-toggle:hover {
		background: var(--sqd-button-bg-hover);
	}

	.execute-button:disabled {
		opacity: 0.55;
		cursor: default;
	}
</style>
