
-- Usar la base de datos TurismoGo
USE TurismoGo;
GO

-- Crear la tabla de Usuarios
CREATE TABLE Usuarios (
    UsuarioID INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    Correo NVARCHAR(100) NOT NULL UNIQUE,
    Contraseña NVARCHAR(100) NOT NULL,
    TipoUsuario NVARCHAR(10) CHECK (TipoUsuario IN ('USU', 'ETU', 'ADM')) NOT NULL
);
GO

-- Crear la tabla de Actividades
CREATE TABLE Actividades (
    ActividadID INT PRIMARY KEY IDENTITY(1,1),
    EmpresaID INT FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(MAX),
    Itinerario NVARCHAR(MAX),
    Destino NVARCHAR(100),
    Fecha DATE,
    Disponibilidad INT
);
GO

-- Crear la tabla de Reservas
CREATE TABLE Reservas (
    ReservaID INT PRIMARY KEY IDENTITY(1,1),
    UsuarioID INT FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    ActividadID INT FOREIGN KEY REFERENCES Actividades(ActividadID),
    FechaReserva DATE,
    Estado NVARCHAR(50)
);
GO

-- Crear la tabla de Reseñas
CREATE TABLE Reseñas (
    ReseñaID INT PRIMARY KEY IDENTITY(1,1),
    UsuarioID INT FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    ActividadID INT FOREIGN KEY REFERENCES Actividades(ActividadID),
    Valoracion INT CHECK (Valoracion BETWEEN 1 AND 5),
    Comentario NVARCHAR(MAX),
    FechaReseña DATE
);
GO

-- Crear la tabla de Estadísticas
CREATE TABLE Estadisticas (
    EstadisticaID INT PRIMARY KEY IDENTITY(1,1),
    ActividadID INT FOREIGN KEY REFERENCES Actividades(ActividadID),
    TotalReservas INT,
    TotalValoraciones INT,
    ValoracionPromedio FLOAT
);
GO

-- Crear la tabla de Historial
CREATE TABLE Historial (
    HistorialID INT PRIMARY KEY IDENTITY(1,1),
    UsuarioID INT FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    ActividadID INT FOREIGN KEY REFERENCES Actividades(ActividadID),
    FechaActividad DATE
);
GO

-- Crear la tabla de ListadoUsuariosActividades
CREATE TABLE ListadoUsuariosActividades (
    ListadoID INT PRIMARY KEY IDENTITY(1,1),
    ActividadID INT FOREIGN KEY REFERENCES Actividades(ActividadID),
    UsuarioID INT FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    FechaSolicitud DATE
);
GO
