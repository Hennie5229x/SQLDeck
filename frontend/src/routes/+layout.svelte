<script lang="ts">
	import { onMount } from 'svelte';

	import '../app.css';
	import favicon from '$lib/assets/favicon.svg';
	import { initializeTheme } from '$lib/theme';

	let { children } = $props();

	onMount(() => {
		initializeTheme();
	});
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

{@render children()}
