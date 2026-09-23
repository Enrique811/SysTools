# Research: Configuración local y secretos

## Decision 1: DPAPI por usuario para el password

**Decision**: Usar `System.Security.Cryptography.ProtectedData` 10.0.12 con `DataProtectionScope.CurrentUser`, sin entropía adicional. Guardar el resultado protegido como Base64 bajo `passwordProtegido`; el password en claro solo existe en el modelo en memoria durante la operación.

**Rationale**: `CurrentUser` vincula la recuperación al contexto del usuario de Windows y cumple la constitución sin administrar otra clave. El paquete oficial se apoya en DPAPI y solo está soportado en Windows, que es la plataforma objetivo. La versión 10.0.12 coincide con el patch train de las dependencias Microsoft ya presentes.

**Alternatives considered**:

- `LocalMachine`: descartado porque permitiría que otros usuarios/procesos del equipo recuperaran el secreto.
- Base64 del texto claro: descartado porque es codificación reversible y repite el defecto del legado.
- Windows Credential Manager: válido, pero amplía alcance y cambia la decisión constitucional de DPAPI para esta etapa.
- Entropía adicional fija: no aporta una barrera secreta real y agrega riesgo de incompatibilidad si cambia.

**Sources**:

- [DataProtectionScope enum](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.dataprotectionscope)
- [System.Security.Cryptography.ProtectedData package](https://www.nuget.org/packages/System.Security.Cryptography.ProtectedData/)

## Decision 2: JSON con modelo persistido separado

**Decision**: Usar `System.Text.Json` y un `StoredConfigurationDocument` privado de Data. El documento tendrá nombres explícitos en español, `schemaVersion: 1`, `passwordProtegido` y un contenedor de propiedades desconocidas para round-trip.

**Rationale**: Serializar la entidad de negocio expondría el password por accidente. Un modelo persistido separado hace visible la frontera de seguridad y permite evolucionar el esquema. `JsonExtensionData` conserva propiedades que una versión actual aún no conoce.

**Alternatives considered**:

- Serializar `AppConfiguration` directamente: descartado por riesgo de password en claro.
- Rechazar propiedades desconocidas: descartado porque impediría leer archivos creados por una versión posterior.
- Ignorar y perder propiedades desconocidas al guardar: descartado por riesgo de downgrade destructivo.
- Newtonsoft.Json: descartado porque la plataforma ya incluye las capacidades requeridas.

**Sources**:

- [System.Text.Json deserialization behavior](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/deserialization)
- [Handle overflow JSON with JsonExtensionData](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/handle-overflow)

## Decision 3: AppData roaming como única ubicación productiva

**Decision**: Resolver la ruta con `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)` y agregar `SysUtilerias\configuracion.json`. Si Windows no devuelve una ruta utilizable, retornar fallo controlado; no escribir junto al ejecutable ni bajo el perfil raíz.

**Rationale**: Coincide con `%APPDATA%`, el roadmap y la constitución. El API de carpetas especiales evita depender de una variable de entorno mutable y define ApplicationData como almacenamiento específico por usuario.

**Alternatives considered**:

- Leer directamente `%APPDATA%`: descartado porque puede no estar definido o ser manipulado.
- `LocalApplicationData`: descartado porque cambia la ruta aprobada.
- Directorio del ejecutable: descartado por permisos, despliegue y mezcla de binarios con datos.

**Sources**:

- [Environment.SpecialFolder](https://learn.microsoft.com/en-us/dotnet/api/system.environment.specialfolder)
- [Environment.GetFolderPath](https://learn.microsoft.com/en-us/dotnet/api/system.environment.getfolderpath?view=net-10.0)

## Decision 4: Sustitución de archivo sin truncamiento previo

**Decision**: Escribir el documento completo en un temporal único del mismo directorio, vaciarlo y sustituir el destino. Para un archivo existente usar `File.Replace`; para el primer guardado usar `File.Move` sin sobreescritura. Serializar operaciones por instancia y limpiar temporales fallidos.

**Rationale**: Nunca se abre el destino con truncamiento antes de tener un JSON completo. `File.Replace` sustituye el contenido de un archivo con otro y permite que el destino anterior permanezca intacto cuando la preparación o sustitución falla.

**Alternatives considered**:

- `File.WriteAllText` directo: descartado porque una interrupción puede dejar contenido parcial.
- Borrar y renombrar: descartado porque crea una ventana sin archivo válido.
- Base de datos local: descartada por complejidad desproporcionada para un solo documento.

**Sources**:

- [File.Replace](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace?view=net-10.0)

## Decision 5: Dos niveles de validez

**Decision**: Distinguir `IsPersistable` de `IsConnectionReady`. Valores de catálogo incorrectos o documento ilegible bloquean persistencia; host, ruta, usuario o password vacíos impiden la aptitud de conexión, pero permiten guardar/crear una configuración base.

**Rationale**: Resuelve la tensión entre crear defaults con datos de conexión vacíos y exigir esos datos antes de Firebird. La UI futura podrá mostrar todos los problemas sin confundir una estructura válida con una conexión preparada.

**Alternatives considered**:

- Una bandera `IsValid`: descartada porque haría inválido el primer archivo o comunicaría falsamente que está listo.
- Insertar `masterkey` por defecto: descartado por seguridad y porque promueve una credencial conocida.

## Decision 6: Resultados tipados y logging seguro

**Decision**: Los contratos devuelven estados y códigos tipados para ausencia, contenido inválido, secreto no disponible, acceso denegado, cancelación y fallo de almacenamiento. Las excepciones se conservan para logs técnicos sin payload y no cruzan hacia UI como texto.

**Rationale**: Los errores de configuración son recuperables y necesitan decisiones distintas. Los códigos estables facilitan pruebas y mensajes futuros sin filtrar rutas de empresa, licencia, password o ciphertext.

**Alternatives considered**:

- Lanzar excepciones hasta Presentation: descartado por la constitución y por acoplar UI a infraestructura.
- Retornar solo `null`: descartado porque no distingue primer uso de corrupción o permisos.

## Decision 7: Pruebas por feature con seams de infraestructura

**Decision**: Crear `SysTools.Configuration.Tests` con referencias a Entities, Business y Data. Probar reglas puras con unit tests; repositorio con directorios temporales y protectores controlados; DPAPI real en Windows; writer y protector reemplazables para provocar fallos deterministas.

**Rationale**: Evita cargar WPF, conserva una suite rápida y permite validar fallos que son difíciles de provocar mediante permisos reales. Las pruebas de composición siguen confirmando el registro desde Presentation.

**Alternatives considered**:

- Agregar todo a `SysTools.Presentation.Tests`: descartado porque oculta la propiedad de cada capa y obliga a cargar dependencias visuales.
- Probar solo con el sistema de archivos real: descartado por fragilidad y falta de escenarios deterministas.

## Decision 8: Sin migración automática del archivo Java

**Decision**: No leer `configuracion.properties` ni decodificar Base64 heredado. Se conserva equivalencia de campos, y la captura/importación visible queda para una feature posterior si el negocio la requiere.

**Rationale**: El roadmap pide eliminar la dependencia del Java y la especificación excluye importación automática. Leer el archivo antiguo introduciría reglas de precedencia, ubicaciones fallback y manejo de secretos inseguros fuera del alcance.

**Alternatives considered**:

- Importar una vez y renombrar el archivo: descartado hasta contar con una decisión explícita de UX y seguridad.
- Mantener lectura dual permanente: descartado porque perpetúa la dependencia.
