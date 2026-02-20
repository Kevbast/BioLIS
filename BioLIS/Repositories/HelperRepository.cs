using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using BioLIS.Data;

#region STORED PROCEDURES
/*
 * 1. SP_GetNextID (@TableName, @NextID OUT)
 * 
 * CREATE PROCEDURE SP_GetNextID
    @TableName NVARCHAR(50),
    @NextID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @TableName = 'Patients'
        SELECT @NextID = ISNULL(MAX(PatientID), 0) + 1 FROM Patients;
    ELSE IF @TableName = 'Doctors'
        SELECT @NextID = ISNULL(MAX(DoctorID), 0) + 1 FROM Doctors;
    ELSE IF @TableName = 'Orders'
        SELECT @NextID = ISNULL(MAX(OrderID), 0) + 1 FROM Orders;
    ELSE IF @TableName = 'LabTests'
        SELECT @NextID = ISNULL(MAX(TestID), 0) + 1 FROM LabTests;
    ELSE IF @TableName = 'SampleTypes'
        SELECT @NextID = ISNULL(MAX(SampleID), 0) + 1 FROM SampleTypes;
    ELSE IF @TableName = 'TestResults'
        SELECT @NextID = ISNULL(MAX(ResultID), 0) + 1 FROM TestResults;
    ELSE IF @TableName = 'ReferenceRanges'
        SELECT @NextID = ISNULL(MAX(RangeID), 0) + 1 FROM ReferenceRanges;
    ELSE IF @TableName = 'Users'
        SELECT @NextID = ISNULL(MAX(UserID), 0) + 1 FROM Users;
    ELSE
        SET @NextID = 1;
END
GO
 * 
 * 
 * 2. SP_GenerateOrderNumber (@NewOrderNumber OUT)
 * 
 * CREATE PROCEDURE SP_GetReferenceRange
    @PatientID INT,
    @TestID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Gender CHAR(1);
    DECLARE @Age INT;
    
    SELECT 
        @Gender = Gender, 
        @Age = DATEDIFF(YEAR, BirthDate, GETDATE()) - 
               CASE 
                   WHEN DATEADD(YEAR, DATEDIFF(YEAR, BirthDate, GETDATE()), BirthDate) > GETDATE() 
                   THEN 1 
                   ELSE 0 
               END
    FROM Patients 
    WHERE PatientID = @PatientID;
    
    IF @Gender IS NULL
    BEGIN
        SELECT NULL AS RangeID, NULL AS MinVal, NULL AS MaxVal, 'Paciente no encontrado' AS ErrorMessage;
        RETURN;
    END
    
    SELECT TOP 1
        RangeID,
        TestID,
        Gender,
        MinAgeYear,
        MaxAgeYear,
        MinVal,
        MaxVal
    FROM ReferenceRanges
    WHERE TestID = @TestID
      AND (Gender = @Gender OR Gender = 'A')
      AND @Age BETWEEN MinAgeYear AND MaxAgeYear
    ORDER BY 
        CASE WHEN Gender = @Gender THEN 0 ELSE 1 END,
        (MaxAgeYear - MinAgeYear) ASC;
END
GO

3.PRCEDIMIENTO QUE VERIFICA SI SE PUEDE BORRAR
CREATE PROCEDURE SP_CanDelete
    @TableName NVARCHAR(50),
    @RecordID INT,
    @CanDelete BIT OUTPUT,
    @Message NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Count INT = 0;
    
    IF @TableName = 'Patients'
    BEGIN
        SELECT @Count = COUNT(*) FROM Orders WHERE PatientID = @RecordID;
        SET @Message = CASE WHEN @Count > 0 
            THEN 'No se puede eliminar. El paciente tiene ' + CAST(@Count AS NVARCHAR(10)) + ' órdenes.'
            ELSE 'El paciente puede ser eliminado.' END;
    END
    ELSE IF @TableName = 'Doctors'
    BEGIN
        SELECT @Count = COUNT(*) FROM Orders WHERE DoctorID = @RecordID;
        SET @Message = CASE WHEN @Count > 0 
            THEN 'No se puede eliminar. El médico tiene ' + CAST(@Count AS NVARCHAR(10)) + ' órdenes.'
            ELSE 'El médico puede ser eliminado.' END;
    END
    ELSE IF @TableName = 'SampleTypes'
    BEGIN
        SELECT @Count = COUNT(*) FROM LabTests WHERE SampleID = @RecordID;
        SET @Message = CASE WHEN @Count > 0 
            THEN 'No se puede eliminar. El tipo tiene ' + CAST(@Count AS NVARCHAR(10)) + ' exámenes.'
            ELSE 'El tipo de muestra puede ser eliminado.' END;
    END
    ELSE IF @TableName = 'LabTests'
    BEGIN
        SELECT @Count = COUNT(*) FROM TestResults WHERE TestID = @RecordID;
        SET @Message = CASE WHEN @Count > 0 
            THEN 'No se puede eliminar. El examen tiene ' + CAST(@Count AS NVARCHAR(10)) + ' resultados.'
            ELSE 'El examen puede ser eliminado.' END;
    END
    ELSE IF @TableName = 'Orders'
    BEGIN
        SELECT @Count = COUNT(*) FROM TestResults WHERE OrderID = @RecordID;
        SET @Message = CASE WHEN @Count > 0 
            THEN 'No se puede eliminar. La orden tiene ' + CAST(@Count AS NVARCHAR(10)) + ' resultados.'
            ELSE 'La orden puede ser eliminada.' END;
    END
    
    SET @CanDelete = CASE WHEN @Count = 0 THEN 1 ELSE 0 END;
END
GO



 * 
 * 
 * 
 * 
 * 
 */
