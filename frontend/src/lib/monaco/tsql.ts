const TSQL_LANGUAGE_ID = 'tsql';

const TSQL_KEYWORDS = [
	'ADD', 'ALTER', 'AND', 'AS', 'ASC', 'BEGIN', 'BETWEEN', 'BREAK', 'BY', 'CASE', 'CHECK',
	'CLUSTERED', 'COLLATE', 'COLUMN', 'COMMIT', 'CONSTRAINT', 'CONTAINS', 'CONTINUE', 'CREATE',
	'CROSS', 'CURRENT', 'CURRENT_DATE', 'CURRENT_TIME', 'CURRENT_TIMESTAMP', 'CURRENT_USER',
	'DATABASE', 'DBCC', 'DEALLOCATE', 'DECLARE', 'DEFAULT', 'DELETE', 'DESC', 'DISTINCT',
	'DISTRIBUTED', 'DROP', 'ELSE', 'END', 'EXEC', 'EXECUTE', 'EXISTS', 'EXTERNAL', 'FETCH',
	'FOR', 'FOREIGN', 'FROM', 'FULL', 'FUNCTION', 'GO', 'GRANT', 'GROUP', 'HAVING', 'HOLDLOCK',
	'IDENTITY', 'IF', 'IN', 'INDEX', 'INNER', 'INSERT', 'INTERSECT', 'INTO', 'IS', 'JOIN',
	'KEY', 'LEFT', 'LIKE', 'MERGE', 'NOCHECK', 'NONCLUSTERED', 'NOT', 'NULL', 'OF', 'OFF',
	'ON', 'OPEN', 'OPENJSON', 'OPTION', 'OR', 'ORDER', 'OUTER', 'OVER', 'PARTITION', 'PERCENT',
	'PRIMARY', 'PRINT', 'PROC', 'PROCEDURE', 'RAISERROR', 'REFERENCES', 'RETURN', 'RETURNS',
	'REVOKE', 'RIGHT', 'ROLLBACK', 'ROWCOUNT', 'SCHEMA', 'SELECT', 'SET', 'TABLE', 'THEN',
	'TOP', 'TRAN', 'TRANSACTION', 'TRIGGER', 'TRUNCATE', 'TRY', 'UNION', 'UNIQUE', 'UPDATE',
	'USE', 'VALUES', 'VIEW', 'WAITFOR', 'WHEN', 'WHERE', 'WHILE', 'WITH',
	'BIGINT', 'BINARY', 'BIT', 'CHAR', 'DATE', 'DATETIME', 'DATETIME2', 'DATETIMEOFFSET',
	'DECIMAL', 'FLOAT', 'IMAGE', 'INT', 'MONEY', 'NCHAR', 'NTEXT', 'NUMERIC', 'NVARCHAR',
	'REAL', 'SMALLDATETIME', 'SMALLINT', 'SMALLMONEY', 'SQL_VARIANT', 'SYSNAME', 'TEXT',
	'TIME', 'TINYINT', 'UNIQUEIDENTIFIER', 'VARBINARY', 'VARCHAR', 'XML'
];

const TSQL_BUILTINS = [
	'APP_NAME', 'CAST', 'COALESCE', 'CONCAT', 'COUNT', 'CURRENT_TIMESTAMP', 'DATEADD',
	'DATEDIFF', 'DATENAME', 'DATEPART', 'DB_ID', 'DB_NAME', 'GETDATE', 'HOST_NAME', 'IIF',
	'ISNULL', 'JSON_QUERY', 'JSON_VALUE', 'LEN', 'NEWID', 'NULLIF', 'OBJECT_ID', 'ROW_NUMBER',
	'SCOPE_IDENTITY', 'STRING_AGG', 'STRING_SPLIT', 'SUM', 'SYSDATETIME', 'SYSTEM_USER',
	'TRY_CAST', 'TRY_CONVERT'
];

export function ensureTsqlLanguage(monaco: typeof import('monaco-editor')) {
	if (monaco.languages.getLanguages().some((language) => language.id === TSQL_LANGUAGE_ID)) {
		return TSQL_LANGUAGE_ID;
	}

	monaco.languages.register({
		id: TSQL_LANGUAGE_ID,
		aliases: ['T-SQL', 'Transact-SQL', 'tsql'],
		extensions: ['.sql'],
		mimetypes: ['text/x-tsql']
	});

	monaco.languages.setLanguageConfiguration(TSQL_LANGUAGE_ID, {
		comments: {
			lineComment: '--',
			blockComment: ['/*', '*/']
		},
		brackets: [
			['{', '}'],
			['[', ']'],
			['(', ')']
		],
		autoClosingPairs: [
			{ open: '{', close: '}' },
			{ open: '[', close: ']' },
			{ open: '(', close: ')' },
			{ open: '"', close: '"' },
			{ open: "'", close: "'" }
		],
		surroundingPairs: [
			{ open: '{', close: '}' },
			{ open: '[', close: ']' },
			{ open: '(', close: ')' },
			{ open: '"', close: '"' },
			{ open: "'", close: "'" }
		]
	});

	monaco.languages.setMonarchTokensProvider(TSQL_LANGUAGE_ID, {
		defaultToken: '',
		tokenPostfix: '.sql',
		ignoreCase: true,
		keywords: TSQL_KEYWORDS,
		builtinFunctions: TSQL_BUILTINS,
		operators: ['+', '-', '*', '/', '%', '=', '<>', '!=', '>', '<', '>=', '<=', '!<', '!>', '!'],
		tokenizer: {
			root: [
				{ include: '@whitespace' },
				[/@@[a-zA-Z_][\w$#@]*/, 'predefined'],
				[/@[a-zA-Z_][\w$#@]*/, 'variable'],
				[/\[[^\]]+\]/, 'identifier'],
				[/"([^"]|"")*"/, 'identifier'],
				[/[a-zA-Z_#][\w$#@]*/, {
					cases: {
						'@keywords': 'keyword',
						'@builtinFunctions': 'predefined',
						'@default': 'identifier'
					}
				}],
				[/[()]/, '@brackets'],
				[/[<>]/, 'delimiter'],
				[/[;,.]/, 'delimiter'],
				[/[=><!~?:&|+\-*\/\^%]+/, {
					cases: {
						'@operators': 'operator',
						'@default': ''
					}
				}],
				[/0x[0-9a-f]+/, 'number.hex'],
				[/\d*\.\d+([eE][\-+]?\d+)?/, 'number.float'],
				[/\d+/, 'number'],
				[/N'/, { token: 'string', next: '@string' }],
				[/'/, { token: 'string', next: '@string' }]
			],
			whitespace: [
				[/[ \t\r\n]+/, ''],
				[/--.*$/, 'comment'],
				[/\/\*/, { token: 'comment', next: '@comment' }]
			],
			comment: [
				[/[^/*]+/, 'comment'],
				[/\/\*/, 'comment', '@push'],
				[/\*\//, 'comment', '@pop'],
				[/[/*]/, 'comment']
			],
			string: [
				[/[^']+/, 'string'],
				[/''/, 'string.escape'],
				[/'/, { token: 'string', next: '@pop' }]
			]
		}
	});

	return TSQL_LANGUAGE_ID;
}
