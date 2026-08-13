# Reservas Temporales

Sistema ASP.NET Core MVC para gestionar reservas temporales de inmuebles de una agencia inmobiliaria.

## Alcance inicial

Primera etapa sin autenticacion, roles ni auditoria avanzada. El objetivo es avanzar con entidades basicas y funcionalidades simples.

Entidades iniciales:

- Propietarios
- Tipos de inmueble
- Inmuebles
- Imagenes de inmueble
- Inquilinos
- Reservas
- Pagos

## Base de datos

El script inicial esta en `database.sql`.

Incluye:

- Creacion de la base `Web2Inmobiliaria2026`
- Tablas principales
- Claves primarias y foraneas
- Indices basicos para busquedas
- Tipos de inmueble iniciales

## Reparto sugerido

Para no depender demasiado entre tareas, conviene empezar por entidades simples.

Integrante 1:

- Propietarios
- Tipos de inmueble

Integrante 2:

- Inquilinos
- Pagos

Integrante 3:

- Inmuebles
- Imagenes de inmueble

Reservas conviene dejarlas para despues de tener listas las entidades anteriores, porque dependen de inquilinos e inmuebles.

## Segunda etapa

Quedan para mas adelante:

- Usuarios
- Login
- Roles de administrador y empleado
- Auditoria de reservas y pagos
- Reportes avanzados
- Renovacion de reservas
- Finalizacion anticipada con multa

## Tecnologias

- ASP.NET Core MVC
- C#
- MySQL/MariaDB
