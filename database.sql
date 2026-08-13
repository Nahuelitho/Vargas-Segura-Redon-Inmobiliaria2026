CREATE DATABASE IF NOT EXISTS Web2Inmobiliaria2026
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_general_ci;

USE Web2Inmobiliaria2026;

CREATE TABLE propietarios (
  id INT NOT NULL AUTO_INCREMENT,
  dni VARCHAR(20) NOT NULL,
  nombre VARCHAR(100) NOT NULL,
  apellido VARCHAR(100) NOT NULL,
  telefono VARCHAR(30) DEFAULT NULL,
  email VARCHAR(100) DEFAULT NULL,
  direccion VARCHAR(255) DEFAULT NULL,
  estado TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  UNIQUE KEY uq_propietarios_dni (dni)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE tipos_inmueble (
  id INT NOT NULL AUTO_INCREMENT,
  descripcion VARCHAR(50) NOT NULL,
  estado TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  UNIQUE KEY uq_tipos_inmueble_descripcion (descripcion)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE inquilinos (
  id INT NOT NULL AUTO_INCREMENT,
  dni VARCHAR(20) NOT NULL,
  nombre VARCHAR(100) NOT NULL,
  apellido VARCHAR(100) NOT NULL,
  telefono VARCHAR(30) DEFAULT NULL,
  email VARCHAR(100) DEFAULT NULL,
  direccion VARCHAR(255) DEFAULT NULL,
  estado TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  UNIQUE KEY uq_inquilinos_dni (dni)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE inmuebles (
  id INT NOT NULL AUTO_INCREMENT,
  id_propietario INT NOT NULL,
  id_tipo INT NOT NULL,
  direccion VARCHAR(255) NOT NULL,
  cupo INT NOT NULL,
  coordenadas VARCHAR(100) DEFAULT NULL,
  precio_por_dia DECIMAL(10,2) NOT NULL,
  porcentaje_reserva DECIMAL(5,2) NOT NULL DEFAULT 0.00,
  imagen_portada VARCHAR(255) DEFAULT NULL,
  disponible TINYINT(1) NOT NULL DEFAULT 1,
  estado TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  KEY ix_inmuebles_propietario (id_propietario),
  KEY ix_inmuebles_tipo (id_tipo),
  CONSTRAINT fk_inmuebles_propietarios FOREIGN KEY (id_propietario) REFERENCES propietarios (id),
  CONSTRAINT fk_inmuebles_tipos FOREIGN KEY (id_tipo) REFERENCES tipos_inmueble (id),
  CONSTRAINT ck_inmuebles_cupo CHECK (cupo > 0),
  CONSTRAINT ck_inmuebles_precio CHECK (precio_por_dia >= 0),
  CONSTRAINT ck_inmuebles_porcentaje CHECK (porcentaje_reserva >= 0 AND porcentaje_reserva <= 100)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE imagenes_inmueble (
  id INT NOT NULL AUTO_INCREMENT,
  id_inmueble INT NOT NULL,
  url VARCHAR(255) NOT NULL,
  estado TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  KEY ix_imagenes_inmueble (id_inmueble),
  CONSTRAINT fk_imagenes_inmuebles FOREIGN KEY (id_inmueble) REFERENCES inmuebles (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE reservas (
  id INT NOT NULL AUTO_INCREMENT,
  id_inquilino INT NOT NULL,
  id_inmueble INT NOT NULL,
  fecha_inicio DATE NOT NULL,
  fecha_fin DATE NOT NULL,
  monto_por_dia DECIMAL(10,2) NOT NULL,
  fecha_terminacion DATE DEFAULT NULL,
  multa DECIMAL(10,2) DEFAULT NULL,
  id_reserva_origen INT DEFAULT NULL,
  estado TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  KEY ix_reservas_inquilino (id_inquilino),
  KEY ix_reservas_inmueble (id_inmueble),
  KEY ix_reservas_fechas (fecha_inicio, fecha_fin),
  KEY ix_reservas_origen (id_reserva_origen),
  CONSTRAINT fk_reservas_inquilinos FOREIGN KEY (id_inquilino) REFERENCES inquilinos (id),
  CONSTRAINT fk_reservas_inmuebles FOREIGN KEY (id_inmueble) REFERENCES inmuebles (id),
  CONSTRAINT fk_reservas_origen FOREIGN KEY (id_reserva_origen) REFERENCES reservas (id),
  CONSTRAINT ck_reservas_fechas CHECK (fecha_fin >= fecha_inicio),
  CONSTRAINT ck_reservas_monto CHECK (monto_por_dia >= 0),
  CONSTRAINT ck_reservas_multa CHECK (multa IS NULL OR multa >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE pagos (
  id INT NOT NULL AUTO_INCREMENT,
  id_reserva INT NOT NULL,
  concepto VARCHAR(255) NOT NULL,
  fecha_pago DATE NOT NULL,
  importe DECIMAL(10,2) NOT NULL,
  estado TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  KEY ix_pagos_reserva (id_reserva),
  CONSTRAINT fk_pagos_reservas FOREIGN KEY (id_reserva) REFERENCES reservas (id),
  CONSTRAINT ck_pagos_importe CHECK (importe >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO tipos_inmueble (descripcion) VALUES
  ('Casa'),
  ('Departamento'),
  ('Monoambiente'),
  ('Loft'),
  ('Cabana');
