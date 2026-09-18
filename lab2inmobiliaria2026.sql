-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Servidor: 127.0.0.1
-- Tiempo de generación: 18-09-2026 a las 21:19:02
-- Versión del servidor: 10.4.32-MariaDB
-- Versión de PHP: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Base de datos: `lab2inmobiliaria2026`
--

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `imagenes_inmueble`
--

CREATE TABLE `imagenes_inmueble` (
  `id` int(11) NOT NULL,
  `id_inmueble` int(11) NOT NULL,
  `url` varchar(255) NOT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `inmuebles`
--

CREATE TABLE `inmuebles` (
  `id` int(11) NOT NULL,
  `id_propietario` int(11) NOT NULL,
  `id_tipo` int(11) NOT NULL,
  `direccion` varchar(255) NOT NULL,
  `cupo` int(11) NOT NULL,
  `coordenadas` varchar(100) DEFAULT NULL,
  `precio_por_dia` decimal(10,2) NOT NULL,
  `porcentaje_reserva` decimal(5,2) NOT NULL DEFAULT 0.00,
  `imagen_portada` varchar(255) DEFAULT NULL,
  `disponible` tinyint(1) NOT NULL DEFAULT 1,
  `estado` tinyint(1) NOT NULL DEFAULT 1
) ;

--
-- Volcado de datos para la tabla `inmuebles`
--

INSERT INTO `inmuebles` (`id`, `id_propietario`, `id_tipo`, `direccion`, `cupo`, `coordenadas`, `precio_por_dia`, `porcentaje_reserva`, `imagen_portada`, `disponible`, `estado`) VALUES
(2, 1, 7, 'Urquiza 1100', 10, '123123', 10000.00, 15.00, NULL, 1, 1),
(3, 2, 20, 'Necochea 4555', 6, '5454668', 45000.00, 50.00, NULL, 1, 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `inquilinos`
--

CREATE TABLE `inquilinos` (
  `id` int(11) NOT NULL,
  `dni` varchar(20) NOT NULL,
  `nombre` varchar(100) NOT NULL,
  `apellido` varchar(100) NOT NULL,
  `telefono` varchar(30) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `direccion` varchar(255) DEFAULT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `inquilinos`
--

INSERT INTO `inquilinos` (`id`, `dni`, `nombre`, `apellido`, `telefono`, `email`, `direccion`, `estado`) VALUES
(1, '98798789', 'Morgan', 'Freeman', '999888777', 'free@gmail.com', 'Los Angeles', 1),
(2, '8369772', 'Pepe', 'Adebayor', '269778653', 'pepaso@gmail.com', 'Rio de Janeiro', 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `pagos`
--

CREATE TABLE `pagos` (
  `id` int(11) NOT NULL,
  `id_reserva` int(11) NOT NULL,
  `concepto` varchar(255) NOT NULL,
  `fecha_pago` date NOT NULL,
  `importe` decimal(10,2) NOT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1,
  `id_usuario_creador` int(11) DEFAULT NULL,
  `id_usuario_anulador` int(11) DEFAULT NULL,
  `es_sena` tinyint(1) NOT NULL DEFAULT 0
) ;

--
-- Volcado de datos para la tabla `pagos`
--

INSERT INTO `pagos` (`id`, `id_reserva`, `concepto`, `fecha_pago`, `importe`, `estado`, `id_usuario_creador`, `id_usuario_anulador`, `es_sena`) VALUES
(1, 2, 'anticipo', '2026-09-18', 40000.00, 1, 3, NULL, 0);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `propietarios`
--

CREATE TABLE `propietarios` (
  `id` int(11) NOT NULL,
  `dni` varchar(20) NOT NULL,
  `nombre` varchar(100) NOT NULL,
  `apellido` varchar(100) NOT NULL,
  `telefono` varchar(30) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `direccion` varchar(255) DEFAULT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `propietarios`
--

INSERT INTO `propietarios` (`id`, `dni`, `nombre`, `apellido`, `telefono`, `email`, `direccion`, `estado`) VALUES
(1, '37412834', 'Luis', 'Segura', '2612635975', 'luis.segu@gmail.com', 'Mendoza', 1),
(2, '1231123123', 'Marcelo', 'Gallardo', '123123123', 'gallardo@gmail.com', 'La matanza 112', 1),
(3, '444333555', 'Martin', 'Demichelis', '444534553535', 'demi@gmail.com', 'Panteras 123', 1),
(4, '3213123123', 'Gareth', 'Bale', '555444333', 'bale@gmail.com', 'Londres 123', 1),
(5, '21312321', '12312321', '12312321', '123123', '123123@gmail.com', '123123213', 1),
(6, '66654645', '66456456', '456456', '456456', '456456@gmail.com', '7567657', 0),
(8, '213123213', '123123', '3123', '31231', '123123@gmail.com', '123123', 1),
(9, 'sddd', 'xxasd', 'asad', 'a21312', 'qweqwe@gmail.com', '123123', 0);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `reservas`
--

CREATE TABLE `reservas` (
  `id` int(11) NOT NULL,
  `id_inquilino` int(11) NOT NULL,
  `id_inmueble` int(11) NOT NULL,
  `fecha_inicio` date NOT NULL,
  `fecha_fin` date NOT NULL,
  `monto_por_dia` decimal(10,2) NOT NULL,
  `fecha_terminacion` date DEFAULT NULL,
  `multa` decimal(10,2) DEFAULT NULL,
  `id_reserva_origen` int(11) DEFAULT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1
) ;

--
-- Volcado de datos para la tabla `reservas`
--

INSERT INTO `reservas` (`id`, `id_inquilino`, `id_inmueble`, `fecha_inicio`, `fecha_fin`, `monto_por_dia`, `fecha_terminacion`, `multa`, `id_reserva_origen`, `estado`) VALUES
(2, 1, 2, '2026-09-18', '2026-09-21', 90000.00, '2026-09-20', 90000.00, NULL, 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `tipos_inmueble`
--

CREATE TABLE `tipos_inmueble` (
  `id` int(11) NOT NULL,
  `descripcion` varchar(50) NOT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Volcado de datos para la tabla `tipos_inmueble`
--

INSERT INTO `tipos_inmueble` (`id`, `descripcion`, `estado`) VALUES
(1, 'Casa', 1),
(2, 'Departamento', 1),
(3, 'Monoambiente', 1),
(4, 'Loft', 1),
(7, 'Cabaña', 1),
(19, 'Loby', 0),
(20, 'Casa de Campo', 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `usuarios`
--

CREATE TABLE `usuarios` (
  `id` int(11) NOT NULL,
  `nombre` varchar(100) NOT NULL,
  `apellido` varchar(100) NOT NULL,
  `email` varchar(254) NOT NULL,
  `password_hash` varchar(512) NOT NULL,
  `rol` varchar(20) NOT NULL DEFAULT 'Empleado',
  `avatar` varchar(100) DEFAULT NULL,
  `sello_seguridad` char(32) NOT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1
) ;

--
-- Volcado de datos para la tabla `usuarios`
--

INSERT INTO `usuarios` (`id`, `nombre`, `apellido`, `email`, `password_hash`, `rol`, `avatar`, `sello_seguridad`, `estado`) VALUES
(1, 'Administrador', 'Inmobiliaria', 'admi@gmail.com', 'AQAAAAIAAYagAAAAEM9BSHi8/znJqAhQA6IVQeAwP2zyZhTC2PyNd0eY3by5YpW74WFxbZCo9eqpWoc4Rw==', 'Administrador', NULL, 'fc033b95ffee4fc185517780b041b1db', 1),
(2, 'Esteban', 'Redon', 'esteban@gmail.com', 'AQAAAAIAAYagAAAAEC8jlxCaRfDMAD4KRirv0ovfeqj6BtUgP1YiTETeaB2Yhh2VxwxLgqV2rzLbc7PpcQ==', 'Empleado', NULL, 'c782fa32164c44618aebf7f488e17087', 1),
(3, 'Luis', 'Segura', 'luis@gmail.com', 'AQAAAAIAAYagAAAAEDDfILN6bEHb9QsQgqhZ2h5ozSzfX6oa00S4K6dW0ps6PuodWmo6+9ZU2M9IXdVJcA==', 'Empleado', '7eb172a433154386a7aa42b958e7eb47.png', '0fbcf9d90cf4496b93c142465731a762', 1),
(4, 'Nahuel', 'Vargas', 'nahu@gmail.com', 'AQAAAAIAAYagAAAAEKikYQaNeEf19YpsbQJ2VLvdkM6CxtqdSGxW4EICr7nvZn877wRDhrFIYB9CzTubbw==', 'Empleado', NULL, '94e036e8043f4f859e2766a0c899a2c4', 1);

--
-- Índices para tablas volcadas
--

--
-- Indices de la tabla `imagenes_inmueble`
--
ALTER TABLE `imagenes_inmueble`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_imagenes_inmueble` (`id_inmueble`);

--
-- Indices de la tabla `inmuebles`
--
ALTER TABLE `inmuebles`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_inmuebles_propietario` (`id_propietario`),
  ADD KEY `ix_inmuebles_tipo` (`id_tipo`);

--
-- Indices de la tabla `inquilinos`
--
ALTER TABLE `inquilinos`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_inquilinos_dni` (`dni`);

--
-- Indices de la tabla `pagos`
--
ALTER TABLE `pagos`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_pagos_reserva` (`id_reserva`),
  ADD KEY `fk_pagos_creador` (`id_usuario_creador`),
  ADD KEY `fk_pagos_anulador` (`id_usuario_anulador`);

--
-- Indices de la tabla `propietarios`
--
ALTER TABLE `propietarios`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_propietarios_dni` (`dni`);

--
-- Indices de la tabla `reservas`
--
ALTER TABLE `reservas`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_reservas_inquilino` (`id_inquilino`),
  ADD KEY `ix_reservas_inmueble` (`id_inmueble`),
  ADD KEY `ix_reservas_fechas` (`fecha_inicio`,`fecha_fin`),
  ADD KEY `ix_reservas_origen` (`id_reserva_origen`);

--
-- Indices de la tabla `tipos_inmueble`
--
ALTER TABLE `tipos_inmueble`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_tipos_inmueble_descripcion` (`descripcion`);

--
-- Indices de la tabla `usuarios`
--
ALTER TABLE `usuarios`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_usuarios_email` (`email`);

--
-- AUTO_INCREMENT de las tablas volcadas
--

--
-- AUTO_INCREMENT de la tabla `imagenes_inmueble`
--
ALTER TABLE `imagenes_inmueble`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `inmuebles`
--
ALTER TABLE `inmuebles`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `inquilinos`
--
ALTER TABLE `inquilinos`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT de la tabla `pagos`
--
ALTER TABLE `pagos`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `propietarios`
--
ALTER TABLE `propietarios`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=10;

--
-- AUTO_INCREMENT de la tabla `reservas`
--
ALTER TABLE `reservas`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `tipos_inmueble`
--
ALTER TABLE `tipos_inmueble`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=21;

--
-- AUTO_INCREMENT de la tabla `usuarios`
--
ALTER TABLE `usuarios`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- Restricciones para tablas volcadas
--

--
-- Filtros para la tabla `imagenes_inmueble`
--
ALTER TABLE `imagenes_inmueble`
  ADD CONSTRAINT `fk_imagenes_inmuebles` FOREIGN KEY (`id_inmueble`) REFERENCES `inmuebles` (`id`);

--
-- Filtros para la tabla `inmuebles`
--
ALTER TABLE `inmuebles`
  ADD CONSTRAINT `fk_inmuebles_propietarios` FOREIGN KEY (`id_propietario`) REFERENCES `propietarios` (`id`),
  ADD CONSTRAINT `fk_inmuebles_tipos` FOREIGN KEY (`id_tipo`) REFERENCES `tipos_inmueble` (`id`);

--
-- Filtros para la tabla `pagos`
--
ALTER TABLE `pagos`
  ADD CONSTRAINT `fk_pagos_anulador` FOREIGN KEY (`id_usuario_anulador`) REFERENCES `usuarios` (`id`),
  ADD CONSTRAINT `fk_pagos_creador` FOREIGN KEY (`id_usuario_creador`) REFERENCES `usuarios` (`id`),
  ADD CONSTRAINT `fk_pagos_reservas` FOREIGN KEY (`id_reserva`) REFERENCES `reservas` (`id`);

--
-- Filtros para la tabla `reservas`
--
ALTER TABLE `reservas`
  ADD CONSTRAINT `fk_reservas_inmuebles` FOREIGN KEY (`id_inmueble`) REFERENCES `inmuebles` (`id`),
  ADD CONSTRAINT `fk_reservas_inquilinos` FOREIGN KEY (`id_inquilino`) REFERENCES `inquilinos` (`id`),
  ADD CONSTRAINT `fk_reservas_origen` FOREIGN KEY (`id_reserva_origen`) REFERENCES `reservas` (`id`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