#endregion
namespace BioLIS.Repositories
{
    /// <summary>
    /// Repositorio con métodos auxiliares y utilitarios
    /// - GetNextID
    /// - GenerateOrderNumber
    /// - CanDelete
    /// - ValidateResult
    /// </summary>
    public class HelperRepository
    {
        private readonly LaboratorioContext _context;

        public HelperRepository(LaboratorioContext context)
        {
            _context = context;
        }

        #region MÉTODOS DE ID Y NUMERACIÓN

        /// <summary>
        /// Obtener el siguiente ID disponible para una tabla
        /// </summary>
        /// <param name="tableName">Nombre de la tabla: Patients, Doctors, Orders, etc.</param>
        /// <returns>Siguiente ID libre</returns>
        public async Task<int> GetNextIdAsync(string tableName)
        {
            var paramTable = new SqlParameter("@TableName", tableName);
            var paramOutput = new SqlParameter("@NextID", 0)
            {
                Direction = ParameterDirection.Output,
                SqlDbType = SqlDbType.Int
            };

            string sql = "EXEC SP_GetNextID @TableName, @NextID OUT";
            await _context.Database.ExecuteSqlRawAsync(sql, paramTable, paramOutput);

            return (int)paramOutput.Value;
        }

        /// <summary>
        /// Generar número de orden único (ORD-YYYYMMDD-XXXX)
        /// </summary>
        /// <returns>Número de orden generado</returns>
        public async Task<string> GenerateOrderNumberAsync()
        {
            var paramOutput = new SqlParameter("@NewOrderNumber", SqlDbType.NVarChar, 20)
            {
                Direction = ParameterDirection.Output
            };

            string sql = "EXEC SP_GenerateOrderNumber @NewOrderNumber OUT";
            await _context.Database.ExecuteSqlRawAsync(sql, paramOutput);

            return paramOutput.Value?.ToString() ?? string.Empty;
        }

        #endregion

        #region MÉTODOS DE VALIDACIÓN

        /// <summary>
        /// Verificar si un registro puede ser eliminado
        /// </summary>
        /// <param name="tableName">Nombre de la tabla</param>
        /// <param name="recordId">ID del registro</param>
        /// <returns>Tupla (CanDelete, Message)</returns>
        public async Task<(bool CanDelete, string Message)> CanDeleteAsync(string tableName, int recordId)
        {
            var paramTable = new SqlParameter("@TableName", tableName);
            var paramId = new SqlParameter("@RecordID", recordId);
            var paramCanDelete = new SqlParameter("@CanDelete", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };
            var paramMessage = new SqlParameter("@Message", SqlDbType.NVarChar, 200)
            {
                Direction = ParameterDirection.Output
            };

            string sql = "EXEC SP_CanDelete @TableName, @RecordID, @CanDelete OUT, @Message OUT";
            await _context.Database.ExecuteSqlRawAsync(sql, paramTable, paramId, paramCanDelete, paramMessage);

            bool canDelete = paramCanDelete.Value != DBNull.Value && (bool)paramCanDelete.Value;
            string message = paramMessage.Value?.ToString() ?? "Error desconocido";

            return (canDelete, message);
        }

        /// <summary>
        /// Validar un resultado de examen
        /// </summary>
        /// <param name="testId">ID del examen</param>
        /// <param name="patientId">ID del paciente</param>
        /// <param name="resultValue">Valor del resultado</param>
        /// <returns>Tupla (Status, IsAbnormal)</returns>
        public async Task<(string Status, bool IsAbnormal)> ValidateResultAsync(int testId, int patientId, decimal resultValue)
        {
            var paramTestId = new SqlParameter("@TestID", testId);
            var paramPatientId = new SqlParameter("@PatientID", patientId);
            var paramResultValue = new SqlParameter("@ResultValue", resultValue);
            var paramStatus = new SqlParameter("@Status", SqlDbType.NVarChar, 20)
            {
                Direction = ParameterDirection.Output
            };
            var paramIsAbnormal = new SqlParameter("@IsAbnormal", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            string sql = "EXEC SP_ValidateResult @TestID, @PatientID, @ResultValue, @Status OUT, @IsAbnormal OUT";
            await _context.Database.ExecuteSqlRawAsync(sql, paramTestId, paramPatientId, paramResultValue, paramStatus, paramIsAbnormal);

            string status = paramStatus.Value?.ToString() ?? "Sin Rango";
            bool isAbnormal = paramIsAbnormal.Value != DBNull.Value && (bool)paramIsAbnormal.Value;

            return (status, isAbnormal);
        }

        #endregion
    }
}