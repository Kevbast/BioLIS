# ? Actualización de Modelos - Auditoría y Estados

## ?? Cambios Realizados en los Modelos

### 1. **Order.cs** - Agregados Estados y Aprobación

#### **Nuevas Propiedades:**

```csharp
[Column("Status")]
public string Status { get; set; } = "Pendiente";

[Column("CompletedDate")]
public DateTime? CompletedDate { get; set; }

[Column("ApprovedBy")]
public int? ApprovedBy { get; set; }

[ForeignKey("ApprovedBy")]
public User? ApprovedByUser { get; set; }
```

#### **Nueva Clase Estática: OrderStatus**

```csharp
public static class OrderStatus
{
    public const string Pendiente = "Pendiente";
    public const string EnProceso = "EnProceso";
    public const string Completada = "Completada";
    public const string Aprobada = "Aprobada";
    public const string Entregada = "Entregada";

    public static List<string> GetAll() { ... }
    public static string GetDisplayName(string status) { ... }
}
```

---

### 2. **TestResult.cs** - Agregada Auditoría y Niveles de Alerta

#### **Nuevas Propiedades:**

```csharp
[Column("AlertLevel")]
public string? AlertLevel { get; set; }

[Column("EnteredBy")]
public int? EnteredBy { get; set; }

[Column("EnteredDate")]
public DateTime EnteredDate { get; set; } = DateTime.Now;

[Column("ModifiedBy")]
public int? ModifiedBy { get; set; }

[Column("ModifiedDate")]
public DateTime? ModifiedDate { get; set; }

// Navegación para auditoría
[ForeignKey("EnteredBy")]
public User? EnteredByUser { get; set; }

[ForeignKey("ModifiedBy")]
public User? ModifiedByUser { get; set; }
```

#### **Nueva Clase Estática: AlertLevels**

```csharp
public static class AlertLevels
{
    public const string Normal = "NORMAL";
    public const string Anormal = "ANORMAL";
    public const string Critico = "CRITICO";
    public const string SinRango = "SIN_RANGO";

    public static List<string> GetAll() { ... }
    public static string GetDisplayName(string? alertLevel) { ... }
    public static string GetBadgeClass(string? alertLevel) { ... }
}
```

---

## ?? **Uso de las Nuevas Propiedades**

### **1. Crear una Orden**

```csharp
var order = new Order
{
    OrderID = newId,
    PatientID = patientId,
    DoctorID = doctorId,
    OrderDate = DateTime.Now,
    OrderNumber = orderNumber,
    Status = OrderStatus.Pendiente, // ? Estado inicial
    CompletedDate = null,
    ApprovedBy = null
};
```

---

### **2. Cambiar Estado de Orden**

```csharp
// Cuando todos los resultados están ingresados
order.Status = OrderStatus.Completada;
order.CompletedDate = DateTime.Now;

// Cuando un supervisor aprueba
order.Status = OrderStatus.Aprobada;
order.ApprovedBy = currentUserId;
```

---

### **3. Ingresar Resultado con Auditoría**

```csharp
var result = new TestResult
{
    ResultID = newId,
    OrderID = orderId,
    TestID = testId,
    ResultValue = 15.5m,
    IsAbnormal = false,
    AlertLevel = AlertLevels.Normal, // ? Calculado por SP_ValidateResult
    EnteredBy = currentUserId,       // ? Quién ingresó
    EnteredDate = DateTime.Now,
    ModifiedBy = null,
    ModifiedDate = null
};
```

---

### **4. Modificar Resultado (Auditoría)**

```csharp
// Al modificar un resultado existente
result.ResultValue = 16.2m;
result.ModifiedBy = currentUserId; // ? Quién modificó
result.ModifiedDate = DateTime.Now;

// Recalcular AlertLevel con SP_ValidateResult
result.AlertLevel = await ValidateResultAsync(result);
```

---

## ?? **Flujo Completo de una Orden**

```
1. CREAR ORDEN
   ?? Status = "Pendiente"
   ?? CompletedDate = null
   ?? ApprovedBy = null

2. TOMAR MUESTRAS
   ?? Status = "EnProceso"

3. INGRESAR RESULTADOS
   ?? EnteredBy = UserID del técnico
   ?? EnteredDate = Ahora
   ?? AlertLevel = Calculado por SP_ValidateResult

4. TODOS LOS RESULTADOS INGRESADOS
   ?? Status = "Completada"
   ?? CompletedDate = Ahora

5. SUPERVISOR APRUEBA
   ?? Status = "Aprobada"
   ?? ApprovedBy = UserID del supervisor

6. PACIENTE RECIBE RESULTADOS
   ?? Status = "Entregada"
```

