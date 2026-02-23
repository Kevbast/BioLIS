using BioLab.Models;
using BioLIS.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
#region STORED PROCEDURES
/*
 1.
CREATE PROCEDURE SP_GetOrderDetails
    @OrderID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        O.OrderID,
        O.OrderNumber,
        O.OrderDate,
        P.PatientID,
        P.FirstName,
        P.LastName,
        P.FirstName + ' ' + P.LastName AS PatientName,
        P.Gender,
        P.BirthDate,
        DATEDIFF(YEAR, P.BirthDate, GETDATE()) - 
            CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, P.BirthDate, GETDATE()), P.BirthDate) > GETDATE() 
                 THEN 1 ELSE 0 END AS PatientAge,
        P.Email AS PatientEmail,
        P.PhotoFilename,
        D.DoctorID,
        D.FullName AS DoctorName,
        D.LicenseNumber,
        D.Email AS DoctorEmail,
        TR.ResultID,
        TR.TestID,
        LT.TestName,
        LT.Units,
        TR.ResultValue,
        TR.IsAbnormal,
        TR.Notes,
        ST.SampleName,
        ST.ContainerColor,
        RR.MinVal,
        RR.MaxVal,
        CASE WHEN RR.MinVal IS NOT NULL THEN 
            CAST(RR.MinVal AS NVARCHAR(10)) + ' - ' + CAST(RR.MaxVal AS NVARCHAR(10))
        ELSE 'Sin rango definido' END AS ReferenceRange,
        CASE 
            WHEN TR.ResultValue IS NULL THEN 'Pendiente'
            WHEN RR.MinVal IS NULL THEN 'Sin Rango'
            WHEN TR.ResultValue >= RR.MinVal AND TR.ResultValue <= RR.MaxVal THEN 'Normal'
            WHEN TR.ResultValue < (RR.MinVal - (RR.MaxVal - RR.MinVal) * 0.2) OR 
                 TR.ResultValue > (RR.MaxVal + (RR.MaxVal - RR.MinVal) * 0.2) THEN 'Critico'
            ELSE 'Anormal'
        END AS ValidationStatus
    FROM Orders O
    INNER JOIN Patients P ON O.PatientID = P.PatientID
    INNER JOIN Doctors D ON O.DoctorID = D.DoctorID
    INNER JOIN TestResults TR ON O.OrderID = TR.OrderID
    INNER JOIN LabTests LT ON TR.TestID = LT.TestID
    INNER JOIN SampleTypes ST ON LT.SampleID = ST.SampleID
    LEFT JOIN ReferenceRanges RR ON TR.TestID = RR.TestID
        AND (RR.Gender = P.Gender OR RR.Gender = 'A')
        AND DATEDIFF(YEAR, P.BirthDate, GETDATE()) BETWEEN RR.MinAgeYear AND RR.MaxAgeYear
    WHERE O.OrderID = @OrderID
    ORDER BY LT.TestName;
END
GO

 2.
CREATE PROCEDURE SP_GetRequiredTubes
    @OrderID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        ST.SampleID,
        ST.SampleName,
        ST.ContainerColor,
        COUNT(DISTINCT TR.TestID) AS TestCount,
        STRING_AGG(LT.TestName, ', ') AS TestNames
    FROM TestResults TR
    INNER JOIN LabTests LT ON TR.TestID = LT.TestID
    INNER JOIN SampleTypes ST ON LT.SampleID = ST.SampleID
    WHERE TR.OrderID = @OrderID
    GROUP BY ST.SampleID, ST.SampleName, ST.ContainerColor
    ORDER BY ST.SampleName;
END
GO
3.
CREATE PROCEDURE SP_GetReferenceRange
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

 */
#endregion
namespace BioLIS.Repositories
{
    //---------Repositorio para gestión de Órdenes y Resultados----------
    //Incluye lógica avanzada: tubos necesarios, validación automática de resultados
    public class OrderRepository
    {
        private readonly LaboratorioContext context;
        private readonly HelperRepository helper;

