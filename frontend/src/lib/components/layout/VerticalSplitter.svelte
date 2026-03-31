<script lang="ts">
	type Props = {
		active?: boolean;
		onpointerdown?: (event: PointerEvent) => void;
	};

	let { active = false, onpointerdown }: Props = $props();
</script>

<div
	class:active
	class="splitter splitter-vertical"
	role="separator"
	aria-orientation="vertical"
	aria-label="Resize sidebar"
	onpointerdown={onpointerdown}
></div>

<style>
	.splitter {
		position: relative;
		height: 100%;
		width: 100%;
		background: var(--sqd-shell-bg);
		cursor: col-resize;
	}

	.splitter::before {
		content: '';
		position: absolute;
		inset: 0;
		margin: auto;
		width: 1px;
		height: 100%;
		background: var(--sqd-border);
	}

	.splitter:hover::before,
	.splitter.active::before {
		background: var(--sqd-muted-fg);
	}
</style>
