# Proyecto Reservas Temporales

Proyecto para gestionar alquileres temporarios de inmuebles de una inmobiliaria.

## Integrantes del Grupo

- Nahuel Vargas -
- Esteban Redon -
- Segura Luis -

## Base de Datos

La base de datos se llama `Lab2Inmobiliaria2026`.

El script esta en `database.sql`.

Para crear la base de datos desde MySQL/MariaDB:

```bash
mysql -u root -p < database.sql
```

## Instalacion y Ejecucion

Requisitos:

- .NET 8 SDK
- MySQL o MariaDB

Instalar/restaurar dependencias del proyecto:

```bash
dotnet restore
```

Compilar:

```bash
dotnet build
```

Correr la aplicacion:

```bash
dotnet run
```

Tambien se puede correr con recarga automatica durante desarrollo:

```bash
dotnet watch run
```

Nota: este proyecto es ASP.NET Core MVC, no usa `npm run dev`.

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

## Acceso, usuarios y perfil
Administrador:
Usuario: admi@gmail.com 
pass: administrador123


Empleados: 
Usuario:esteban@gmail.com
pass:esteban123

Usuario:luis@gmail.com 
pass:gabriel333

Usuario:nahu@gmail.com 
pass:nahuel1234


### Contraseñas, sesiones y avatar

- Las contraseñas se almacenan con el hash de `PasswordHasher` de ASP.NET Core; nunca en texto plano.
- El cambio de contraseña exige la actual y cierra todas las sesiones. El cambio administrativo de un usuario también invalida sus sesiones anteriores.
- Las operaciones de escritura usan protección antifalsificación. El login admite hasta 10 intentos por minuto y dirección IP.
- Las cookies son HTTP-only y requieren HTTPS fuera de desarrollo. En despliegues detrás de un proxy, configurar correctamente HTTPS antes de habilitar el acceso.
- El avatar acepta archivos PNG, JPEG o WebP de hasta 2 MB; se comprueba su firma y se asigna un nombre generado por el servidor. No se acepta SVG.
- Los avatares se guardan en `App_Data/avatares`, fuera de los archivos públicos. La aplicación necesita permiso de escritura allí. Esta carpeta no se versiona y debe preservarse al desplegar o hacer copias de seguridad.


## Para Mas Adelante

Queda pendiente para otra etapa:

- Imágenes adicionales de inmuebles
- Auditoría de reservas
- Conectar la seña inicial al formulario de creación de reservas
- Reportes avanzados
- Renovacion de reservas
- Finalizacion anticipada con multa

## Tecnologias

- ASP.NET Core MVC
- C#
- MySQL/MariaDB