        public OrderRepository(LaboratorioContext context, HelperRepository helper)
        {
            this.context = context;
            this.helper = helper;
        }

#region CRUD GESTIÓN DE ÓRDENES
        //1.Obtener todas las órdenes
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await this.context.Orders
                .Include(o => o.Patient)
                .Include(o => o.Doctor)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
        //2.Obtener órden por ID
        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await this.context.Orders
                .Include(o => o.Patient)
                .Include(o => o.Doctor)
                .Include(o => o.TestResults)
                    .ThenInclude(tr => tr.LabTest)
                        .ThenInclude(lt => lt.SampleType)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);
        }
        //3.Crear una órden
        public async Task<Order> CreateOrderAsync(int patientId, int doctorId)
        {
            // Obtener nuevo ID
            int newId = await helper.GetNextIdAsync("Orders");

            // Generar número de orden único
            string orderNumber = await helper.GenerateOrderNumberAsync();

            Order order = new Order
            {
                OrderID = newId,
                PatientID = patientId,
                DoctorID = doctorId,
                OrderDate = DateTime.Now,
                OrderNumber = orderNumber
            };

            this.context.Orders.Add(order);
            await this.context.SaveChangesAsync();

            return order;
        }
        //4.Actualizar órden
        public async Task<bool> UpdateOrderAsync(Order order)
        {
            var existing = await this.context.Orders.FindAsync(order.OrderID);
            if (existing == null)
                return false;

            existing.PatientID = order.PatientID;
            existing.DoctorID = order.DoctorID;
            existing.OrderDate = order.OrderDate;
            existing.OrderNumber = order.OrderNumber;

            await this.context.SaveChangesAsync();
            return true;
        }
        //5.Delete órden
        public async Task<(bool Success, string Message)> DeleteOrderAsync(int orderId)
        {
            var validation = await helper.CanDeleteAsync("Orders", orderId);

            if (!validation.CanDelete)
                return (false, validation.Message);

            var order = await this.context.Orders.FindAsync(orderId);
            if (order == null)
                return (false, "Orden no encontrada.");

            this.context.Orders.Remove(order);
            await this.context.SaveChangesAsync();

            return (true, "Orden eliminada exitosamente.");
        }
        //6.Obtener órdenes de hoy
        public async Task<List<Order>> GetTodayOrdersAsync()
        {
            var today = DateTime.Today;
            return await this.context.Orders
                .Include(o => o.Patient)
                .Include(o => o.Doctor)
                .Where(o => o.OrderDate.Date == today)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
        //7.Obtener órdenes por paciente
        public async Task<List<Order>> GetOrdersByPatientAsync(int patientId)
        {
            return await this.context.Orders
                .Include(o => o.Doctor)
                .Where(o => o.PatientID == patientId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
        //8.Obtener órdenes por doctor
        public async Task<List<Order>> GetOrdersByDoctorAsync(int doctorId)
        {
            return await this.context.Orders
                .Include(o => o.Patient)
                .Where(o => o.DoctorID == doctorId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }


#endregion
#region GESTIÓN DE RESULTADOS

        //1.Obtener todos los resultados de una orden
        public async Task<List<TestResult>> GetResultsByOrderAsync(int orderId)
        {
            return await this.context.TestResults
                .Include(tr => tr.LabTest)
                    .ThenInclude(lt => lt.SampleType)
                .Where(tr => tr.OrderID == orderId)
                .OrderBy(tr => tr.LabTest.TestName)
                .ToListAsync();
        }
        //2.Agregar resultado a una orden CON VALIDACIÓN AUTOMÁTICA
        public async Task<TestResult> AddTestResultAsync(int orderId, int testId,
                                                         decimal? resultValue = null,
                                                         string? notes = null)
        {
            // Obtener nuevo ID
            int newId = await helper.GetNextIdAsync("TestResults");

            // Validar automáticamente si hay valor
            bool isAbnormal = false;
            if (resultValue.HasValue)
            {
                var order = await this.context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    var validation = await helper.ValidateResultAsync(testId, order.PatientID, resultValue.Value);
                    isAbnormal = validation.IsAbnormal;
                }
            }

            TestResult testResult = new TestResult
            {
                ResultID = newId,
                OrderID = orderId,
                TestID = testId,
                ResultValue = resultValue,
                IsAbnormal = isAbnormal,
                Notes = notes
            };

            this.context.TestResults.Add(testResult);
            await this.context.SaveChangesAsync();

            return testResult;
        }
        //3.Actualizar resultado CON VALIDACIÓN AUTOMÁTICA
        public async Task<bool> UpdateTestResultAsync(int resultId, decimal resultValue, string? notes = null)
        {
            var testResult = await this.context.TestResults
                .Include(tr => tr.Order)
                .FirstOrDefaultAsync(tr => tr.ResultID == resultId);

            if (testResult == null)
                return false;

            // Validar automáticamente
            var validation = await helper.ValidateResultAsync(
                testResult.TestID,
                testResult.Order.PatientID,
                resultValue
            );

            testResult.ResultValue = resultValue;
            testResult.IsAbnormal = validation.IsAbnormal;
            testResult.Notes = notes;

            await this.context.SaveChangesAsync();
            return true;
        }
        //4.Eliminar resultado
        public async Task<(bool Success, string Message)> DeleteTestResultAsync(int resultId)
        {
            var testResult = await this.context.TestResults.FindAsync(resultId);
            if (testResult == null)
                return (false, "Resultado no encontrado.");

            this.context.TestResults.Remove(testResult);
            await this.context.SaveChangesAsync();

            return (true, "Resultado eliminado exitosamente.");
        }

        #endregion

#region MÉTODOS USANDO STORED PROCEDURES-----------------------------------------------------------------

        //---1.Obtener detalles completos de una orden usando SP_GetOrderDetails-----
        //Incluye: Paciente, Doctor, Resultados con validación, Tubos
        public async Task<List<OrderDetailDTO>> GetOrderDetailsAsync(int orderId)
        {
            var param = new SqlParameter("@OrderID", orderId);

            // Ejecutar stored procedure
            var sql = "EXEC SP_GetOrderDetails @OrderID";

            var results = await this.context.Database
                .SqlQueryRaw<OrderDetailDTO>(sql, param)
                .ToListAsync();

            return results;
        }

        //2.Obtener tubos necesarios para una orden usando SP_GetRequiredTubes
        //Agrupa automáticamente y evita duplicados
        public async Task<List<RequiredTubeDTO>> GetRequiredTubesAsync(int orderId)
        {
            var param = new SqlParameter("@OrderID", orderId);

            var sql = "EXEC SP_GetRequiredTubes @OrderID";

            var results = await this.context.Database
                .SqlQueryRaw<RequiredTubeDTO>(sql, param)
                .ToListAsync();

            return results;
        }

        //3.Obtener rango de referencia para un paciente y examen usando SP_GetReferenceRange
        public async Task<ReferenceRangeDTO?> GetReferenceRangeAsync(int patientId, int testId)
        {
            var paramPatient = new SqlParameter("@PatientID", patientId);
            var paramTest = new SqlParameter("@TestID", testId);

            var sql = "EXEC SP_GetReferenceRange @PatientID, @TestID";

            var results = await this.context.Database
                .SqlQueryRaw<ReferenceRangeDTO>(sql, paramPatient, paramTest)
                .ToListAsync();

            return results.FirstOrDefault();
        }

        #endregion
        #region MÉTODOS ESPECIALES

        //Verificar si todos los resultados de una orden tienen valores
        public async Task<bool> AreAllResultsCompleteAsync(int orderId)
        {
            var results = await this.context.TestResults
                .Where(tr => tr.OrderID == orderId)
                .ToListAsync();

            return results.Any() && results.All(r => r.ResultValue.HasValue);
        }

        //Obtener resultados anormales de una orden
        public async Task<List<TestResult>> GetAbnormalResultsAsync(int orderId)
        {
            return await this.context.TestResults
                .Include(tr => tr.LabTest)
                .Where(tr => tr.OrderID == orderId && tr.IsAbnormal)
                .OrderBy(tr => tr.LabTest.TestName)
                .ToListAsync();
        }

        //Contar resultados por estado
        public async Task<OrderResultsSummary> GetOrderSummaryAsync(int orderId)
        {
            var results = await this.context.TestResults
                .Where(tr => tr.OrderID == orderId)
                .ToListAsync();

            return new OrderResultsSummary
            {
                TotalResults = results.Count,
                CompletedResults = results.Count(r => r.ResultValue.HasValue),
                PendingResults = results.Count(r => !r.ResultValue.HasValue),
                AbnormalResults = results.Count(r => r.IsAbnormal),
                NormalResults = results.Count(r => !r.IsAbnormal && r.ResultValue.HasValue)
            };
        }

        #endregion
    }

    #region DTOs PARA STORED PROCEDURES
    //DTO para SP_GetOrderDetails
    //Mapea exactamente las columnas que devuelve el stored procedure
    public class OrderDetailDTO
    {
        public int OrderID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public int PatientID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public int PatientAge { get; set; }
        public string? PatientEmail { get; set; }
        public string? PhotoFilename { get; set; }
        public int DoctorID { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string? LicenseNumber { get; set; }
        public string? DoctorEmail { get; set; }
        public int ResultID { get; set; }
        public int TestID { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string? Units { get; set; }
        public decimal? ResultValue { get; set; }
        public bool IsAbnormal { get; set; }
        public string? Notes { get; set; }
        public string SampleName { get; set; } = string.Empty;
        public string? ContainerColor { get; set; }
        public decimal? MinVal { get; set; }
        public decimal? MaxVal { get; set; }
        public string ReferenceRange { get; set; } = string.Empty;
        public string ValidationStatus { get; set; } = string.Empty; // Normal, Anormal, Critico
    }

    //DTO para SP_GetRequiredTubes
    public class RequiredTubeDTO
    {
        public int SampleID { get; set; }
        public string SampleName { get; set; } = string.Empty;
        public string? ContainerColor { get; set; }
        public int TestCount { get; set; }
        public string TestNames { get; set; } = string.Empty;
    }

    //DTO para SP_GetReferenceRange
    public class ReferenceRangeDTO
    {
        public int? RangeID { get; set; }
        public int? TestID { get; set; }
        public string? Gender { get; set; }
        public int? MinAgeYear { get; set; }
        public int? MaxAgeYear { get; set; }
        public decimal? MinVal { get; set; }
        public decimal? MaxVal { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion

    #region CLASES AUXILIARES

    //Resumen de resultados de una orden
    public class OrderResultsSummary
    {
        public int TotalResults { get; set; }
        public int CompletedResults { get; set; }
        public int PendingResults { get; set; }
        public int AbnormalResults { get; set; }
        public int NormalResults { get; set; }

        public decimal CompletionPercentage =>
            TotalResults > 0 ? (decimal)CompletedResults / TotalResults * 100 : 0;

        public bool IsComplete => CompletedResults == TotalResults && TotalResults > 0;
    }

    #endregion




}
