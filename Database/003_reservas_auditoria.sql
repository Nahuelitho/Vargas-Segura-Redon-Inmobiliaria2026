ALTER TABLE reservas
    ADD COLUMN id_usuario_creador INT NULL,
    ADD COLUMN id_usuario_terminador INT NULL;

ALTER TABLE reservas
    ADD CONSTRAINT fk_reserva_usuario_creador
        FOREIGN KEY (id_usuario_creador) REFERENCES usuarios(id),
    ADD CONSTRAINT fk_reserva_usuario_terminador
        FOREIGN KEY (id_usuario_terminador) REFERENCES usuarios(id);

CREATE INDEX ix_reserva_usuario_creador
    ON reservas(id_usuario_creador);

CREATE INDEX ix_reserva_usuario_terminador
    ON reservas(id_usuario_terminador);