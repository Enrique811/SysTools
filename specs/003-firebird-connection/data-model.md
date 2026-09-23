# Data Model: Conexión y prueba Firebird

## ConnectionTestStatus

Catálogo estable compartido por Entities. No contiene códigos ni tipos del proveedor.

| Value | Meaning |
|---|---|
| `Success` | La conexión abrió correctamente y el objeto se cerró/desechó. |
| `InvalidConfiguration` | Uno o más de los cuatro campos requeridos están vacíos o son whitespace. |
| `AuthenticationFailed` | Firebird devolvió un código de autenticación reconocido. |
| `ServerUnavailable` | La resolución, red o servidor falló con una señal reconocida. |
| `DatabaseUnavailable` | El servidor no pudo abrir la base o detectó formato/ODS no disponible. |
| `Timeout` | Venció el límite interno de 5 segundos sin cancelación explícita del consumidor. |
| `Canceled` | El consumidor canceló el intento. |
| `UnexpectedFailure` | El fallo no puede clasificarse de forma confiable. |

## ConnectionConfigurationIssue

Problema seguro de readiness encontrado antes de crear una conexión.

| Field | Type | Rules |
|---|---|---|
| Field | string | Uno de `ipEmpresa`, `rutaEmpresa`, `usuario`, `password`; nunca incluye el valor. |
| Message | string | Mensaje fijo y seguro; no copia configuración ni excepción. |

### Validation rules

- Se evalúan los cuatro campos en una sola pasada lógica.
- Un campo falla cuando `string.IsNullOrWhiteSpace` es verdadero.
- El orden es estable: `ipEmpresa`, `rutaEmpresa`, `usuario`, `password`.
- La validación no modifica el `AppConfiguration` recibido.
- Cualquier issue produce `InvalidConfiguration` y evita crear una conexión.

## ConnectionTestResult

Resultado inmutable que puede entregarse a Presentation sin filtrar infraestructura.

| Field | Type | Rules |
|---|---|---|
| Status | ConnectionTestStatus | Exactamente uno de los ocho estados. |
| Message | string | Texto controlado para operador; nunca es el mensaje crudo de una excepción. |
| Duration | TimeSpan | No negativa; mide desde antes de validar hasta obtener el estado final. |
| Issues | IReadOnlyList&lt;ConnectionConfigurationIssue&gt; | No nula; solo contiene elementos para `InvalidConfiguration`. |

### Invariants

- `Success` implica `Issues.Count == 0`.
- `InvalidConfiguration` implica uno o más issues y cero conexiones creadas.
- Los demás fallos implican `Issues.Count == 0`.
- Ningún resultado contiene `AppConfiguration`, password, licencia, connection string, `Exception`, `FbException` o `DbConnection`.
- `Message` proviene de un catálogo fijo por estado.

### Safe message catalogue

| Status | Message |
|---|---|
| `Success` | `La conexión con Firebird se estableció correctamente.` |
| `InvalidConfiguration` | `Completa los datos requeridos para conectar con Firebird.` |
| `AuthenticationFailed` | `Firebird rechazó las credenciales proporcionadas.` |
| `ServerUnavailable` | `No fue posible contactar al servidor Firebird.` |
| `DatabaseUnavailable` | `El servidor respondió, pero la base de datos no está disponible.` |
| `Timeout` | `La conexión excedió el tiempo permitido.` |
| `Canceled` | `La prueba de conexión fue cancelada.` |
| `UnexpectedFailure` | `No fue posible comprobar la conexión con Firebird.` |

## AppConfiguration usage

Entidad existente de la feature 002. Esta feature consume cuatro campos solo en memoria.

| Existing field | Connection mapping | Validation |
|---|---|---|
| IpEmpresa | `DataSource` | Requerido, no whitespace. |
| RutaEmpresa | `Database` | Requerido, no whitespace; ruta o alias remoto. |
| Usuario | `UserID` | Requerido, no whitespace. |
| Password | `Password` | Requerido, no whitespace; se preserva exactamente y no se registra. |

La prueba acepta una instancia recién editada o una cargada por `IConfigurationService`; no exige persistencia y nunca la modifica.

## Internal Data objects

### Firebird connection settings

Constantes internas de Data, no configurables en esta feature:

| Setting | Value |
|---|---|
| Port | `3050` |
| Charset | `ISO8859_1` |
| Dialect | `3` |
| Connection timeout | `5 seconds` |
| Pooling | `true` |

### Safe diagnostic

Data deriva un diagnóstico efímero para logging; no cruza al resultado público.

| Field | Type | Rules |
|---|---|---|
| Status | ConnectionTestStatus | Clasificación conservadora. |
| ExceptionType | string | Nombre del tipo, no `Exception.ToString()`. |
| ErrorCodes | IReadOnlyList&lt;int&gt; | Solo números obtenidos del proveedor; vacía si no existen. |

## State transitions

```text
Requested
  └─ Validating
      ├─ missing fields → InvalidConfiguration
      └─ valid
          └─ Opening
              ├─ opened → Disposing → Success
              ├─ caller canceled → Disposing → Canceled
              ├─ deadline expired → Disposing → Timeout
              ├─ known auth/network/database error → Disposing → classified failure
              └─ unknown error → Disposing → UnexpectedFailure
```

Reglas de transición:

- Todos los estados finales son terminales para una solicitud.
- Toda ruta que crea una conexión atraviesa `Disposing` exactamente una vez.
- Solicitudes concurrentes tienen máquinas de estado independientes.
- Si cancelación del consumidor y deadline coinciden, `Canceled` tiene precedencia.
