export type SavedConnection = {
	id: string;
	name?: string | null;
	server: string;
	port: number;
	username: string;
	trustServerCertificate: boolean;
	displayName: string;
	isConnected: boolean;
};

export type DatabaseNode = {
	name: string;
	isSystem: boolean;
};

export type TableNode = {
	schema: string;
	name: string;
};

export type ConnectionFormData = {
	name: string;
	server: string;
	port: number;
	username: string;
	password: string;
	trustServerCertificate: boolean;
};
