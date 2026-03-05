# Carpeta de Imágenes de Usuarios

Esta carpeta debe contener las fotos de perfil de los usuarios del sistema.

## Estructura

```
wwwroot/images/users/
??? default-user.png          # Imagen por defecto (obligatoria)
??? admin.png                 # Foto del admin (opcional)
??? [otros usuarios].png      # Fotos de otros usuarios
```

## Imagen por Defecto

**IMPORTANTE:** Debes colocar una imagen llamada `default-user.png` en esta carpeta.

Esta imagen se usará cuando:
- Un usuario no tenga foto de perfil asignada
- La foto del usuario no se encuentre
- Se cree un nuevo usuario sin especificar foto

## Recomendaciones

- **Formato:** PNG o JPG
- **Tamaño:** 300x300 píxeles (mínimo)
- **Peso:** Menor a 500 KB
- **Fondo:** Preferiblemente transparente (PNG)

## Ejemplo de Imagen por Defecto

Puedes usar:
- Avatar genérico de usuario
- Logo de la aplicación
- Silueta de persona
- Icono de usuario material design

## Subir Fotos de Usuario

Las fotos se pueden subir desde:
- El formulario de creación de usuarios (`/Users/Create`)
- Al actualizar el perfil de usuario

El sistema guardará automáticamente las fotos en esta carpeta.
