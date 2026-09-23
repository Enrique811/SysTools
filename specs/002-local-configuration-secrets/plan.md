# Implementation Plan: Configuración local y secretos

**Branch**: `002-local-configuration-secrets` | **Date**: 2026-09-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-local-configuration-secrets/spec.md`

## Summary

Incorporar una capacidad interna de configuración que cree, lea, valide y actualice `%APPDATA%\SysUtilerias\configuracion.json` sin depender del archivo Java. `AppConfiguration` y los resultados compartidos vivirán en Entities; Business definirá los contratos, las dos intensidades de validación y la coordinación; Data implementará JSON, reemplazo seguro de archivo y DPAPI con alcance `CurrentUser`. Presentation solo registrará las implementaciones en el composition root y no habilitará UI nueva.

El documento persistido separará `passwordProtegido` del password utilizable en memoria, incluirá una versión de esquema y tolerará propiedades futuras. Las escrituras se serializarán dentro del proceso y se harán mediante un temporal en el mismo directorio seguido de sustitución, de modo que un fallo no deje JSON parcial ni destruya el último archivo válido.

## Technical Context

**Language/Version**: C# 14 sobre .NET 10 (`global.json` fija 10.0.100 con roll-forward a la feature instalada; entorno actual 10.0.401)

**Primary Dependencies**: `System.Text.Json` incluido en .NET 10; `System.Security.Cryptography.ProtectedData` 10.0.12 para DPAPI; `Microsoft.Extensions.Logging.Abstractions` 10.0.12; DI y Serilog ya compuestos por Presentation

**Storage**: Un documento JSON por usuario en `%APPDATA%\SysUtilerias\configuracion.json`; temporal de escritura en el mismo directorio y sin base de datos

**Testing**: xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1 y pruebas con directorios temporales aislados; pruebas de integración DPAPI solo en Windows

**Target Platform**: Windows 11 x64; la aplicación y las pruebas que ejecutan DPAPI usan `net10.0-windows`

**Project Type**: Aplicación Windows Desktop WPF con proyectos separados Presentation, Business, Data y Entities

**Performance Goals**: Obtener configuración base en menos de 1 segundo en al menos 95 de 100 lecturas locales; cada operación procesa un solo documento pequeño y no bloquea el hilo de UI mediante I/O síncrono expuesto

**Constraints**: Offline; DPAPI `CurrentUser`; ningún secreto o configuración completa en logs; conservar el archivo anterior ante fallos; operaciones cancelables; sin conexión Firebird, UI de configuración, licencia funcional, impresoras ni reportes

**Scale/Scope**: Un perfil de configuración por usuario de Windows, 11 campos funcionales, una versión de esquema, una escritura activa a la vez por proceso y documentos esperados menores de 1 MB

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-design gate

| Principle / restriction | Evaluation | Evidence in this plan |
|---|---|---|
| 3 capas + Entities | PASS | Entities contiene datos/resultados; Business contratos y reglas; Data archivo/DPAPI; Presentation solo compone. |
| WPF + MVVM sin lógica en vistas | PASS | No se agrega UI y ninguna View/ViewModel accede al archivo o al protector. |
| Migración incremental Spec Kit | PASS | Es la etapa 1 posterior a la shell y excluye conexión y configuración visual. |
| Compatibilidad funcional y mejora del legado | PASS | Conserva los once campos, sustituye Base64 por protección real y evita estado global. |
| Seguridad, logging y configuración controlada | PASS | JSON en AppData, DPAPI, resultados controlados y logging sin payloads. |
| Validación proporcional al riesgo | PASS | Pruebas unitarias de reglas, integración de archivo/DPAPI, fallos inyectados, round-trip y auditoría de secretos. |
| Orden del primer sprint | PASS | Avanza de shell a configuración y deja Firebird como feature siguiente. |

No existen violaciones que requieran Complexity Tracking.

### Post-design gate

El modelo, el contrato y el quickstart mantienen las mismas fronteras. `IConfigurationRepository` se define en Business para invertir la dependencia; `JsonConfigurationRepository` y `DpapiSecretProtector` quedan en Data; los tipos persistidos no escapan de Data. La validación de aptitud para conexión no abre Firebird y la licencia permanece opaca. Todos los gates continúan en PASS.

## Project Structure

### Documentation (this feature)

```text
specs/002-local-configuration-secrets/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── configuration-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md                 # generado por speckit-tasks
```

### Source Code (repository root)

```text
src/
├── Entities/
│   └── Configuration/
│       ├── AppConfiguration.cs
│       ├── ConfigurationIssue.cs
│       ├── ConfigurationValidationResult.cs
│       └── ConfigurationOperationResults.cs
├── Business/
│   └── Configuration/
│       ├── IConfigurationRepository.cs
│       ├── IConfigurationService.cs
│       ├── ConfigurationValidator.cs
│       └── ConfigurationService.cs
├── Data/
│   └── Configuration/
│       ├── IConfigurationPathProvider.cs
│       ├── ISecretProtector.cs
│       ├── IAtomicFileWriter.cs
│       ├── AppDataConfigurationPathProvider.cs
│       ├── DpapiSecretProtector.cs
│       ├── AtomicFileWriter.cs
│       ├── StoredConfigurationDocument.cs
│       └── JsonConfigurationRepository.cs
└── Presentation/
    └── App.xaml.cs              # solo registros DI

