# ? Actualización de Repositorios - Cambios Mínimos

## ?? Resumen de Cambios

Se agregaron **solo 6 métodos pequeños** para aprovechar las nuevas propiedades de los modelos (Status, AlertLevel, auditoría).

---

## 1. **CatalogRepository.cs** - 4 métodos nuevos

### ? **GetOrdersAsync()**
```csharp
public async Task<List<Order>> GetOrdersAsync()
{
    return await this.context.Orders
        .Include(o => o.Patient)
        .Include(o => o.Doctor)
        .Include(o => o.ApprovedByUser) // ? Nueva relación FK
        .OrderByDescending(o => o.OrderDate)
        .ToListAsync();
}
```
**Uso:** Listar todas las órdenes con información completa.

---

### ? **GetOrdersByStatusAsync(string status)**
```csharp
public async Task<List<Order>> GetOrdersByStatusAsync(string status)
{
    return await this.context.Orders
        .Include(o => o.Patient)
        .Include(o => o.Doctor)
        .Include(o => o.ApprovedByUser)
        .Where(o => o.Status == status)
        .OrderByDescending(o => o.OrderDate)
        .ToListAsync();
}
```
**Uso:** Filtrar órdenes por estado (Pendiente, EnProceso, Completada, etc.).

**Ejemplo:**
```csharp
var pendientes = await catalogRepo.GetOrdersByStatusAsync(OrderStatus.Pendiente);
```

---

### ? **GetOrdersByDoctorAsync(int doctorId)**
```csharp
public async Task<List<Order>> GetOrdersByDoctorAsync(int doctorId)
{
    return await this.context.Orders
        .Include(o => o.Patient)
        .Include(o => o.Doctor)
        .Where(o => o.DoctorID == doctorId)
        .OrderByDescending(o => o.OrderDate)
        .ToListAsync();
}
```
**Uso:** Filtro para que un Doctor solo vea **sus** órdenes.

**Ejemplo:**
```csharp
// Si el usuario es Doctor
if (userRole == UserRoles.Doctor)
{
    var misOrdenes = await catalogRepo.GetOrdersByDoctorAsync(doctorId);
}
```

---

### ? **GetDoctorsWithoutUserAsync()**
```csharp
public async Task<List<Doctor>> GetDoctorsWithoutUserAsync()
{
    var doctorsWithUser = await this.context.Users
        .Where(u => u.DoctorID != null)
        .Select(u => u.DoctorID.Value)
        .ToListAsync();

    return await this.context.Doctors
        .Where(d => !doctorsWithUser.Contains(d.DoctorID))
        .OrderBy(d => d.FullName)
        .ToListAsync();
}
```
**Uso:** Obtener doctores que NO tienen usuario asignado (para el formulario de crear usuario con rol Doctor).

**Ejemplo:**
```csharp
// En UsersController/Create
if (selectedRole == UserRoles.Doctor)
{
    var availableDoctors = await catalogRepo.GetDoctorsWithoutUserAsync();
    ViewBag.AvailableDoctors = availableDoctors;
}
```

---

## 2. **OrderRepository.cs** - 2 métodos nuevos

### ? **ChangeOrderStatusAsync(int orderId, string newStatus, int? approvedBy)**
```csharp
public async Task<bool> ChangeOrderStatusAsync(int orderId, string newStatus, int? approvedBy = null)
{
    var order = await this.context.Orders.FindAsync(orderId);
    if (order == null)
        return false;

    order.Status = newStatus;

    if (newStatus == OrderStatus.Completada)
    {
        order.CompletedDate = DateTime.Now;
    }

    if (newStatus == OrderStatus.Aprobada && approvedBy.HasValue)
    {
        order.ApprovedBy = approvedBy.Value;
    }

    await this.context.SaveChangesAsync();
    return true;
}
```
**Uso:** Cambiar el estado de una orden automáticamente.

**Ejemplo:**
```csharp
// Cuando todos los resultados están ingresados
var allComplete = await orderRepo.AreAllResultsCompleteAsync(orderId);
if (allComplete)
{
    await orderRepo.ChangeOrderStatusAsync(orderId, OrderStatus.Completada);
}

// Cuando un Admin aprueba
await orderRepo.ChangeOrderStatusAsync(orderId, OrderStatus.Aprobada, currentUserId);
```

---

### ? **UpdateTestResultWithAuditAsync(...)**
```csharp
public async Task<bool> UpdateTestResultWithAuditAsync(
    int resultId, decimal resultValue, string? alertLevel, int modifiedBy, string? notes = null)
{
    var testResult = await this.context.TestResults.FindAsync(resultId);
    if (testResult == null)
        return false;

    testResult.ResultValue = resultValue;
    testResult.AlertLevel = alertLevel;
    testResult.Notes = notes;
    testResult.ModifiedBy = modifiedBy;
    testResult.ModifiedDate = DateTime.Now;

    await this.context.SaveChangesAsync();
    return true;
}
```
**Uso:** Actualizar un resultado guardando quién lo modificó y cuándo.

