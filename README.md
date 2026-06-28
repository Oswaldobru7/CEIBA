# EventosVivos

Sistema de reservas para eventos culturales, conferencias y talleres. La solucion implementa una API REST en .NET y una aplicacion Angular para gestionar eventos, reservas, confirmacion de pagos, cancelaciones y reportes de ocupacion.

## Arquitectura

La solucion usa una arquitectura por capas con separacion clara de responsabilidades:

- `EventosVivos.Domain`: entidades y enums del negocio.
- `EventosVivos.Application`: casos de uso, contratos, validaciones y reglas de negocio.
- `EventosVivos.Infrastructure`: almacenamiento en memoria y datos de referencia de venues.
- `EventosVivos.Api`: endpoints REST y configuracion HTTP.
- `EventosVivos.Tests`: runner de pruebas automatizadas sin dependencias externas.
- `frontend/eventos-vivos`: aplicacion Angular standalone.

Esta estructura mantiene las reglas de negocio fuera de la API y permite probarlas directamente. Para la prueba se usa almacenamiento en memoria porque el enunciado permite base de datos a eleccion, incluyendo memoria; esto simplifica la ejecucion local y concentra la evaluacion en las reglas de negocio. La infraestructura puede reemplazarse por EF Core con SQL Server, PostgreSQL o SQLite manteniendo los contratos de repositorio.

## Reglas implementadas

- RN-01: un evento no puede exceder la capacidad del venue.
- RN-02: eventos activos no pueden compartir venue con horarios superpuestos.
- RN-03: eventos de sabado/domingo no pueden iniciar despues de las 22:00.
- RN-04: no se permiten reservas si falta menos de 1 hora para el inicio.
- RN-05: eventos con precio mayor a 100 limitan a 10 entradas por transaccion.
- RF-03 especial: si el evento inicia en menos de 24 horas, el maximo es 5 entradas y esta regla prevalece sobre RN-05.
- RN-06: los eventos activos se marcan como completados cuando la fecha actual supera la hora de fin.
- RN-07: cancelar una reserva confirmada con menos de 48 horas marca las entradas como perdidas y no las libera.

## Requisitos

- .NET SDK 10
- Node.js 24 o compatible
- npm

## Ejecutar backend

```bash
dotnet run --project src/EventosVivos.Api
```

La API queda disponible por defecto en `http://localhost:5000` o en el puerto indicado por la salida de `dotnet run`.

Endpoints principales:

- `GET /api/venues`
- `POST /api/events`
- `GET /api/events?type=&from=&to=&venueId=&status=&search=`
- `POST /api/reservations`
- `POST /api/reservations/{id}/confirm-payment`
- `POST /api/reservations/{id}/cancel`
- `GET /api/events/{id}/occupancy-report`

## Ejecutar frontend

```bash
cd frontend/eventos-vivos
npm install
npm start
```

La aplicacion Angular consume la API en `http://localhost:5000/api`. Si la API usa otro puerto, actualizar `frontend/eventos-vivos/src/environments/environment.ts`.

## Ejecutar pruebas

```bash
dotnet run --project tests/EventosVivos.Tests
```

Las pruebas cubren los casos criticos de negocio: capacidad de venue, superposicion de horarios, restriccion nocturna de fin de semana, reserva tardia, limites de tickets, confirmacion de pago, cancelacion con penalizacion y reporte de ocupacion.

## Ejemplo de creacion de evento

```json
{
  "title": "Conferencia IA",
  "description": "Evento cultural con contenido suficiente",
  "venueId": 1,
  "maxCapacity": 100,
  "startAt": "2026-07-01T18:00:00Z",
  "endAt": "2026-07-01T20:00:00Z",
  "price": 80,
  "type": "conferencia"
}
```

## Decisiones tecnicas

- API minimal de ASP.NET Core para exponer endpoints REST sin sobrecargar la prueba con infraestructura accidental.
- Validacion en backend como fuente de verdad; Angular solo mejora la experiencia de usuario.
- Repositorios por interfaz para aislar casos de uso y facilitar sustitucion por persistencia real.
- `ProblemDetails` para errores HTTP consistentes.
- CORS limitado a `http://localhost:4200`.
- Tests automatizados sin paquetes externos para garantizar ejecucion aun si NuGet no esta disponible.

## Mejoras futuras

- Persistencia con EF Core y SQLite/PostgreSQL.
- Autenticacion y autorizacion para separar administrador y comprador.
- Control de concurrencia con transacciones o `RowVersion` para evitar sobreventa bajo carga real.
- Swagger/OpenAPI, Docker Compose y despliegue en nube.
