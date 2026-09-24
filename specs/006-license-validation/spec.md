# Feature Specification: Validacion de licencias heredadas

**Feature Branch**: `[006-license-validation]`

**Created**: 2026-09-23

**Status**: Draft

**Input**: User description: "Implementar validacion de licencia JSON firmada RSA, compatible con el sistema Java actual, ligada al UUID local de Windows y validada contra fecha/hora del servidor Firebird."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Validar una licencia Java existente (Priority: P1)

Un operador con una licencia emitida para el sistema Java puede usarla sin regenerarla y obtiene una decision unica de licencia valida o invalida.

**Why this priority**: Conservar las licencias existentes es el requisito central de la migracion; una diferencia en firma, fechas o UUID bloquearia instalaciones ya autorizadas.

**Independent Test**: Se valida un conjunto controlado de licencias firmadas por cada emisor heredado y se comprueba que el contenido canonico, la identidad del emisor y la decision coinciden con el validador Java.

**Acceptance Scenarios**:

1. **Given** una licencia firmada por cualquiera de los dos emisores heredados, para el UUID local y vigente en la fecha del servidor, **When** se valida, **Then** se acepta sin regenerarla y se identifica el emisor correcto.
2. **Given** una licencia cuyo contenido fue alterado despues de firmarse, **When** se valida, **Then** se rechaza por firma invalida.
3. **Given** fechas con horas distintas dentro del JSON, **When** se verifica la firma, **Then** el contenido firmado usa el inicio del primer dia y el final del ultimo dia igual que el sistema Java.

---

### User Story 2 - Cargar una licencia de forma segura (Priority: P2)

Un operador puede proporcionar la licencia como JSON directo o como referencia a un archivo `.lic`, y recibe un resultado controlado cuando la entrada no puede interpretarse.

**Why this priority**: El sistema heredado admite ambas fuentes y la configuracion actual conserva la licencia como texto opaco.

**Independent Test**: Se prueban JSON directo, archivo UTF-8, ruta relativa, ruta absoluta, archivo ausente, archivo inaccesible, JSON malformado, campos faltantes, fechas invalidas y firma no codificada correctamente sin usar Firebird real ni el UUID real del equipo.

**Acceptance Scenarios**:

1. **Given** un JSON de licencia completo, **When** se carga, **Then** se interpreta directamente sin tratarlo como ruta.
2. **Given** una ruta valida a un archivo UTF-8, **When** se carga, **Then** se interpreta su contenido y se conserva una descripcion segura de la fuente.
3. **Given** entrada vacia, archivo ausente o contenido invalido, **When** se carga, **Then** se devuelve un estado especifico y un mensaje controlado sin excepcion cruda.

---

### User Story 3 - Comprobar equipo y vigencia con fuentes confiables (Priority: P2)

El sistema confirma que una licencia autentica pertenece al equipo Windows actual y que esta vigente segun el reloj del servidor, sin confiar en el reloj local.

**Why this priority**: La firma por si sola no evita copiar una licencia a otro equipo ni manipular la fecha local.

**Independent Test**: Con dobles de UUID y reloj de servidor se prueban coincidencia, UUID distinto, UUID no disponible, limite inicial, limite final, licencia aun no iniciada, vencida, fallo de Firebird y cancelacion.

**Acceptance Scenarios**:

1. **Given** una firma valida y UUID coincidente, **When** la fecha del servidor esta exactamente en cualquiera de los limites incluidos, **Then** la licencia esta vigente.
2. **Given** una firma valida para otro UUID, **When** se valida, **Then** se rechaza sin consultar el reloj del equipo local.
3. **Given** que no puede obtenerse el UUID local o la fecha del servidor, **When** se valida, **Then** la licencia no permite operar y el resultado distingue la causa sin filtrar detalles tecnicos.
4. **Given** una cancelacion solicitada durante la consulta del servidor, **When** se valida, **Then** la cancelacion se conserva para el consumidor y no se presenta como licencia invalida.

---

### User Story 4 - Diagnosticar sin exponer datos sensibles (Priority: P3)

Soporte puede distinguir la etapa que fallo y consultar eventos tecnicos seguros sin que mensajes o logs revelen el contenido de la licencia, la firma, UUID completos, rutas sensibles, credenciales o excepciones al operador.

