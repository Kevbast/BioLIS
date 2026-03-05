-- ============================================
-- Script: Actualizar Rol "Recepcion" a "Laboratorio"
-- Descripción: Actualiza todos los usuarios con rol "Recepcion" al nuevo rol "Laboratorio"
-- ============================================

USE [TU_BASE_DE_DATOS]; -- ?? CAMBIAR POR EL NOMBRE DE TU BASE DE DATOS
GO

-- Ver usuarios actuales con rol "Recepcion"
SELECT UserID, Username, Email, Role, DoctorID
FROM Users
WHERE Role = 'Recepcion';

-- Si encuentras usuarios, ejecuta el UPDATE:
UPDATE Users
SET Role = 'Laboratorio'
WHERE Role = 'Recepcion';

-- Verificar cambios
SELECT UserID, Username, Email, Role, DoctorID
FROM Users
WHERE Role = 'Laboratorio';

-- Resultado esperado:
-- Todos los usuarios que tenían rol "Recepcion" ahora tienen "Laboratorio"
