using BioLIS.Models;
using BioLIS.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BioLIS.Repositories
{
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

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await this.context.Orders
                .Include(o => o.Patient)
                .Include(o => o.Doctor)
                .Where(o => o.IsActive) // Filtro de activos
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await this.context.Orders
                .Include(o => o.Patient)
                .Include(o => o.Doctor)
                .Include(o => o.ApprovedByUser)
                    .ThenInclude(u => u.Doctor)
                .Include(o => o.TestResults)
                    .ThenInclude(tr => tr.LabTest)
                        .ThenInclude(lt => lt.SampleType)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.IsActive);
        }

        public async Task<Order> CreateOrderAsync(int patientId, int doctorId, int? currentUserId = null)
        {
            int newId = await helper.GetNextIdAsync("Orders");
            string orderNumber = await helper.GenerateOrderNumberAsync();

            Order order = new Order
            {
                OrderID = newId,
                PublicId = Guid.NewGuid(), // Genera GUID para Azure
                PatientID = patientId,
                DoctorID = doctorId,
                OrderDate = DateTime.Now,
                OrderNumber = orderNumber,
                Status = "Pendiente",
                IsActive = true,
                CreatedBy = currentUserId // Auditoría
            };

            this.context.Orders.Add(order);
            await this.context.SaveChangesAsync();

            return order;
        }

        public async Task<bool> UpdateOrderAsync(Order order)
        {
            var existing = await this.context.Orders.FirstOrDefaultAsync(o => o.OrderID == order.OrderID && o.IsActive);
            if (existing == null) return false;

            existing.PatientID = order.PatientID;
            existing.DoctorID = order.DoctorID;
            existing.OrderDate = order.OrderDate;
            existing.OrderNumber = order.OrderNumber;

            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> DeleteOrderAsync(int orderId)
        {
            var order = await this.context.Orders.FirstOrDefaultAsync(o => o.OrderID == orderId && o.IsActive);
            if (order == null) return (false, "Orden no encontrada.");

            // SOFT DELETE
            order.IsActive = false;
            await this.context.SaveChangesAsync();

            return (true, "Orden desactivada exitosamente (Soft Delete).");
        }

        public async Task<List<Order>> GetTodayOrdersAsync()
        {
            var today = DateTime.Today;
            return await this.context.Orders
                .Include(o => o.Patient)
                .Include(o => o.Doctor)
                .Where(o => o.OrderDate.Date == today && o.IsActive)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByPatientAsync(int patientId)
        {
            return await this.context.Orders
                .Include(o => o.Doctor)
                .Where(o => o.PatientID == patientId && o.IsActive)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByDoctorAsync(int doctorId)
        {
            return await this.context.Orders
                .Include(o => o.Patient)
                .Include(o => o.Doctor)
                .Where(o => o.DoctorID == doctorId && o.IsActive)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
        public async Task<List<TestResult>> GetPatientHistoryAsync(int patientId)
        {
            return await this.context.TestResults
                .Include(tr => tr.LabTest)
                .Include(tr => tr.Order)
                .Where(tr => tr.Order.PatientID == patientId &&
                             tr.ResultValue.HasValue &&
                             tr.Order.IsActive &&
                             tr.Order.Status == "Aprobada") // Solo resultados validados
                .OrderBy(tr => tr.Order.OrderDate)
                .ToListAsync();
        }

        #endregion

        #region GESTIÓN DE RESULTADOS (CRUD Y AUDITORÍA)

        public async Task<List<TestResult>> GetResultsByOrderAsync(int orderId)
        {
            return await this.context.TestResults
                .Include(tr => tr.LabTest)
                    .ThenInclude(lt => lt.SampleType)
                .Include(tr => tr.LabTest)
                    .ThenInclude(lt => lt.ReferenceRanges)
                .Include(tr => tr.EnteredByUser)
                .Include(tr => tr.ModifiedByUser)
                .Where(tr => tr.OrderID == orderId)
                .OrderBy(tr => tr.LabTest.TestName)
                .ToListAsync();
        }

        public async Task<TestResult?> GetTestResultByIdAsync(int resultId)
        {
            return await this.context.TestResults
                .Include(tr => tr.LabTest)
                    .ThenInclude(lt => lt.SampleType)
                .Include(tr => tr.Order)
                    .ThenInclude(o => o.Patient)
                .FirstOrDefaultAsync(tr => tr.ResultID == resultId);
        }

        public async Task<List<Order>> GetOrdersWithPendingResultsAsync()
        {
            return await this.context.Orders
                .Include(o => o.Patient)
                .Include(o => o.Doctor)
                .Where(o => o.IsActive && (o.Status == "Pendiente" || o.Status == "EnProceso"))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<TestResult> AddTestResultAsync(int orderId, int testId, decimal? resultValue = null, string? notes = null, int? userId = null)
        {
            int newId = await helper.GetNextIdAsync("TestResults");
            bool isAbnormal = false;
            string? alertLevel = null;

            if (resultValue.HasValue)
            {
                var order = await this.context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    var validation = await helper.ValidateResultAsync(testId, order.PatientID, resultValue.Value);
                    isAbnormal = validation.IsAbnormal;
                    alertLevel = validation.Status;
                }
            }

            TestResult testResult = new TestResult
            {
                ResultID = newId,
                OrderID = orderId,
                TestID = testId,
                ResultValue = resultValue,
                IsAbnormal = isAbnormal,
                AlertLevel = NormalizeAlertLevel(alertLevel),
                Notes = notes
            };

            if (resultValue.HasValue && userId.HasValue)
            {
                testResult.EnteredBy = userId.Value;
                testResult.EnteredDate = DateTime.Now;
            }

            this.context.TestResults.Add(testResult);
            await this.context.SaveChangesAsync();
            return testResult;
        }

        public async Task<bool> UpdateTestResultWithAuditAsync(int resultId, decimal resultValue, string? alertLevel, int currentUserId, string? notes = null)
        {
            var testResult = await this.context.TestResults.FindAsync(resultId);
            if (testResult == null) return false;

            bool hasPreviousValue = testResult.ResultValue.HasValue;
            decimal? previousValue = testResult.ResultValue;
            string? previousAlertLevel = testResult.AlertLevel;
            string previousNotes = testResult.Notes ?? string.Empty;

            if (!testResult.ResultValue.HasValue)
            {
                testResult.EnteredBy = currentUserId;
                testResult.EnteredDate = DateTime.Now;
            }

            string normalizedAlertLevel = NormalizeAlertLevel(alertLevel);
            testResult.IsAbnormal = (normalizedAlertLevel == AlertLevels.Anormal || normalizedAlertLevel == AlertLevels.Critico);
            testResult.ResultValue = resultValue;
            testResult.AlertLevel = normalizedAlertLevel;

            if (normalizedAlertLevel == AlertLevels.Critico && string.IsNullOrWhiteSpace(notes))
            {
                testResult.Notes = "Requiere atención médica inmediata";
            }
            else if (normalizedAlertLevel == AlertLevels.Critico && !string.IsNullOrWhiteSpace(notes) && !notes.Contains("atención médica"))
            {
                testResult.Notes = $"URGENTE: {notes}";
            }
            else
            {
                testResult.Notes = notes;
            }

            bool hasAnyChange = previousValue != resultValue || previousAlertLevel != normalizedAlertLevel || previousNotes != (testResult.Notes ?? string.Empty);

            if (hasPreviousValue && hasAnyChange)
            {
                testResult.ModifiedBy = currentUserId;
                testResult.ModifiedDate = DateTime.Now;
            }

            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> DeleteTestResultAsync(int resultId)
        {
            var testResult = await this.context.TestResults.FindAsync(resultId);
            if (testResult == null) return (false, "Resultado no encontrado.");

            this.context.TestResults.Remove(testResult);
            await this.context.SaveChangesAsync();
            return (true, "Resultado eliminado exitosamente.");
        }

        private string NormalizeAlertLevel(string? alertLevel)
        {
            if (string.IsNullOrWhiteSpace(alertLevel)) return AlertLevels.Normal;
            return alertLevel.ToUpper() switch
            {
                "NORMAL" => AlertLevels.Normal,
                "ANORMAL" => AlertLevels.Anormal,
                "CRITICO" or "CRÍTICO" => AlertLevels.Critico,
                "SIN RANGO" or "SIN_RANGO" or "SINRANGO" => AlertLevels.SinRango,
                _ => AlertLevels.Normal
            };
        }

        #endregion

        #region MÉTODOS USANDO STORED PROCEDURES

        public async Task<List<OrderDetailDTO>> GetOrderDetailsAsync(int orderId)
        {
            var param = new SqlParameter("@OrderID", orderId);
            var sql = "EXEC SP_GetOrderDetails @OrderID";
            return await this.context.Database.SqlQueryRaw<OrderDetailDTO>(sql, param).ToListAsync();
        }

        public async Task<List<RequiredTubeDTO>> GetRequiredTubesAsync(int orderId)
        {
            var param = new SqlParameter("@OrderID", orderId);
            var sql = "EXEC SP_GetRequiredTubes @OrderID";
            return await this.context.Database.SqlQueryRaw<RequiredTubeDTO>(sql, param).ToListAsync();
        }

        public async Task<ReferenceRangeDTO?> GetReferenceRangeAsync(int patientId, int testId)
        {
            var paramPatient = new SqlParameter("@PatientID", patientId);
            var paramTest = new SqlParameter("@TestID", testId);
            var sql = "EXEC SP_GetReferenceRange @PatientID, @TestID";

            var results = await this.context.Database.SqlQueryRaw<ReferenceRangeDTO>(sql, paramPatient, paramTest).ToListAsync();
            return results.FirstOrDefault();
        }

        #endregion

        #region MÉTODOS ESPECIALES

        public async Task<bool> AreAllResultsCompleteAsync(int orderId)
        {
            var results = await this.context.TestResults.Where(tr => tr.OrderID == orderId).ToListAsync();
            return results.Any() && results.All(r => r.ResultValue.HasValue);
        }

        public async Task<List<TestResult>> GetAbnormalResultsAsync(int orderId)
        {
            return await this.context.TestResults
                .Include(tr => tr.LabTest)
                .Where(tr => tr.OrderID == orderId && tr.IsAbnormal)
                .OrderBy(tr => tr.LabTest.TestName)
                .ToListAsync();
        }

        public async Task<OrderResultsSummary> GetOrderSummaryAsync(int orderId)
        {
            var results = await this.context.TestResults.Where(tr => tr.OrderID == orderId).ToListAsync();

            return new OrderResultsSummary
            {
                TotalResults = results.Count,
                CompletedResults = results.Count(r => r.ResultValue.HasValue),
                PendingResults = results.Count(r => !r.ResultValue.HasValue),
                AbnormalResults = results.Count(r => r.AlertLevel == AlertLevels.Anormal || r.AlertLevel == AlertLevels.Critico),
                NormalResults = results.Count(r => r.AlertLevel == AlertLevels.Normal && r.ResultValue.HasValue),
                CriticalResults = results.Count(r => r.AlertLevel == AlertLevels.Critico),
                NoRangeResults = results.Count(r => r.AlertLevel == AlertLevels.SinRango && r.ResultValue.HasValue)
            };
        }

        public async Task<(bool Success, string Message)> ChangeOrderStatusAsync(int orderId, string newStatus, int? approvedBy = null)
        {
            var order = await this.context.Orders.FirstOrDefaultAsync(o => o.OrderID == orderId && o.IsActive);
            if (order == null) return (false, "Orden no encontrada.");

            var targetStatus = NormalizeOrderStatus(newStatus);
            if (string.IsNullOrEmpty(targetStatus)) return (false, "Estado de destino inválido.");

            var currentStatus = string.IsNullOrWhiteSpace(order.Status) ? OrderStatus.Pendiente : order.Status;
            if (currentStatus == targetStatus) return (true, $"La orden ya estaba en estado '{targetStatus}'.");

            if (!IsValidStatusTransition(currentStatus, targetStatus))
                return (false, $"Transición inválida: {currentStatus} -> {targetStatus}. Flujo permitido: Pendiente -> EnProceso -> Completada -> Aprobada.");

            if (targetStatus == OrderStatus.Completada)
            {
                var totalResults = await this.context.TestResults.CountAsync(tr => tr.OrderID == orderId);
                var pendingResults = await this.context.TestResults.CountAsync(tr => tr.OrderID == orderId && !tr.ResultValue.HasValue);

                if (totalResults == 0) return (false, "La orden no tiene exámenes asociados para completar.");
                if (pendingResults > 0) return (false, $"No se puede marcar como completada. Faltan {pendingResults} resultado(s) por registrar.");
            }

            order.Status = targetStatus;

            if (targetStatus == OrderStatus.Completada) order.CompletedDate = DateTime.Now;
            if (targetStatus == OrderStatus.Aprobada && approvedBy.HasValue) order.ApprovedBy = approvedBy.Value;

            await this.context.SaveChangesAsync();
            return (true, $"Estado actualizado a '{targetStatus}'.");
        }

        private static string? NormalizeOrderStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return null;
            return status.Trim().ToUpperInvariant() switch
            {
                "PENDIENTE" => OrderStatus.Pendiente,
                "ENPROCESO" or "EN_PROCESO" or "EN PROCESO" => OrderStatus.EnProceso,
                "COMPLETADA" => OrderStatus.Completada,
                "APROBADA" => OrderStatus.Aprobada,
                _ => null
            };
        }

        private static bool IsValidStatusTransition(string currentStatus, string targetStatus)
        {
            return (currentStatus, targetStatus) switch
            {
                (OrderStatus.Pendiente, OrderStatus.EnProceso) => true,
                (OrderStatus.EnProceso, OrderStatus.Completada) => true,
                (OrderStatus.Completada, OrderStatus.Aprobada) => true,
                _ => false
            };
        }

        #endregion

        #region INTEGRACIÓN N8N

        public async Task CreateIntegrationEventAsync(string eventType, string payloadJson)
        {
            var integrationEvent = new IntegrationEvent
            {
                EventType = eventType,
                PayloadJSON = payloadJson,
                IsProcessed = false,
                CreatedAt = DateTime.Now
            };

            this.context.IntegrationEvents.Add(integrationEvent);
            await this.context.SaveChangesAsync();
        }


        //Creación del Token
        public async Task<OrderShareToken> CreateOrderShareTokenAsync(int orderId)
        {
            var shareToken = new OrderShareToken
            {
                TokenID = Guid.NewGuid(),
                OrderID = orderId,
                PinCode = new Random().Next(1000, 9999).ToString(), // PIN de 4 dígitos
                ExpiresAt = DateTime.Now.AddDays(30),
                DownloadsCount = 0,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            this.context.OrderShareTokens.Add(shareToken);
            await this.context.SaveChangesAsync();

            return shareToken;
        }
        #endregion
    }


    #region DTOs PARA STORED PROCEDURES
    public class OrderDetailDTO
    {
        public int OrderID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }

        // ¡NUEVO! Resumen IA y Teléfonos desde el SP
        public string? AISummary { get; set; }
        public string? PatientPhone { get; set; }
        public string? DoctorPhone { get; set; }

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
        public string ValidationStatus { get; set; } = string.Empty;
    }

    public class RequiredTubeDTO
    {
        public int SampleID { get; set; }
        public string SampleName { get; set; } = string.Empty;
        public string? ContainerColor { get; set; }
        public int TestCount { get; set; }
        public string TestNames { get; set; } = string.Empty;
    }

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
    public class OrderResultsSummary
    {
        public int TotalResults { get; set; }
        public int CompletedResults { get; set; }
        public int PendingResults { get; set; }
        public int AbnormalResults { get; set; }
        public int NormalResults { get; set; }
        public int CriticalResults { get; set; }
        public int NoRangeResults { get; set; }

        public decimal CompletionPercentage => TotalResults > 0 ? (decimal)CompletedResults / TotalResults * 100 : 0;
        public bool IsComplete => CompletedResults == TotalResults && TotalResults > 0;
    }
    #endregion
}