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

        #endregion
    }
}
