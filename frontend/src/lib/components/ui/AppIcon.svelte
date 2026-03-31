<script lang="ts">
	import Icon from '@iconify/svelte';

	type Props = {
		icon: string;
		width?: string | number;
		height?: string | number;
		class?: string;
	};

	let { icon, width = '1rem', height = '1rem', class: className = '' }: Props = $props();
</script>

<Icon {icon} {width} {height} class={className} />