**Why this priority**: Un diagnostico util reduce soporte, pero nunca debe convertir el licenciamiento en una fuente de filtraciones.

**Independent Test**: Se inyectan centinelas en licencia, firma, UUID, ruta y errores del origen y se auditan resultados, mensajes y eventos capturados.

**Acceptance Scenarios**:

1. **Given** cualquier fallo de lectura, estructura, firma, equipo o vigencia, **When** se crea el resultado visible, **Then** contiene un codigo y mensaje controlados sin payload ni excepcion.
2. **Given** un fallo tecnico, **When** se registra, **Then** el evento permite ubicar la etapa y conserva el diagnostico permitido sin registrar secretos ni identificadores completos.

### Edge Cases

- El UUID acepta diferencias de mayusculas y espacios exteriores, pero debe conservar el formato canonico de 36 caracteres con guiones.
- Los cuatro campos `uuid`, `inicio`, `fin` y `firma` son obligatorios, de texto y no vacios; campos adicionales no alteran el contenido firmado.
- Las fechas deben usar exactamente `yyyy-MM-dd HH:mm:ss`, ser fechas reales y tener inicio menor o igual que fin.
- La firma se verifica sobre UTF-8 y saltos de linea `LF`, sin salto final.
- El inicio firmado se canoniza a `00:00:00` y el fin a `23:59:59`, aunque el JSON contenga otras horas validas.
- Los limites de vigencia son inclusivos y se comparan en la semantica temporal ya entregada por Firebird, sin convertirlos mediante zona horaria local.
- Una firma valida de un emisor no debe probarse como perteneciente al otro emisor.
- Una ruta relativa se resuelve contra el directorio de trabajo; un JSON cuyo primer caracter no blanco es `{` se considera contenido directo.
- Una licencia no iniciada se distingue de una vencida y de una fecha de servidor no disponible.
- La cancelacion no se convierte en un resultado fallido ni se absorbe en logs.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST aceptar como entrada un JSON directo o una ruta absoluta o relativa a un archivo de licencia UTF-8.
- **FR-002**: La licencia MUST contener exactamente como datos requeridos `uuid`, `inicio`, `fin` y `firma`, todos como textos no vacios; campos adicionales MAY ignorarse.
- **FR-003**: `inicio` y `fin` MUST aceptar solo el formato estricto `yyyy-MM-dd HH:mm:ss`, MUST representar fechas reales y el inicio MUST ser menor o igual que el fin.
- **FR-004**: El UUID de licencia MUST normalizar espacios exteriores y mayusculas, MUST conservar guiones y MUST cumplir el formato canonico de UUID heredado.
- **FR-005**: El contenido firmado MUST componerse exactamente como `uuid={UUID}\ninicio={yyyy-MM-dd 00:00:00}\nfin={yyyy-MM-dd 23:59:59}`, codificado en UTF-8, sin salto final.
- **FR-006**: La firma MUST validarse con SHA-256 y RSA PKCS#1 v1.5 contra las dos llaves publicas heredadas, identificando de forma controlada si corresponde al distribuidor Colombia o al desarrollador.
- **FR-007**: Las licencias existentes aceptadas por el validador Java y firmadas por cualquiera de los emisores heredados MUST conservar el mismo resultado sin regeneracion.
- **FR-008**: La validacion MUST obtener el UUID local mediante una abstraccion de plataforma y MUST comparar el valor normalizado sin distinguir mayusculas.
- **FR-009**: La fuente Windows de UUID MUST intentar primero la consulta moderna de identidad del equipo y MAY usar la consulta heredada como respaldo; un valor ausente, malformado o un comando fallido MUST producir un fallo controlado.
- **FR-010**: La vigencia MUST evaluarse exclusivamente con la fecha/hora entregada por el repositorio de reloj del servidor existente y MUST NOT consultar el reloj local.
- **FR-011**: Los limites de inicio y fin MUST ser inclusivos; una fecha anterior MUST clasificarse como licencia no iniciada y una posterior como vencida.
- **FR-012**: Una licencia solo MUST considerarse valida cuando fuente, estructura, firma, UUID y vigencia sean validos en la misma operacion.
- **FR-013**: Los fallos MUST distinguir al menos entrada ausente, archivo no encontrado o ilegible, JSON invalido, campos invalidos, firma invalida, UUID local no disponible, UUID diferente, licencia no iniciada, licencia vencida y fecha de servidor no disponible.
- **FR-014**: Los resultados MUST ser inmutables, MUST contener un codigo estable y mensaje seguro, y MUST NOT exponer contenido de licencia, firma, llave, UUID completo, credenciales, cadena de conexion, configuracion completa o excepcion.
- **FR-015**: Logs y diagnosticos MUST usar eventos estructurados seguros y MUST NOT registrar contenido de licencia, firma, llaves, UUID completos, rutas sensibles, credenciales ni configuracion serializada.
- **FR-016**: La cancelacion solicitada MUST propagarse sin convertirse en fallo de licencia ni registrarse como error.
- **FR-017**: La validacion MUST poder comprobarse con fuentes controladas de UUID, archivos, llaves y fecha de servidor, sin depender del equipo o Firebird reales.
- **FR-018**: El servicio de licencias MUST estar disponible mediante la composicion de dependencias, sin permitir que Presentation lea archivos, ejecute comandos del sistema, valide firmas o consulte Firebird directamente.
- **FR-019**: Esta feature MUST NOT habilitar aun la interfaz grafica de importacion, editar configuracion, generar licencias, incluir llaves privadas, usar el reloj local, generar codigos de barras, imprimir ni bloquear globalmente la shell.

