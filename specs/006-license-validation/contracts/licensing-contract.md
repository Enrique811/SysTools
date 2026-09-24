# Licensing Contract

## Public Business API

```text
ILicenseService.ValidateAsync(
    string? licenseInput,
    AppConfiguration configuration,
    CancellationToken cancellationToken = default)
    -> Task<LicenseValidationResult>
```

- `configuration` es obligatoria porque el reloj Firebird existente necesita datos de conexion; el servicio no la registra ni la devuelve.
- `licenseInput` puede ser JSON directo o ruta. Nulo/vacio produce `MissingInput`.
- Solo `Status == Valid` / `IsValid == true` autoriza operacion.
- Una cancelacion solicitada lanza `OperationCanceledException`; no devuelve un estado invalido.
- Cualquier otro fallo esperado se traduce a un estado y mensaje controlados.

## Infrastructure Contracts

```text
ILicenseSourceReader.ReadAsync(path, cancellationToken)
    -> Task<LicenseSourceReadResult>

IHardwareIdProvider.GetHardwareIdAsync(cancellationToken)
    -> Task<string?>

IServerClockRepository.GetCurrentAsync(configuration, cancellationToken)
    -> Task<DateTime>
```

- Las rutas se resuelven en Data. El limite por archivo es 64 KiB; archivos mayores producen `TooLarge`.
- El proveedor de UUID devuelve UUID canonico o `null`; nunca salida cruda del proceso.
- `IServerClockRepository` ya traduce Firebird a `RepositoryAccessException`; LicenseService lo convierte a `ServerTimeUnavailable` sin exponer detalles.

## License JSON

```json
{
  "uuid": "00112233-4455-6677-8899-AABBCCDDEEFF",
  "inicio": "2026-01-01 00:00:00",
  "fin": "2026-12-31 23:59:59",
  "firma": "<base64>"
}
```

- Raiz objeto; nombres exactos en minusculas.
- Los cuatro valores son strings no vacios y no duplicados.
- Se aceptan campos adicionales y cualquier orden.
- `uuid` se recorta y pasa a mayusculas; debe ser `8-4-4-4-12` hexadecimal.
- Fechas usan cultura invariante, formato exacto y calendario gregoriano.

## Signed Payload

```text
uuid={UUID_NORMALIZADO}
inicio={FECHA_INICIO} 00:00:00
fin={FECHA_FIN} 23:59:59
```

Los separadores son byte `0A`, no existe salto final y la codificacion es UTF-8. Se verifica `SHA256withRSA`, equivalente a SHA-256 + RSA PKCS#1 v1.5.

## Controlled Messages

| Status | Message |
|--------|---------|
| `Valid` | `La licencia es valida.` |
| `MissingInput` | `No se configuro la licencia.` |
| `SourceUnavailable` | `No fue posible leer el archivo de licencia.` |
| `InvalidJson` | `El contenido de la licencia no es JSON valido.` |
| `InvalidFields` | `La licencia contiene campos invalidos.` |
| `InvalidSignature` | `La firma de la licencia no es valida.` |
| `HardwareIdUnavailable` | `No fue posible identificar este equipo.` |
| `HardwareMismatch` | `La licencia no corresponde a este equipo.` |
| `NotYetValid` | `La licencia aun no ha iniciado.` |
| `Expired` | `La licencia esta vencida.` |
| `ServerTimeUnavailable` | `No fue posible comprobar la vigencia con el servidor.` |

No se anexan valores dinamicos a estos mensajes.

## Logging Contract

- Eventos permitidos: etapa (`Source`, `Parse`, `Signature`, `Hardware`, `ServerTime`), estado, emisor autenticado y nombre de tipo de fallo catalogado.
- Prohibido: licencia, JSON, firma, llaves, UUID completo/parcial, ruta, host, base, usuario, password, cadena de conexion, configuracion serializada, mensaje crudo o stack trace en templates propios del servicio.
- La excepcion puede enviarse al proveedor de logging como objeto tecnico solo para fallos inesperados si sus datos son previamente clasificados; los fallos de repositorio ya llegan traducidos y no se destructuran.
- La cancelacion no emite evento de error.

## Dependency and Scope Contract

- Entities no depende de criptografia, JSON, archivos, procesos, logging, WPF o Firebird.
- Business no referencia Data, WPF ni FirebirdSql.
- Data no decide validez; solo lee una fuente y obtiene UUID.
- Views/ViewModels no leen archivos, ejecutan procesos, verifican firmas ni consultan repositorios.
- Esta feature registra dependencias pero no cambia estados visuales ni bloquea comandos existentes.
