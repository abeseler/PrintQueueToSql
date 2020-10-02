USE [AAD]
GO

/****** Object:  StoredProcedure [dbo].[usp_get_printer_list]    Script Date: 4/11/2019 11:22:07 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/******************************************************************************************************

This stored procedure that returns a printer list for the "HighJump Print Queue to SQL" Windows Service
to sync.

******************************************************************************************************* 
EXAMPLE CALL

EXEC [usp_get_printer_list]

*******************************************************************************************************
CHANGE LOG

Date		Name				Comments
20200929	Adam Beseler		Initial Creation

******************************************************************************************************/

CREATE PROCEDURE [dbo].[usp_get_printer_list]

AS
 
SET NOCOUNT ON

BEGIN TRY
	SELECT
		printer_name
		,jobs
		,status
	FROM t_printer_queues WITH (NOLOCK)
	WHERE type = 'RPT'

	GOTO EXIT_LABEL
END TRY
BEGIN CATCH
	DECLARE
		--Error handling variables
		@v_nErrorNum			INTEGER
		,@v_vchErrorMsg			NVARCHAR(MAX)

	SELECT @v_vchErrorMsg = 'Procedure: usp_get_printer_list: ',@v_nErrorNum = ERROR_NUMBER()

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
GRANT EXECUTE ON [dbo].[usp_get_printer_list] TO [AAD_USER], [WA_USER] AS [dbo]
GO
