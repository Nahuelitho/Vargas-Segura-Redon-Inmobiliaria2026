# Proyecto Reservas Temporales

Proyecto para gestionar alquileres temporarios de inmuebles de una inmobiliaria.

## Integrantes del Grupo

- Nahuel Vargas -
- Esteban Redon -
- Segura Luis -

## Base de Datos

La base de datos se llama `Lab2Inmobiliaria2026`.

El script esta en `database.sql`.

```mermaid
erDiagram
    PROPIETARIOS ||--o{ INMUEBLES : posee
    TIPOS_INMUEBLE ||--o{ INMUEBLES : clasifica
    INMUEBLES ||--o{ IMAGENES_INMUEBLE : tiene
    INQUILINOS ||--o{ RESERVAS : realiza
    INMUEBLES ||--o{ RESERVAS : se_reserva_en
    RESERVAS ||--o{ PAGOS : registra
    RESERVAS ||--o{ RESERVAS : renueva

    PROPIETARIOS {
        int id PK
        varchar dni UK
        varchar nombre
        varchar apellido
        varchar telefono
        varchar email
        varchar direccion
        tinyint estado
    }

    TIPOS_INMUEBLE {
        int id PK
        varchar descripcion UK
        tinyint estado
    }

    INQUILINOS {
        int id PK
        varchar dni UK
        varchar nombre
        varchar apellido
        varchar telefono
        varchar email
        varchar direccion
        tinyint estado
    }

    INMUEBLES {
        int id PK
        int id_propietario FK
        int id_tipo FK
        varchar direccion
        int cupo
        varchar coordenadas
        decimal precio_por_dia
        decimal porcentaje_reserva
        varchar imagen_portada
        tinyint disponible
        tinyint estado
    }

    IMAGENES_INMUEBLE {
        int id PK
        int id_inmueble FK
        varchar url
        tinyint estado
    }

    RESERVAS {
        int id PK
        int id_inquilino FK
        int id_inmueble FK
        date fecha_inicio
        date fecha_fin
        decimal monto_por_dia
        date fecha_terminacion
        decimal multa
        int id_reserva_origen FK
        tinyint estado
    }

    PAGOS {
        int id PK
        int id_reserva FK
        varchar concepto
        date fecha_pago
        decimal importe
        tinyint estado
    }
```

## Primera Etapa

Por ahora vamos a trabajar con las entidades principales, sin login, roles ni auditoria.

Entidades:

- Propietarios
- Tipos de inmueble
- Inmuebles
- Imagenes de inmueble
- Inquilinos
- Reservas
- Pagos

## Para Mas Adelante

Queda pendiente para otra etapa:

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
