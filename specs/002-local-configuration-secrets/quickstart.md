# Quickstart: Validación de configuración local

## Purpose

Validar la feature de configuración sin modificar la configuración real del operador y sin requerir Firebird, licencia, impresora o UI adicional.

## Prerequisites

- Windows 11 x64.
- SDK .NET 10 seleccionado por `global.json`.
- Repositorio en la raíz de SysTools.
- Ningún secreto real en datos de prueba.

## 1. Restore and build

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln -c Debug --no-restore
```

Expected:

- Todos los proyectos compilan sin warnings.
- Data no introduce una referencia a WPF.
- Entities no depende de Business, Data ni Presentation.

## 2. Run the feature suite

```powershell
dotnet test tests/SysTools.Configuration.Tests/SysTools.Configuration.Tests.csproj -c Debug --no-build
```

Expected groups:

- Defaults and entity invariants.
- Complete validation and stable issue order.
- Service orchestration for missing, valid, invalid and failed storage.
- JSON round-trip for all eleven fields and Unicode.
- Real DPAPI round-trip for the current Windows user.
- Malformed JSON, invalid Base64 and protected data that cannot be opened.
- Atomic writer preserving the destination on injected failure.
- Concurrent in-process operations completing with valid JSON.
- Unknown JSON properties surviving a load/save cycle.
- Logging assertions showing event metadata but no password, ciphertext, license or full payload.

## 3. Run all regression tests

```powershell
dotnet test SysTools.sln -c Debug --no-build
```

Expected:

- Todas las pruebas de la feature y de Presentation pasan.
- Las pruebas arquitectónicas rechazan acceso a Data o archivos desde Views/ViewModels.
- La composición puede resolver `IConfigurationService` y la shell sin servicios externos.

## 4. Inspect a generated test document

Ejecutar la prueba de integración marcada para conservar temporalmente su directorio solo durante la inspección, usando una credencial ficticia única como `Systools-Test-Secret-9f4d`.

Validate:

- El archivo se llama `configuracion.json` y sigue el [contrato v1](contracts/configuration-contract.md#json-contract-v1).
- Contiene los once valores funcionales o su representación protegida y `schemaVersion`.
- No contiene el password ficticio.
- Decodificar `passwordProtegido` como Base64 no revela texto legible; solo el protector del mismo usuario recupera el valor.
- Acentos, rutas con espacios e `informacion` larga regresan sin cambios funcionales.
- El directorio temporal se elimina al finalizar la validación.

No copiar el archivo de prueba a `%APPDATA%` y no usar credenciales productivas.

## 5. Failure scenarios

Ejecutar los filtros dedicados:

```powershell
dotnet test tests/SysTools.Configuration.Tests/SysTools.Configuration.Tests.csproj -c Debug --no-build --filter "Category=Failure"
dotnet test tests/SysTools.Configuration.Tests/SysTools.Configuration.Tests.csproj -c Debug --no-build --filter "Category=Security"
```

Expected:

- JSON vacío, truncado o con tipos incorrectos devuelve `InvalidContent` y conserva el archivo.
- Un password protegido inválido devuelve `SecretUnavailable` sin valor parcial.
- Un fallo antes de replace conserva byte por byte el destino anterior.
- Una configuración con varios errores devuelve todos los issues en una sola evaluación.
- Los logs capturados no contienen los tokens centinela usados como password o licencia.

## 6. Performance check

Ejecutar la prueba Release que crea/carga defaults 100 veces en directorios aislados:

```powershell
dotnet test tests/SysTools.Configuration.Tests/SysTools.Configuration.Tests.csproj -c Release --filter "Category=Performance"
```

Expected:

- Al menos 95 de 100 operaciones completan en menos de 1 segundo.
- Cada resultado es `DefaultCreated` o `Loaded`, según la fase del ciclo.
- No quedan archivos temporales huérfanos.

## 7. Scope audit

```powershell
rg -n "FbConnection|FirebirdSql|ZXing|FastReport|PrintDialog|LicenseValidation|configuracion\.properties" src tests/SysTools.Configuration.Tests
```

Expected:

- No hay implementación de Firebird, barcode, reportes, impresión, licencia funcional o importación Java.
- Cualquier mención aparece solo en una prueba negativa o documentación de alcance.

## Completion evidence

Registrar después de implementar:

- Comandos y resultados de restore/build/test.
- Resultado de los 20 round-trips y 100 mediciones de defaults.
- Búsqueda del password centinela en archivo, temporales y logs.
- Evidencia de preservación del archivo en fallos.
- Resultado de auditoría de alcance y arquitectura.
