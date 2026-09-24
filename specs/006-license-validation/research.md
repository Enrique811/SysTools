# Research: Validacion de licencias heredadas

## Decision 1: Reproducir el payload firmado del Java byte por byte

**Decision**: Normalizar el UUID con `Trim()` y mayusculas; interpretar `inicio` y `fin` estrictamente; firmar/verificar exactamente `uuid={UUID}\ninicio={fecha inicial a 00:00:00}\nfin={fecha final a 23:59:59}` en UTF-8, con `LF` y sin salto final.

**Rationale**: `LicenseJsonValidator.buildSignedPayload` y el generador Java usan esa representacion. Cambiar espacios, saltos, casing, encoding u horas invalida licencias existentes.

**Alternatives considered**: Firmar el JSON serializado, conservar las horas recibidas o usar saltos de plataforma. Se rechazan porque rompen compatibilidad criptografica.

## Decision 2: Verificar ambas llaves X.509 heredadas, nunca llaves privadas

**Decision**: Importar las dos llaves publicas DER/Base64 con `RSA.ImportSubjectPublicKeyInfo`, verificar SHA-256 y padding PKCS#1 v1.5 en orden distribuidor/desarrollador y devolver un `LicenseIssuer` estable.

**Rationale**: Coincide con `SHA256withRSA` de Java y permite reconocer las licencias emitidas por ambos actores. Una llave publica no permite emitir licencias.

**Alternatives considered**: PSS, certificados, una sola llave o copiar el generador. No son compatibles o introducirian material privado prohibido.

## Decision 3: Parsing JSON estricto con compatibilidad de campos

**Decision**: Usar `System.Text.Json`; exigir objeto raiz y cuatro propiedades de texto no vacias, aceptar orden arbitrario y campos adicionales, rechazar JSON malformado, tipos distintos y propiedades requeridas duplicadas.

**Rationale**: Los documentos validos del Java siguen este contrato. Un parser real elimina aceptacion accidental de texto no JSON y las duplicidades ambiguas sin cambiar licencias validas.

**Alternatives considered**: Replicar la expresion regular Java o deserializar silenciosamente con valores por defecto. Ambas ocultan errores estructurales.

## Decision 4: Separar lectura de fuente, identidad y reloj

**Decision**: Business define `ILicenseSourceReader` e `IHardwareIdProvider` y reutiliza `IServerClockRepository`. Data implementa archivo y Windows; las pruebas usan dobles.

**Rationale**: Respeta inversion de dependencias, permite pruebas deterministas y evita I/O o Firebird en Presentation.

**Alternatives considered**: Leer archivos, ejecutar procesos y abrir Firebird dentro de `LicenseService`. Viola la arquitectura y dificulta fallos controlados.

## Decision 5: UUID Windows mediante comandos fijos con respaldo legado

**Decision**: Ejecutar primero PowerShell sin perfil con `Get-CimInstance Win32_ComputerSystemProduct` y, si no entrega un UUID canonico, intentar WMIC desde rutas Windows conocidas. No se concatena entrada del usuario; se limita tiempo y salida, y se termina el proceso al cancelar.

**Rationale**: Conserva la estrategia Java sin agregar una dependencia de plataforma. Los argumentos son constantes y la salida se valida antes de usarse.

**Alternatives considered**: `System.Management`, `MachineGuid` del registro o UUID aleatorio. El primero agrega paquete; `MachineGuid` no es el mismo identificador; uno aleatorio rompe licencias.

## Decision 6: Fecha Firebird como valor calendario sin conversion local

**Decision**: Comparar el `DateTime` entregado por `IServerClockRepository` directamente contra limites `DateTimeKind.Unspecified`, ambos inclusivos. No usar reloj local ni convertir zona horaria.

**Rationale**: Firebird `TIMESTAMP` no contiene offset y el Java compara valores calendario. Inventar una zona altera los bordes.

**Alternatives considered**: `DateTimeOffset`, UTC o `DateTime.Now`. No hay offset fuente y el reloj local es manipulable.

## Decision 7: Modelo de resultado seguro y de fallo cerrado

**Decision**: Un enum representa cada resultado final; solo `Valid` habilita operacion. El resultado expone emisor y fechas cuando son seguras, pero no UUID, firma, contenido, ruta, llave ni excepcion. Los errores se registran con evento/etapa y tipo tecnico, nunca payloads.

**Rationale**: Facilita decisiones sin depender de textos y mejora la filtracion del resumen Java, que mostraba UUID/ruta completos.

**Alternatives considered**: Muchos booleanos independientes o devolver excepciones. Permiten estados contradictorios o exponen detalles.

## Decision 8: Cortocircuitar etapas y preservar cancelacion

**Decision**: Validar fuente, JSON y firma antes de consultar UUID; validar UUID antes de consultar servidor; llamar cada fuente externa como maximo una vez. `OperationCanceledException` solicitada se propaga y no se registra como error.

**Rationale**: Reduce I/O, evita Firebird para licencias evidentemente invalidas y produce una causa determinista sin cambiar el requisito de fallo cerrado.

**Alternatives considered**: Ejecutar todas las etapas para un diagnostico completo. Hace trabajo innecesario y amplifica exposicion/fallos.

## Decision 9: Vectores sin material privado en el repositorio

**Decision**: Las pruebas guardan solo documentos y firmas de prueba o vectores ya firmados; nunca archivos de llave privada. Los tests de reglas pueden inyectar un verificador determinista, mientras los tests criptograficos validan RSA con llaves publicas/vectores fijos.

**Rationale**: Las llaves privadas son activos de emision y no pertenecen a la aplicacion ni al control de versiones.

**Alternatives considered**: Copiar `private.key` desde el generador o generar licencias en cada test con una llave productiva. Se rechaza por seguridad.