tests/
├── SysTools.Configuration.Tests/
│   ├── Entities/
│   ├── Business/
│   ├── Data/
│   ├── Composition/
│   ├── Security/
│   └── TestDoubles/
└── SysTools.Presentation.Tests/
    └── Architecture/
        └── LayerDependencyTests.cs
```

**Structure Decision**: Se conservan los cuatro proyectos productivos existentes. Se agrega un proyecto de pruebas `net10.0-windows` centrado en la feature para probar las tres capas sin cargar WPF; la prueba arquitectónica existente se amplía para prohibir referencias de configuración desde Views/ViewModels hacia Data. Presentation registra las abstracciones y concretos solamente en `App.ConfigureServices`.

## Design Decisions

### Separación de configuración de dominio y documento persistido

- `AppConfiguration` contiene el password utilizable únicamente en memoria y no tiene atributos ni tipos de serialización.
- `StoredConfigurationDocument` pertenece a Data, usa nombres JSON explícitos y solo contiene `passwordProtegido`.
- El repositorio transforma entre ambos modelos; nunca serializa directamente `AppConfiguration`.
- El documento incluye `schemaVersion: 1` y captura propiedades desconocidas para devolverlas al archivo en una actualización, evitando pérdida accidental al convivir con versiones futuras.

### Contratos y resultados controlados

- `IConfigurationService` expone carga, validación y guardado asíncronos.
- `IConfigurationRepository` abstrae lectura/escritura y pertenece a Business; Data lo implementa.
- Las operaciones esperables no usan excepciones como contrato. Devuelven estados tipados y códigos seguros; excepciones de I/O, JSON o criptografía se registran dentro de la capa que tiene contexto técnico.
- `ConfigurationValidationResult` separa `IsPersistable` de `IsConnectionReady`: la configuración base vacía puede persistirse, pero no se anuncia lista para conectar.

### Protección del password

- `DpapiSecretProtector` usa `ProtectedData` con `DataProtectionScope.CurrentUser` y sin entropía adicional, evitando una segunda clave que también tendría que administrarse.
- El resultado binario se codifica en Base64 solamente como transporte del ciphertext dentro del JSON; Base64 nunca se usa sobre el password en claro.
- Bytes y buffers temporales controlados se limpian cuando sea viable. Ningún `ToString`, resultado o evento incluye el password o ciphertext.

### Escritura y concurrencia

- El repositorio usa un `SemaphoreSlim` por instancia para serializar lecturas/escrituras que pudieran competir dentro del proceso.
- Se serializa a UTF-8 en un archivo temporal único dentro del mismo directorio, se fuerza el vaciado y luego se usa `File.Move` para la primera creación o `File.Replace` para actualizar un destino existente.
- El temporal se elimina en `finally` si la sustitución no lo consumió. El archivo destino nunca se trunca antes de tener el documento completo.
- No se promete coordinación entre procesos en esta etapa; el alcance actual es una sola instancia visual. Un conflicto externo se traduce en fallo controlado y preserva el archivo observado.

### Integración y diagnóstico

- `App.ConfigureServices` registra servicio, validador, repositorio, path provider, protector y writer; no carga ni muestra la configuración durante el arranque.
- Business registra inicio/resultado de operaciones y validaciones por código; Data registra tipo de operación y excepción técnica. No se usa destructuring de `AppConfiguration` ni `StoredConfigurationDocument`.
- Las pruebas de composición resuelven `IConfigurationService`; las pruebas de shell comprueban que un resultado fallido no impide resolver o mantener la ventana.

## Complexity Tracking

No aplica: el diseño no introduce excepciones a la constitución ni proyectos productivos adicionales.