### Key Entities

- **License document**: Datos heredados firmados: UUID de destino, limites de vigencia y firma, junto con la identidad comprobada del emisor.
- **License validation result**: Decision inmutable y segura con etapa, codigo, estado final, emisor cuando fue autenticado y limites temporales cuando fueron interpretados.
- **License source**: Entrada directa o archivo del que se obtiene el documento, representada sin exponer el contenido ni una ruta sensible en resultados visibles.
- **Local hardware identity**: UUID canonico recuperado del equipo Windows mediante una capacidad sustituible para pruebas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de los vectores de compatibilidad aceptados por el sistema Java para ambos emisores produce una licencia valida en C# sin regeneracion.
- **SC-002**: El 100% de una matriz con al menos 25 alteraciones de UUID, fechas, firma, codificacion y estructura se rechaza con el codigo esperado y sin excepciones no controladas.
- **SC-003**: El 100% de los casos de limite inicial, limite final, antes del inicio y despues del fin produce la misma decision aunque cambie el reloj local de Windows.
- **SC-004**: Los escenarios automatizados de fallo de UUID y servidor terminan con una decision segura o una cancelacion propagada, sin falsos positivos.
- **SC-005**: Una auditoria automatizada encuentra cero apariciones de los centinelas de licencia, firma, UUID completo, ruta, password y cadena de conexion en resultados, mensajes y logs.
- **SC-006**: El conjunto automatizado de la feature se ejecuta sin abrir WPF, sin una base Firebird real y sin ejecutar herramientas reales de identidad del equipo.
- **SC-007**: Una revision arquitectonica confirma que el 100% de las operaciones de archivos, plataforma y servidor quedan detras de abstracciones y que no existe validacion de licencia en Views o ViewModels.

## Assumptions

- Las dos llaves publicas embebidas en `LicenseJsonValidator` del proyecto Java son las fuentes autorizadas; ninguna llave privada se copia ni se distribuye con SysTools.
- El repositorio de reloj de servidor de la feature 004 conserva la semantica del `TIMESTAMP` Firebird sin inventar zona horaria.
- La configuracion de la feature 002 sigue tratando `Licencia` como valor opaco; guardar o seleccionar una nueva licencia pertenece a la futura pantalla de configuracion.
- La comparacion de compatibilidad usa la implementacion Java y vectores firmados controlados disponibles localmente; los archivos de prueba no contienen credenciales ni llaves privadas.
- La decision de impedir operaciones concretas en la UI se integrara en la feature de interfaz del verificador; esta etapa entrega el servicio y sus estados.
- Renovacion, revocacion remota, activacion en linea, generacion de licencias y rotacion de llaves estan fuera de alcance.
