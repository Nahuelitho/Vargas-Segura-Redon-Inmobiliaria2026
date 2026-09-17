-- Migración aditiva: ejecutar sobre la base existente; no modifica los alquileres.
CREATE TABLE IF NOT EXISTS usuarios (
  id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  nombre VARCHAR(100) NOT NULL,
  apellido VARCHAR(100) NOT NULL,
  email VARCHAR(254) NOT NULL,
  password_hash VARCHAR(512) NOT NULL,
  rol VARCHAR(20) NOT NULL DEFAULT 'Empleado',
  avatar VARCHAR(100) DEFAULT NULL,
  sello_seguridad CHAR(32) NOT NULL,
  estado TINYINT(1) NOT NULL DEFAULT 1,
  UNIQUE KEY uq_usuarios_email (email),
  CONSTRAINT ck_usuarios_rol CHECK (rol IN ('Administrador', 'Empleado'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
