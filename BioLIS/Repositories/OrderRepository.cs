using BioLab.Models;
using BioLIS.Data;
using Microsoft.EntityFrameworkCore;

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



        #endregion
    }
}
