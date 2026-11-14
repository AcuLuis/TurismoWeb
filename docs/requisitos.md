# Requisitos funcionales

Requisitos originales del proyecto (antes en `requisitos.txt`) y requisitos complementarios que se derivan de ellos para que el sistema sea usable en un contexto real.

## Requisitos originales

| ID | Requisito |
|---|---|
| RQ-003 | Publicar actividades y excursiones por parte de las empresas de turismo. |
| RQ-004 | Explorar actividades y excursiones disponibles según destinos y fechas. |
| RQ-005 | Ver información detallada de cada actividad, incluyendo descripción, itinerario y reseñas de otros usuarios, además de completar la reserva de la actividad. |

## Requisitos complementarios

| ID | Requisito | Relacionado con |
|---|---|---|
| RQ-101 | Registro e inicio de sesión de turistas y empresas, con contraseñas protegidas mediante hash. | Todos |
| RQ-102 | Cada rol (Turista, Empresa, Administrador) accede solo a sus funciones. | Todos |
| RQ-103 | La empresa solo puede publicar si un administrador la ha verificado. | RQ-003 |
| RQ-104 | Cada actividad tiene cupos; el sistema impide reservar más personas que los cupos disponibles. | RQ-005 |
| RQ-105 | El turista puede consultar y cancelar sus reservas según la política de cancelación. | RQ-005 |
| RQ-106 | La empresa puede confirmar o rechazar las reservas recibidas y exportar la lista de participantes. | RQ-003 |
| RQ-107 | Solo un turista que completó la actividad puede reseñarla, una única vez. | RQ-005 |
| RQ-108 | El administrador puede moderar reseñas y suspender cuentas. | RQ-005 |
| RQ-109 | El catálogo es público: se puede explorar sin iniciar sesión. Para reservar sí se requiere sesión. | RQ-004 |

## Reglas de negocio

- Fechas de una actividad: la fecha de fin es igual o posterior a la de inicio, y el inicio no puede estar en el pasado al publicar.
- Precio mayor que 0, en soles (PEN). Capacidad entre 1 y 500 personas.
- Una reserva guarda el precio unitario y el total en el momento de reservar: los cambios de precio posteriores no la afectan.
- Una reserva puede cancelarse hasta 2 días antes del inicio (valor configurable).
- Estados de reserva: Pendiente, Confirmada, Rechazada y Cancelada. "Completada" se deriva de una reserva confirmada cuya actividad ya terminó.
- Valoración de una reseña: de 1 a 5 estrellas, con un comentario de entre 10 y 1000 caracteres.
