-- Migración aditiva e idempotente para MySQL/MariaDB. Ejecutar después de 001_usuarios.sql.
-- Los pagos anteriores conservan su estado; su autor desconocido queda en NULL.
CREATE TABLE IF NOT EXISTS pagos (
 id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
 id_reserva INT NOT NULL,
 concepto VARCHAR(255) NOT NULL,
 fecha_pago DATE NOT NULL,
 importe DECIMAL(10,2) NOT NULL,
 estado TINYINT(1) NOT NULL DEFAULT 1,
 KEY ix_pagos_reserva (id_reserva),
 CONSTRAINT fk_pagos_reservas FOREIGN KEY (id_reserva) REFERENCES reservas(id)
);

SET @pagos_ddl = IF(EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='pagos' AND COLUMN_NAME='id_usuario_creador'), 'SELECT 1', 'ALTER TABLE pagos ADD COLUMN id_usuario_creador INT NULL, ADD CONSTRAINT fk_pagos_creador FOREIGN KEY (id_usuario_creador) REFERENCES usuarios(id)');
PREPARE pagos_migracion FROM @pagos_ddl;
EXECUTE pagos_migracion;
DEALLOCATE PREPARE pagos_migracion;

SET @pagos_ddl = IF(EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='pagos' AND COLUMN_NAME='id_usuario_anulador'), 'SELECT 1', 'ALTER TABLE pagos ADD COLUMN id_usuario_anulador INT NULL, ADD CONSTRAINT fk_pagos_anulador FOREIGN KEY (id_usuario_anulador) REFERENCES usuarios(id)');
PREPARE pagos_migracion FROM @pagos_ddl;
EXECUTE pagos_migracion;
DEALLOCATE PREPARE pagos_migracion;

SET @pagos_ddl = IF(EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='pagos' AND COLUMN_NAME='es_sena'), 'SELECT 1', 'ALTER TABLE pagos ADD COLUMN es_sena TINYINT(1) NOT NULL DEFAULT 0');
PREPARE pagos_migracion FROM @pagos_ddl;
EXECUTE pagos_migracion;
DEALLOCATE PREPARE pagos_migracion;