**Ejemplo:**
```csharp
await orderRepo.UpdateTestResultWithAuditAsync(
    resultId: 9001,
    resultValue: 16.5m,
    alertLevel: AlertLevels.Anormal,
    modifiedBy: currentUserId,
    notes: "Valor corregido"
);
```

---

## ?? Comparación: ANTES vs DESPUÉS

### **ANTES:**
```csharp
// CatalogRepository solo tenía:
- GetPatientsAsync()
- GetDoctorsAsync()
- GetLabTestsAsync()
- GetSampleTypesAsync()
- GetReferenceRangesAsync()

// OrderRepository solo tenía:
- GetAllOrdersAsync()
- CreateOrderAsync()
- UpdateOrderAsync()
- CreateTestResultAsync()
```

### **DESPUÉS:**
```csharp
// CatalogRepository ahora tiene:
? GetOrdersAsync() - Con Include de ApprovedByUser
? GetOrdersByStatusAsync() - Filtrar por Status
? GetOrdersByDoctorAsync() - Filtrar por DoctorID
? GetDoctorsWithoutUserAsync() - Para crear usuarios

// OrderRepository ahora tiene:
? ChangeOrderStatusAsync() - Cambiar Status automáticamente
? UpdateTestResultWithAuditAsync() - Guardar auditoría (ModifiedBy, ModifiedDate)
```

---

## ?? Casos de Uso Implementados

### **1. Dashboard con Filtros por Estado**
```csharp
// En OrdersController/Index
public async Task<IActionResult> Index(string? status)
{
    List<Order> orders;
    
    if (!string.IsNullOrEmpty(status))
    {
        orders = await catalogRepo.GetOrdersByStatusAsync(status);
    }
    else
    {
        orders = await catalogRepo.GetOrdersAsync();
    }
    
    ViewBag.Statuses = OrderStatus.GetAll();
    return View(orders);
}
```

---

### **2. Doctor Ve Solo Sus Órdenes**
```csharp
// En OrdersController/Index con filtro por rol
public async Task<IActionResult> Index()
{
    var userRole = HttpContext.Session.GetString("Role");
    var doctorId = HttpContext.Session.GetInt32("DoctorID");
    
    List<Order> orders;
    
    if (userRole == UserRoles.Doctor && doctorId.HasValue)
    {
        orders = await catalogRepo.GetOrdersByDoctorAsync(doctorId.Value);
    }
    else
    {
        orders = await catalogRepo.GetOrdersAsync();
    }
    
    return View(orders);
}
```

---

### **3. Crear Usuario para Doctor**
```csharp
// En UsersController/Create
[HttpGet]
public async Task<IActionResult> Create()
{
    ViewBag.Roles = UserRoles.GetAll();
    
    // Mostrar solo doctores SIN usuario
    var availableDoctors = await catalogRepo.GetDoctorsWithoutUserAsync();
    ViewBag.AvailableDoctors = availableDoctors;
    
    return View();
}
```

---

### **4. Cambiar Estado Automáticamente**
```csharp
// En ResultsController/IngresoResultados
[HttpPost]
public async Task<IActionResult> IngresarResultado(int orderId, int testId, decimal value)
{
    // Ingresar resultado
    await orderRepo.CreateTestResultAsync(orderId, testId, value, currentUserId);
    
    // Verificar si todos los resultados están completos
    var allComplete = await orderRepo.AreAllResultsCompleteAsync(orderId);
    
    if (allComplete)
    {
        // Cambiar estado a "Completada"
        await orderRepo.ChangeOrderStatusAsync(orderId, OrderStatus.Completada);
        TempData["Success"] = "Todos los resultados ingresados. Orden completada.";
    }
    
    return RedirectToAction("Details", "Orders", new { id = orderId });
}
```

---

### **5. Modificar Resultado con Auditoría**
```csharp
// En ResultsController/Edit
[HttpPost]
public async Task<IActionResult> Edit(int resultId, decimal newValue, string notes)
{
    var currentUserId = HttpContext.Session.GetInt32("UserID").Value;
    
    // Actualizar con auditoría
    await orderRepo.UpdateTestResultWithAuditAsync(
        resultId,
        newValue,
        AlertLevels.Anormal, // Calculado previamente
        currentUserId,
        notes
    );
    
    TempData["Success"] = "Resultado modificado. Auditoría guardada.";
    return RedirectToAction("Details", "Orders", new { id = orderId });
}
```

---

## ? Compilación

```
Build successful ?
```

---

## ?? Próximos Pasos Sugeridos

1. **Actualizar OrdersController** (usar los nuevos métodos)
   - Index con filtro por Status
   - Index con filtro por Doctor (si es Doctor)
   - Cambiar Status automáticamente

2. **Crear ResultsController** (ingresar/modificar resultados)
   - Usar UpdateTestResultWithAuditAsync
   - Cambiar Status a Completada si todos los resultados están ingresados

3. **Actualizar UsersController/Create**
   - Usar GetDoctorsWithoutUserAsync para el dropdown

---

**¡Repositorios actualizados con cambios mínimos! ??**

Los cambios son pequeños pero muy útiles para aprovechar las nuevas propiedades de Status, AlertLevel y auditoría.
