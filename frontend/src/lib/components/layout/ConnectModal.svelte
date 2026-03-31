<script lang="ts">
	import type { ConnectionFormData } from '$lib/types/connections';

	type Props = {
		errorMessage?: string;
		saving?: boolean;
		onCancel?: () => void;
		onSubmit?: (payload: ConnectionFormData) => void | Promise<void>;
	};

	let { errorMessage = '', saving = false, onCancel, onSubmit }: Props = $props();

	let form = $state<ConnectionFormData>({
		name: '',
		server: '',
		port: 1433,
		username: '',
		password: '',
		trustServerCertificate: true
	});

	async function handleSubmit(event: SubmitEvent) {
		event.preventDefault();

		await onSubmit?.({
			name: form.name.trim(),
			server: form.server.trim(),
			port: Number(form.port),
			username: form.username.trim(),
			password: form.password,
			trustServerCertificate: form.trustServerCertificate
		});
	}

	function handleBackdropClick(event: MouseEvent) {
		if (event.target === event.currentTarget && !saving) {
			onCancel?.();
		}
	}
</script>

<div class="modal-backdrop" role="presentation" onclick={handleBackdropClick}>
	<form class="connect-modal" onsubmit={handleSubmit}>
		<div class="modal-header">
			<h2>Connect to SQL Server</h2>
		</div>

		<div class="modal-body">
			<label>
				<span>Name</span>
				<input bind:value={form.name} placeholder="Optional display name" />
			</label>

			<div class="split-row">
				<label>
					<span>Server</span>
					<input bind:value={form.server} required />
				</label>

				<label class="port-field">
					<span>Port</span>
					<input bind:value={form.port} type="number" min="1" required />
				</label>
			</div>

			<label>
				<span>Username</span>
				<input bind:value={form.username} required />
			</label>

			<label>
				<span>Password</span>
				<input bind:value={form.password} type="password" required />
			</label>

			<label class="checkbox-row">
				<input bind:checked={form.trustServerCertificate} type="checkbox" />
				<span>Trust Server Certificate</span>
			</label>

			{#if errorMessage}
				<p class="modal-error">{errorMessage}</p>
			{/if}
		</div>

		<div class="modal-actions">
			<button class="modal-button" type="button" onclick={onCancel} disabled={saving}>Cancel</button>
			<button class="modal-button primary" type="submit" disabled={saving}>
				{saving ? 'Connecting...' : 'Connect'}
			</button>
		</div>
	</form>
</div>

<style>
	.modal-backdrop {
		position: fixed;
		inset: 0;
		z-index: 20;
		display: grid;
		place-items: center;
		padding: 16px;
		background: color-mix(in srgb, var(--sqd-app-bg) 62%, transparent);
	}

	.connect-modal {
		width: min(420px, 100%);
		border: 1px solid var(--sqd-border);
		background: var(--sqd-panel-strong-bg);
		box-shadow: 0 18px 40px rgba(0, 0, 0, 0.24);
	}

	.modal-header,
	.modal-actions {
		padding: 10px 12px;
		border-bottom: 1px solid var(--sqd-border);
	}

	.modal-header h2 {
		margin: 0;
		font-size: 0.82rem;
		font-weight: 600;
		letter-spacing: 0.04em;
		text-transform: uppercase;
		color: var(--sqd-muted-fg);
	}

	.modal-body {
		display: grid;
		gap: 10px;
		padding: 12px;
	}

	label {
		display: grid;
		gap: 6px;
		font-size: 0.78rem;
	}

	label span {
		color: var(--sqd-muted-fg);
	}

	input {
		width: 100%;
		box-sizing: border-box;
		padding: 7px 8px;
		border: 1px solid var(--sqd-border);
		background: var(--sqd-panel-bg);
		color: var(--sqd-app-fg);
	}

	.split-row {
		display: grid;
		grid-template-columns: minmax(0, 1fr) 92px;
		gap: 10px;
	}

	.checkbox-row {
		grid-template-columns: auto 1fr;
		align-items: center;
		gap: 8px;
	}

	.checkbox-row input {
		width: 14px;
		height: 14px;
		margin: 0;
	}

	.modal-error {
		margin: 0;
		font-size: 0.78rem;
		color: var(--color-error-500);
	}

	.modal-actions {
		display: flex;
		justify-content: flex-end;
		gap: 8px;
		border-top: 1px solid var(--sqd-border);
		border-bottom: 0;
	}

	.modal-button {
		padding: 6px 10px;
		border: 1px solid var(--sqd-border);
		background: var(--sqd-button-bg);
		color: var(--sqd-app-fg);
		cursor: pointer;
	}

	.modal-button.primary {
		background: var(--sqd-button-bg-hover);
	}

	.modal-button:disabled {
		cursor: wait;
		opacity: 0.7;
	}
</style>
