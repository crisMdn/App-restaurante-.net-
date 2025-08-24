CREATE TABLE clientes (
    id_cliente INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre TEXT NOT NULL,
    telefono TEXT NOT NULL,
    telefono_secundario TEXT NULL
);
CREATE TABLE sqlite_sequence(name,seq);
CREATE TABLE direcciones (
    id_direccion INTEGER PRIMARY KEY AUTOINCREMENT,
    id_cliente INTEGER NOT NULL,
    direccion TEXT NOT NULL,
    ciudad TEXT NULL,
    pais TEXT NULL,
    FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente)
);
CREATE TABLE historial_impresiones (
    id_impresion INTEGER PRIMARY KEY AUTOINCREMENT,
    id_direccion INTEGER NOT NULL,
    fecha_impresion DATETIME DEFAULT CURRENT_TIMESTAMP,
    usuario TEXT,
    FOREIGN KEY (id_direccion) REFERENCES direcciones(id_direccion)
);
CREATE TABLE cola_impresion (
    id_cola INTEGER PRIMARY KEY AUTOINCREMENT,
    id_direccion INTEGER NOT NULL,
    usuario TEXT,                                -- quién imprimió
    momento DATETIME DEFAULT CURRENT_TIMESTAMP,  -- cuándo se mandó a imprimir
    FOREIGN KEY (id_direccion) REFERENCES direcciones(id_direccion)
);
CREATE TRIGGER after_print_enqueue
AFTER INSERT ON cola_impresion
BEGIN
    INSERT INTO historial_impresiones (id_direccion, usuario, fecha_impresion)
    VALUES (NEW.id_direccion, NEW.usuario, COALESCE(NEW.momento, CURRENT_TIMESTAMP));

    -- Retención de 12 días
    DELETE FROM historial_impresiones
    WHERE fecha_impresion < datetime('now','-12 days');
END;
CREATE INDEX idx_clientes_tel ON clientes(telefono);
CREATE INDEX idx_clientes_tel2 ON clientes(telefono_secundario);
CREATE INDEX idx_dir_cliente ON direcciones(id_cliente);
CREATE TABLE reportes (
  id_reporte       INTEGER PRIMARY KEY AUTOINCREMENT,
  id_cliente       INTEGER NOT NULL,
  id_direccion     INTEGER NULL,
  estado           TEXT NOT NULL DEFAULT 'abierto',
  motivo           TEXT NULL,
  observacion      TEXT NULL,
  fecha_creacion   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  fecha_cierre     DATETIME NULL,
  FOREIGN KEY (id_cliente)   REFERENCES clientes(id_cliente),
  FOREIGN KEY (id_direccion) REFERENCES direcciones(id_direccion)
);
CREATE INDEX idx_reportes_fecha ON reportes(fecha_creacion);
CREATE INDEX idx_reportes_estado ON reportes(estado);