---

## ?? **Consultas Útiles**

### **Órdenes Pendientes**

```csharp
var pendientes = await context.Orders
    .Where(o => o.Status == OrderStatus.Pendiente)
    .Include(o => o.Patient)
    .Include(o => o.Doctor)
    .ToListAsync();
```

### **Resultados Críticos del Día**

```csharp
var criticos = await context.TestResults
    .Where(r => r.AlertLevel == AlertLevels.Critico && 
                r.EnteredDate >= DateTime.Today)
    .Include(r => r.Order.Patient)
    .Include(r => r.LabTest)
    .ToListAsync();
```

### **Órdenes por Doctor (con filtro de rol)**

```csharp
// Si es Doctor, solo ve sus órdenes
if (userRole == UserRoles.Doctor)
{
    var ordenes = await context.Orders
        .Where(o => o.DoctorID == doctorId)
        .Include(o => o.Patient)
        .ToListAsync();
}
// Admin y Laboratorio ven todas
else
{
    var ordenes = await context.Orders
        .Include(o => o.Patient)
        .Include(o => o.Doctor)
        .ToListAsync();
}
```

### **Auditoría: Resultados Modificados**

```csharp
var modificados = await context.TestResults
    .Where(r => r.ModifiedBy != null)
    .Include(r => r.EnteredByUser)
    .Include(r => r.ModifiedByUser)
    .Include(r => r.Order)
    .OrderByDescending(r => r.ModifiedDate)
    .ToListAsync();
```

---

## ?? **Uso en Vistas (Razor)**

### **Mostrar Estado con Badge**

```razor
<td>
    @if (order.Status == OrderStatus.Pendiente)
    {
        <span class="badge bg-warning">Pendiente</span>
    }
    else if (order.Status == OrderStatus.EnProceso)
    {
        <span class="badge bg-info">En Proceso</span>
    }
    else if (order.Status == OrderStatus.Completada)
    {
        <span class="badge bg-success">Completada</span>
    }
    else if (order.Status == OrderStatus.Aprobada)
    {
        <span class="badge bg-primary">Aprobada</span>
    }
    else
    {
        <span class="badge bg-secondary">Entregada</span>
    }
</td>
```

### **Mostrar AlertLevel con Clase**

```razor
<td>
    <span class="@AlertLevels.GetBadgeClass(result.AlertLevel)">
        @AlertLevels.GetDisplayName(result.AlertLevel)
    </span>
</td>
```

### **Mostrar Auditoría**

```razor
<small class="text-muted">
    Ingresado por: @result.EnteredByUser?.Username
    en @result.EnteredDate.ToString("dd/MM/yyyy HH:mm")
    
    @if (result.ModifiedBy != null)
    {
        <br />
        Modificado por: @result.ModifiedByUser?.Username
        en @result.ModifiedDate?.ToString("dd/MM/yyyy HH:mm")
    }
</small>
```

---

## ?? **Validaciones Importantes**

### **1. No Modificar Resultados Aprobados (a menos que seas Admin)**

```csharp
if (order.Status == OrderStatus.Aprobada && userRole != UserRoles.Admin)
{
    return BadRequest("No puedes modificar resultados de una orden aprobada");
}
```

### **2. Solo Imprimir si está Completada o Aprobada**

```csharp
if (order.Status == OrderStatus.Pendiente || order.Status == OrderStatus.EnProceso)
{
    return BadRequest("No puedes imprimir resultados incompletos");
}
```

### **3. Cambiar Estado Solo si Todos los Resultados Están Ingresados**

```csharp
var allResultsEntered = await context.TestResults
    .Where(r => r.OrderID == orderId)
    .AllAsync(r => r.ResultValue != null);

if (allResultsEntered)
{
    order.Status = OrderStatus.Completada;
    order.CompletedDate = DateTime.Now;
    await context.SaveChangesAsync();
}
```

---

## ? **Compilación**

```
Build successful ?
```

Los modelos están listos y compilando correctamente.

---

## ?? **Próximos Pasos**

1. ? Modelos actualizados
2. ? Actualizar Repositories para usar nuevas propiedades
3. ? Crear/Actualizar Controllers (DoctorsController, OrdersController)
4. ? Crear vistas con filtros por Status y AlertLevel
5. ? Implementar lógica de cambio automático de estado

---

**¡Modelos listos para usar! ??**
