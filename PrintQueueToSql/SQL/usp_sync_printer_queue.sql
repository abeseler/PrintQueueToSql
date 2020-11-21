USE [AAD]
GO

/****** Object:  StoredProcedure [dbo].[usp_sync_printer_queue]    Script Date: 4/11/2019 11:22:07 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/******************************************************************************************************

This stored procedure updates the t_printer_queues table

The printers are polled from a Windows Service named "HighJump Print Queue to SQL" on the print server

******************************************************************************************************* 
EXAMPLE CALL

EXEC [usp_sync_printer_queue]
	@in_vchPrinterName		= 'CA00Pack'
	,@in_vchPrinterStatus	= 'None'
	,@in_nJobsInQueue		= 0

*******************************************************************************************************
CHANGE LOG

Date		Name				Comments
20200929	Adam Beseler		Initial Creation

******************************************************************************************************/

CREATE PROCEDURE [dbo].[usp_sync_printer_queue]
	@in_vchPrinterName				NVARCHAR(100)
	,@in_vchPrinterStatus			NVARCHAR(MAX)
	,@in_nJobsInQueue				INTEGER

AS
 
SET NOCOUNT ON

BEGIN TRY
	UPDATE t_printer_queues WITH (UPDLOCK,ROWLOCK)
	SET status = @in_vchPrinterStatus
		,jobs_in_queue = @in_nJobsInQueue
	WHERE printer_name = @in_vchPrinterName

	GOTO EXIT_LABEL
END TRY
BEGIN CATCH
	DECLARE
		--Error handling variables
		@v_nErrorNum			INTEGER
		,@v_vchErrorMsg			NVARCHAR(MAX)

	SELECT @v_vchErrorMsg = 'Procedure: usp_sync_printer_queue: ',@v_nErrorNum = ERROR_NUMBER()

	IF @v_nErrorNum = 1205
	BEGIN
		SET @v_vchErrorMsg = CONCAT(@v_vchErrorMsg,'Deadlock Error = 40001 ',ERROR_MESSAGE())
	END
	ELSE    
	BEGIN
		SET @v_vchErrorMsg = CONCAT(@v_vchErrorMsg,'SQL Error = ',@v_nErrorNum,' ', ERROR_MESSAGE())
	END

	RAISERROR(@v_vchErrorMsg,18,1)
END CATCH

EXIT_LABEL:
	RETURN

GO
GRANT EXECUTE ON [dbo].[usp_sync_printer_queue] TO [AAD_USER], [WA_USER] AS [dbo]
GO
