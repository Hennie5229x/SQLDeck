export type QueryExecutionResultSet = {
	columns: string[];
	rows: Record<string, unknown>[];
	rowsAffected: number;
	message: string;
};

export type QueryExecutionResponse = {
	success: boolean;
	message: string;
	resultSets: QueryExecutionResultSet[];
	executedSql: string;
	server: string;
	database: string;
};

export type QueryExecutionResultState = {
	data: QueryExecutionResponse | null;
	error: string;
	executing: boolean;
};
