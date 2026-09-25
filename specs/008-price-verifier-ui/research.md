# Research: Integracion operativa del verificador de precios

## Decision 1: Orquestador de sesion en Business

**Decision**: Crear `IPriceVerifierWorkflow` y `PriceVerifierWorkflow` como caso de uso de Business. Consumira `IConfigurationService`, `IConnectionTestService`, `ILicenseService`, `IProductService` e `IPriceFormatterService`, y devolvera resultados inmutables y seguros de Entities.

**Rationale**: La constitucion prohibe que la UI consulte Firebird o valide licencias directamente. Un facade evita inyectar cinco capacidades en el ViewModel, conserva reglas/orden en Business y proporciona una frontera unica facil de probar.

**Alternatives considered**: Coordinar desde el ViewModel (acoplamiento y secretos); agregar el servicio en Presentation (caso de uso mal ubicado); modificar servicios previos (contratos ya estables).

## Decision 2: Configuracion privada por sesion

**Decision**: El workflow conserva internamente la ultima `AppConfiguration` que completo la preparacion. Presentation solo recibe estado, mensajes seguros e informacion adicional permitida. `Invalidate` elimina la sesion al desactivar o ante fallo de acceso al catalogo.

**Rationale**: Las consultas existentes requieren configuracion, pero contiene password, host, rutas y licencia. Mantenerla detras de Business evita exponerla a bindings, logs o estado visual.

**Alternatives considered**: Devolver configuracion al ViewModel (privacidad); recargar por codigo (contradice aclaracion); token serializado de sesion (complejidad innecesaria).

## Decision 3: Preparacion secuencial con short-circuit

**Decision**: Ejecutar `LoadAsync`; exigir configuracion presente y `Validation.IsConnectionReady`; luego `TestAsync` con `Success`; finalmente `ValidateAsync(configuration.Licencia, configuration)` y exigir `IsValid`. Cada fallo detiene los pasos siguientes.

**Rationale**: Conexion y licencia dependen de configuracion, y licencia consulta el reloj Firebird. El orden evita trabajo invalido y atribuye el bloqueo sin filtrar detalles.

**Alternatives considered**: Paralelizar (dependencias y fallos ambiguos); aceptar `DefaultCreated` automaticamente (defaults no operativos); licencia antes de conexion (causa ambigua).

## Decision 4: Contratos de resultado seguros

**Decision**: Preparacion distingue Ready, ConfigurationUnavailable, ConnectionUnavailable y LicenseUnavailable. Lookup distingue Success, MissingInput, InputTooLong, NotFound y OperationalFailure; Product y precio solo aparecen en Success.

**Rationale**: Estados excluyentes eliminan excepciones esperadas de Presentation y evitan transportar configuracion o diagnostico tecnico.

**Alternatives considered**: Excepciones (flujo fragil); tuplas/banderas (combinaciones contradictorias); duplicar Product como strings (Entity existente suficiente).

## Decision 5: Lifecycle asincrono, cancelacion y epoch

**Decision**: El ViewModel implementa lifecycle asincrono. Cada activacion crea token y epoch; desactivar cancela e incrementa epoch. Workflow y ViewModel verifican vigencia antes de conservar sesion o publicar.

**Rationale**: El token reduce trabajo; el epoch bloquea continuaciones antiguas incluso si un proveedor no coopera con cancelacion.

**Alternatives considered**: Async desde constructor (`async void` y UI sin pintar); solo CancellationToken (insuficiente); revalidar por `Window.Activated` (Alt-Tab no es reactivacion de modulo).

## Decision 6: Comando async propio y single-flight

**Decision**: Agregar `AsyncRelayCommand` sin paquete externo, con `ExecuteAsync`, `CanExecute`, guardia atomica y manejo seguro. El ViewModel repite la guardia; confirmaciones busy se ignoran.

**Rationale**: `RelayCommand` es sincrono. La doble guardia garantiza una llamada por confirmacion sin cola ni cancelacion.

**Alternatives considered**: CommunityToolkit.Mvvm (dependencia innecesaria); `async void` directo (riesgo fatal); cola/latest-wins (contradice aclaracion).

## Decision 7: Estado del modulo reflejado por shell

**Decision**: PriceVerifierViewModel es fuente de conexion, licencia y mensaje. ShellViewModel reexpone y propaga cambios en lugar de mantener copias mutables.

**Rationale**: Un origen evita estados contradictorios y aprovecha que la shell ya conoce su modulo inicial.

**Alternatives considered**: Copias en Shell (divergencia); store global (excesivo hoy); indicadores duplicados (rompe composicion actual).

## Decision 8: Foco/seleccion como comportamiento visual

**Decision**: El ViewModel incrementa `FocusRequestVersion`; la View usa Dispatcher para enfocar y seleccionar todo solo cuando el campo esta habilitado. Enter enlaza al comando con actualizacion inmediata del texto.

**Rationale**: Foco es comportamiento WPF. La señal prueba intencion sin tipos visuales en ViewModel y soporta solicitudes repetidas.

**Alternatives considered**: Focus desde ViewModel (acoplamiento); limpiar codigo (contradice aclaracion); booleano de foco (no modela eventos repetidos).

## Decision 9: Accesibilidad y layout

**Decision**: Texto junto al color, progreso ocupado perceptible, propiedades de automatizacion, anuncio polite, tabulacion natural y foco visible. Validar 1280x720, maximizado y 125% DPI con smoke STA y recorrido manual.

**Rationale**: Cumple teclado/estado perceptible y evita aserciones pixel-perfect fragiles.

**Alternatives considered**: Solo snapshots/coordenadas (fragiles); solo manual (no protege bindings ni nombres accesibles).

## Decision 10: Suites existentes, sin dependencias nuevas

**Decision**: Probar workflow en BusinessRules y ViewModel/WPF/shell en Presentation. Usar `TaskCompletionSource` para busy/carreras. No crear proyecto, paquete UI automation ni codigo Data.

**Rationale**: Las suites ya tienen referencias/plataforma adecuadas y las integraciones Firebird previas siguen opt-in.

**Alternatives considered**: Proyecto nuevo (costo sin aislamiento adicional); UI Automation externa (innecesaria); Data nuevo (no hay proveedor nuevo).

## Decision 11: Logging sanitizado

**Decision**: Workflow y ViewModel registran solo etapa, estado/categoria y duracion; no adjuntan codigo, Product, configuracion, licencia ni excepciones potencialmente contaminadas.

**Rationale**: Un mensaje de excepcion puede contener datos del entorno. Las capas inferiores conservan sus diagnosticos seguros.

**Alternatives considered**: Excepcion completa en Presentation (riesgo de secretos); cero logging (viola observabilidad constitucional).
