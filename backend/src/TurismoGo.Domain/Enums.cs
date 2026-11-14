namespace TurismoGo.Domain;

/// <summary>Rol de una cuenta. Determina a qué funciones accede.</summary>
public enum Rol
{
    Turista = 1,
    Empresa = 2,
    Admin = 3,
}

/// <summary>Estado de verificación de una empresa de turismo.</summary>
public enum EstadoEmpresa
{
    /// <summary>Registrada, a la espera de revisión. No puede publicar.</summary>
    Pendiente = 1,
    /// <summary>Verificada por un administrador. Puede publicar actividades.</summary>
    Verificada = 2,
    /// <summary>Suspendida. Sus actividades dejan de mostrarse.</summary>
    Suspendida = 3,
}

/// <summary>Estado de publicación de una actividad.</summary>
public enum EstadoActividad
{
    Borrador = 1,
    Publicada = 2,
    Cancelada = 3,
}

/// <summary>Estado de una reserva. "Completada" se deriva: confirmada y con la actividad ya finalizada.</summary>
public enum EstadoReserva
{
    Pendiente = 1,
    Confirmada = 2,
    Rechazada = 3,
    Cancelada = 4,
}
