# EventosVivos - Sistema de Reservas

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![Angular](https://img.shields.io/badge/Angular-20-DD0031?logo=angular)
![TypeScript](https://img.shields.io/badge/TypeScript-5.8-3178C6?logo=typescript)
![Arquitectura](https://img.shields.io/badge/Arquitectura-Capas-1B8A73)

Aplicacion full stack para gestionar eventos culturales, conferencias y talleres. Permite crear eventos, consultar disponibilidad, registrar reservas, confirmar pagos, cancelar reservas y revisar reportes de ocupacion.

---

## Descripcion del Proyecto

EventosVivos implementa una API REST en .NET y una aplicacion Angular para operar un flujo completo de reservas. El sistema permite:

- Consultar venues disponibles con su capacidad.
- Crear eventos por tipo, fecha, venue, precio y aforo maximo.
- Filtrar eventos por tipo, estado y busqueda por titulo.
- Crear reservas con datos del comprador y cantidad de entradas.
- Confirmar pagos y generar codigo de reserva.
- Cancelar reservas aplicando reglas de penalizacion.
- Consultar reportes de ocupacion con entradas vendidas, disponibles, perdidas e ingresos.

La persistencia actual es en memoria para facilitar la ejecucion local y centrar la prueba en reglas de negocio. La infraestructura esta aislada por contratos, por lo que puede reemplazarse por EF Core con SQL Server, PostgreSQL o SQLite sin cambiar los casos de uso.

---

## Prerrequisitos

| Herramienta | Version minima | Verificacion |
|-------------|----------------|--------------|
| .NET SDK | 10.x | `dotnet --version` |
| Node.js | 24.x o compatible | `node --version` |
| npm | 10.x o superior | `npm --version` |
| Angular CLI | 20.x | `ng version` |

Instalar Angular CLI globalmente si no lo tiene:

```bash
npm install -g @angular/cli@20
```

---

## Instalacion

### 1. Clonar el repositorio

```bash
git clone <url-del-repositorio>
cd G
```

### 2. Restaurar backend

```bash
cd Backend-ceiba
dotnet restore
```

### 3. Instalar frontend

```bash
cd ../Frontend-ceiba/eventos-vivos
npm install
```

---

## Como Ejecutar la Aplicacion

### Backend

Desde la raiz del repositorio:

```bash
dotnet run --project Backend-ceiba/src/EventosVivos.Api
```

La API queda disponible por defecto en `http://localhost:5127` o en el puerto indicado por la salida de `dotnet run`.

### Frontend

En otra terminal:

```bash
cd Frontend-ceiba/eventos-vivos
npm start
```

Abrir en el navegador: **[http://localhost:4200](http://localhost:4200)**

El frontend consume la API en `http://localhost:5127/api`. Si la API usa otro puerto, actualizar:

```text
Frontend-ceiba/eventos-vivos/src/environments/environment.ts
```

### Compilar frontend para produccion

```bash
cd Frontend-ceiba/eventos-vivos
npm run build
```

### Ejecutar pruebas de negocio

```bash
dotnet run --project Backend-ceiba/tests/EventosVivos.Tests
```

---

## Manual de Usuario

### Vista general

Al abrir la aplicacion vera:

- Encabezado principal con accion para actualizar eventos.
- Formulario para crear eventos.
- Panel de eventos con filtros por texto, tipo y estado.
- Flujo de reserva al seleccionar un evento.
- Panel de operacion para confirmar pago o cancelar reserva.
- Reporte de ocupacion con metricas clave.

### Crear un evento

1. Complete titulo, descripcion, venue, capacidad, fechas, precio y tipo.
2. Haga clic en **Crear**.
3. Si los datos cumplen las reglas de negocio, el evento aparece en la lista.
4. Si existe un error de validacion, la aplicacion muestra el mensaje retornado por la API.

### Consultar y filtrar eventos

1. Use el campo **Buscar titulo** para filtrar por texto.
2. Seleccione un **Tipo** o **Estado** si necesita acotar resultados.
3. Haga clic en **Filtrar**.
4. Seleccione un evento de la lista para operar reservas.

### Crear una reserva

1. Seleccione un evento disponible.
2. Ingrese cantidad de entradas, comprador y email.
3. Haga clic en **Reservar**.
4. La reserva queda en estado pendiente hasta confirmar el pago.

### Confirmar pago

1. Despues de crear una reserva, revise el panel **Operacion**.
2. Haga clic en **Confirmar pago**.
3. El sistema asigna un codigo de reserva y actualiza el estado.

### Cancelar reserva

1. Con una reserva creada o confirmada, haga clic en **Cancelar**.
2. Si la cancelacion ocurre con menos de 48 horas, las entradas se marcan como perdidas.
3. El panel informa cuantas entradas quedaron penalizadas.

### Consultar ocupacion

1. Seleccione un evento.
2. Haga clic en **Reporte**.
3. Revise entradas vendidas, disponibles, perdidas, porcentaje de ocupacion e ingresos.

---

## Solucion Tecnica de Cada Requisito

### RN-01 - Capacidad del venue

- **Implementacion:** `EventService` valida que el aforo maximo del evento no supere la capacidad del venue.
- **Resultado:** evita publicar eventos con capacidad imposible para el lugar seleccionado.

### RN-02 - Cruce de horarios por venue

- **Implementacion:** antes de crear un evento, el servicio consulta eventos activos del mismo venue y valida solapamientos.
- **Resultado:** un venue no puede tener dos eventos activos en el mismo horario.

### RN-03 - Restriccion nocturna de fines de semana

- **Implementacion:** eventos de sabado o domingo no pueden iniciar despues de las 22:00.
- **Resultado:** la regla se centraliza en backend y no depende del formulario Angular.

### RN-04 - Reserva tardia

- **Implementacion:** `ReservationService` rechaza reservas cuando falta menos de 1 hora para iniciar el evento.
- **Resultado:** se evita vender entradas demasiado cerca del inicio.

### RN-05 y RF-03 - Limites de entradas por transaccion

- **Implementacion:** eventos con precio mayor a 100 limitan la compra a 10 entradas; si el evento inicia en menos de 24 horas, el limite baja a 5 y prevalece.
- **Resultado:** las reglas comerciales se aplican de forma deterministica.

### RN-06 - Eventos completados

- **Implementacion:** los eventos activos se marcan como completados cuando la fecha actual supera la hora de fin.
- **Resultado:** la consulta refleja el estado real del evento.

### RN-07 - Cancelacion con penalizacion

- **Implementacion:** cancelar una reserva confirmada con menos de 48 horas registra entradas perdidas y no libera cupos.
- **Resultado:** el reporte de ocupacion conserva la informacion financiera y operativa.

---

## Arquitectura y Decisiones Tecnicas

### Backend por capas

```text
EventosVivos.Api
  -> EventosVivos.Application
      -> EventosVivos.Domain
  -> EventosVivos.Infrastructure
```

- `Domain`: entidades y enums del negocio.
- `Application`: casos de uso, contratos, validaciones y reglas.
- `Infrastructure`: repositorios en memoria, reloj del sistema y datos base.
- `Api`: endpoints HTTP, CORS, mapeo de errores y configuracion.
- `Tests`: runner de pruebas automatizadas sin dependencias externas.

### Frontend Angular standalone

El frontend usa Angular con componentes standalone, formularios reactivos y servicios HTTP. La pantalla principal centraliza el flujo operativo para mantener la prueba simple, verificable y rapida de ejecutar.

### Errores HTTP consistentes

La API usa `ProblemDetails` para devolver errores con formato uniforme. El frontend muestra esos mensajes en el panel de notificacion.

### Persistencia en memoria

La implementacion actual usa almacenamiento en memoria porque reduce configuracion local y permite validar reglas sin depender de una base de datos. Los repositorios ya estan abstraidos mediante interfaces.

---

## Estructura del Proyecto

```text
G/
├── Backend-ceiba/
│   ├── EventosVivos.slnx
│   ├── src/
│   │   ├── EventosVivos.Api/
│   │   ├── EventosVivos.Application/
│   │   ├── EventosVivos.Domain/
│   │   └── EventosVivos.Infrastructure/
│   └── tests/
│       └── EventosVivos.Tests/
├── Frontend-ceiba/
│   └── eventos-vivos/
│       ├── src/
│       │   ├── app/
│       │   ├── environments/
│       │   └── styles.css
│       ├── angular.json
│       └── package.json
└── README.md
```

---

## Endpoints Principales

| Metodo | Endpoint | Uso |
|--------|----------|-----|
| GET | `/api/venues` | Lista venues disponibles |
| POST | `/api/events` | Crea un evento |
| GET | `/api/events` | Consulta eventos con filtros |
| POST | `/api/reservations` | Crea una reserva |
| POST | `/api/reservations/{id}/confirm-payment` | Confirma pago |
| POST | `/api/reservations/{id}/cancel` | Cancela reserva |
| GET | `/api/events/{id}/occupancy-report` | Reporte de ocupacion |

---

## Ejemplo de Creacion de Evento

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

---

## Tecnologias Utilizadas

| Tecnologia | Version | Uso |
|------------|---------|-----|
| .NET | 10 | API REST y reglas de negocio |
| ASP.NET Core | 10 | Endpoints HTTP |
| C# | 14 / net10.0 | Backend |
| Angular | 20 | Frontend |
| TypeScript | 5.8 | Tipado del cliente |
| RxJS | 7.8 | Consumo reactivo de HTTP |
| CSS | Nativo | Estilos visuales |

---

## Mejoras Futuras

- Persistencia con EF Core y SQLite/PostgreSQL.
- Swagger/OpenAPI para documentar y probar endpoints.
- Autenticacion y autorizacion para separar administrador y comprador.
- Control de concurrencia con transacciones o `RowVersion` para evitar sobreventa.
- Docker Compose para levantar API y frontend con un solo comando.

---

## Licencia

Proyecto de uso academico y evaluacion tecnica.
