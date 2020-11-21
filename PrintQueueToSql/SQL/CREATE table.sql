USE AAD;

CREATE TABLE t_printer_queues (
	printer_name		NVARCHAR(100)		UNIQUE		NOT NULL
	,jobs_in_queue		INTEGER
	,status				NVARCHAR(MAX)
	,type				NVARCHAR(10)
	,polling_enabled	CHAR(1)				DEFAULT 'Y'
)


GO
GRANT SELECT,UPDATE,INSERT,DELETE ON [t_printer_queues] TO [AAD_USER], [WA_USER] AS [dbo]
GO
