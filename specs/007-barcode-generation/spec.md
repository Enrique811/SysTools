# Feature Specification: Generacion de codigos de barras

**Feature Branch**: `[007-barcode-generation]`

**Created**: 2026-09-23

**Status**: Draft

**Input**: User description: "Implementar generacion de codigos de barras, detectando EAN-8, EAN-13, UPC-A y CODE128 con las mismas reglas funcionales del sistema Java."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Elegir automaticamente el tipo compatible (Priority: P1)

Un operador proporciona el codigo de barras de un producto y el sistema identifica automaticamente si corresponde a EAN-8, EAN-13, UPC-A o si debe tratarse como CODE128, sin pedir una seleccion manual.

**Why this priority**: Elegir el tipo correcto es la base para generar una imagen legible y conservar el comportamiento de las etiquetas existentes.

**Independent Test**: Se puede proporcionar una tabla de codigos numericos validos e invalidos, textos no numericos y longitudes distintas para comprobar el tipo elegido sin usar interfaz grafica, reportes ni impresoras.

**Acceptance Scenarios**:

1. **Given** un codigo numerico de 8 digitos con checksum valido, **When** se clasifica, **Then** el tipo elegido es EAN-8.
2. **Given** un codigo numerico de 13 digitos con checksum valido, **When** se clasifica, **Then** el tipo elegido es EAN-13.
3. **Given** un codigo numerico de 12 digitos con checksum valido, **When** se clasifica, **Then** el tipo elegido es UPC-A.
4. **Given** un codigo EAN-8, EAN-13 o UPC-A con checksum invalido, **When** se clasifica, **Then** se conserva el texto y se elige CODE128 como alternativa.
5. **Given** un codigo no numerico o de otra longitud, **When** se clasifica, **Then** se elige CODE128.

---

### User Story 2 - Generar una imagen lista para etiquetas (Priority: P1)

Un consumidor de negocio solicita el codigo de barras de un producto y recibe una imagen en memoria junto con el tipo realmente usado, lista para incorporarse posteriormente a una etiqueta.

**Why this priority**: La imagen es el resultado funcional que necesitan las futuras etapas de vista previa, reportes e impresion.

**Independent Test**: Para cada tipo soportado se genera una imagen, se comprueba su formato y dimensiones, y un lector independiente recupera exactamente el texto original normalizado.

**Acceptance Scenarios**:

1. **Given** un EAN-8, EAN-13 o UPC-A valido, **When** se genera, **Then** se devuelve una imagen PNG monocromatica de 340 por 56 pixeles y el tipo detectado.
2. **Given** un valor que corresponde a CODE128, **When** se genera, **Then** se devuelve una imagen PNG de 60 pixeles de alto y ancho suficiente para conservar su legibilidad.
3. **Given** un CODE128 corto, **When** se genera, **Then** su ancho es al menos 500 pixeles.
4. **Given** un CODE128 largo, **When** se genera, **Then** su ancho crece a razon de 18 pixeles por caracter cuando ese valor supera el minimo de 500.
5. **Given** una imagen generada correctamente, **When** se decodifica, **Then** reproduce exactamente el valor utilizado para generarla, incluidos ceros iniciales.

---

### User Story 3 - Manejar entradas y fallos sin interrumpir la operacion (Priority: P2)

Un consumidor recibe un resultado controlado cuando no existe un codigo utilizable o la imagen no puede generarse, sin excepciones crudas, imagen parcial ni datos ambiguos.

**Why this priority**: Una falla de generacion es recuperable y no debe cerrar la aplicacion ni producir una etiqueta incorrecta.

**Independent Test**: Se prueban entradas nulas, vacias, solo espacios, caracteres no soportados, fallos inyectados y cancelacion; cada caso produce un resultado estable o propaga la cancelacion solicitada.

**Acceptance Scenarios**:

1. **Given** una entrada nula, vacia o compuesta solo por espacios, **When** se solicita una imagen, **Then** se devuelve un resultado de entrada ausente, sin imagen.
2. **Given** espacios exteriores alrededor de un codigo valido, **When** se genera, **Then** solo se retiran esos espacios y se conserva el contenido interior.
3. **Given** que el generador no puede representar el valor, **When** falla la operacion, **Then** se devuelve un estado controlado, sin imagen parcial ni detalle tecnico para el usuario.
4. **Given** una cancelacion solicitada, **When** la generacion esta en curso, **Then** la cancelacion se conserva y no se convierte en un fallo de codigo de barras.
5. **Given** un fallo recuperable, **When** se devuelve el resultado, **Then** incluye un mensaje amigable correspondiente al estado y no contiene detalles tecnicos.

### Edge Cases

- Los codigos numericos conservan todos sus ceros iniciales y nunca se convierten a numero.
- Solo se consideran numericos los caracteres ASCII `0` a `9`; otros digitos o separadores usan CODE128 si son representables.
- El checksum se valida con todos los digitos anteriores y el ultimo digito se trata como verificador.
- Un checksum invalido no impide generar: cambia el tipo a CODE128 y conserva el codigo completo.
- Los espacios exteriores se eliminan una vez; los espacios interiores forman parte del contenido CODE128.
- Una entrada que queda vacia despues de retirar espacios exteriores no genera imagen.
- La salida no puede compartir una coleccion de bytes mutable que permita alterar resultados ya entregados.
- Un fallo no devuelve bytes parciales ni declara exitoso un resultado sin imagen.
- Un PNG truncado, con bloques incompletos, CRC invalido o sin marcador final se trata como fallo de generacion.
- El crecimiento de ancho de CODE128 debe tener un limite seguro para impedir consumo desproporcionado ante entradas excesivas.
- Generaciones consecutivas del mismo valor deben producir el mismo tipo, dimensiones y contenido decodificable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST aceptar el codigo como texto y MUST retirar de ambos extremos unicamente caracteres comprendidos entre `U+0000` y `U+0020`, reproduciendo la normalizacion heredada de Java; MUST conservar cualquier otro caracter exterior y todo el contenido interior.
- **FR-002**: El sistema MUST conservar exactamente ceros iniciales y contenido interior; MUST NOT convertir el codigo a un tipo numerico.
- **FR-003**: Un valor compuesto exclusivamente por 8 digitos ASCII MUST clasificarse como EAN-8 solo cuando su checksum sea valido.
- **FR-004**: Un valor compuesto exclusivamente por 13 digitos ASCII MUST clasificarse como EAN-13 solo cuando su checksum sea valido.
- **FR-005**: Un valor compuesto exclusivamente por 12 digitos ASCII MUST clasificarse como UPC-A solo cuando su checksum sea valido.
- **FR-006**: La validacion de EAN-8 MUST ponderar por `3,1,3,1,3,1,3` los primeros siete digitos y comparar el verificador calculado con el octavo.
- **FR-007**: La validacion de EAN-13 MUST ponderar por `1,3,1,3,1,3,1,3,1,3,1,3` los primeros doce digitos y comparar el verificador calculado con el decimotercero.
- **FR-008**: La validacion de UPC-A MUST ponderar por `3,1,3,1,3,1,3,1,3,1,3` los primeros once digitos y comparar el verificador calculado con el duodecimo.
- **FR-009**: El digito verificador de los tres formatos MUST calcularse como el valor necesario para alcanzar el siguiente multiplo de diez, usando cero cuando la suma ya sea multiplo de diez.
- **FR-010**: Todo codigo con longitud, caracteres o checksum que no cumpla EAN-8, EAN-13 o UPC-A MUST usar CODE128 como alternativa, siempre que el contenido pueda representarse.
- **FR-011**: Un checksum invalido MUST NOT descartar ni corregir silenciosamente el valor original.
- **FR-012**: Una generacion exitosa MUST devolver una imagen PNG estructuralmente completa en memoria, con firma, cabecera, bloques integros y marcador final validos, ademas del texto normalizado, tipo, ancho y alto.
- **FR-013**: Las imagenes EAN-8, EAN-13 y UPC-A MUST medir 340 por 56 pixeles y usar margen exterior cero, conservando suficiente contraste para lectura automatizada.
- **FR-014**: Las imagenes CODE128 MUST medir 60 pixeles de alto y tener un ancho igual al mayor entre 500 pixeles y 18 pixeles por caracter normalizado, sujeto al limite seguro definido para entradas aceptadas.
- **FR-015**: Una imagen exitosa MUST poder decodificarse como el mismo tipo y texto normalizado que declara el resultado.
- **FR-016**: El resultado MUST ser inmutable y MUST impedir que un consumidor modifique los bytes conservados por otra referencia.
- **FR-017**: Entradas nulas, vacias o solo espacios MUST producir un estado controlado de entrada ausente y cero bytes de imagen.
- **FR-018**: Valores no representables, entradas excesivas o fallos de generacion MUST producir un estado controlado sin imagen parcial, excepcion cruda ni cierre de la aplicacion.
- **FR-019**: La cancelacion solicitada MUST propagarse sin convertirse en fallo ni registrarse como error.
- **FR-020**: Los registros de diagnostico MUST identificar etapa, tipo elegido y categoria de fallo sin incluir el codigo completo ni bytes de imagen.
- **FR-021**: La clasificacion y generacion MUST poder validarse sin WPF, Firebird, licencia real, motor de reportes o impresora.
- **FR-022**: La capacidad MUST estar disponible mediante la composicion de dependencias para futuros consumidores, sin permitir que Views o ViewModels generen imagenes directamente.
- **FR-023**: Esta feature MUST NOT implementar captura visual, busqueda de productos, vista previa, plantillas, reportes, seleccion de impresora ni impresion.
- **FR-024**: Todo resultado de fallo recuperable MUST incluir un mensaje amigable, estable y seguro para presentar al usuario, sin excepciones, detalles tecnicos ni contenido del codigo.

### Key Entities

- **Barcode type**: Clasificacion final de un valor como EAN-8, EAN-13, UPC-A o CODE128.
- **Barcode generation result**: Resultado inmutable que distingue exito, entrada ausente, valor no representable, entrada excesiva o fallo de generacion; cuando hay exito contiene texto normalizado, tipo, dimensiones e imagen.
- **Barcode image**: Representacion PNG en memoria del texto y tipo elegidos, aislada de mutaciones externas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de una matriz de al menos 100 codigos validos e invalidos obtiene el mismo tipo que las reglas documentadas, incluyendo al menos 20 casos por formato numerico y 20 casos CODE128.
- **SC-002**: El 100% de las imagenes de prueba generadas para EAN-8, EAN-13, UPC-A y CODE128 puede decodificarse recuperando exactamente el texto normalizado original.
- **SC-003**: El 100% de los casos con ceros iniciales conserva la misma longitud y contenido antes y despues de generar y decodificar.
- **SC-004**: En 1,000 generaciones consecutivas distribuidas entre los cuatro tipos se producen cero imagenes vacias, corruptas, duplicadas por referencia mutable o con dimensiones incorrectas.
- **SC-005**: Al menos el 95% de las generaciones con entradas de tamaño operativo termina en menos de 100 milisegundos en una prueba local controlada.
- **SC-006**: El 100% de entradas ausentes, excesivas, no representables y fallos inyectados produce un estado controlado, cero bytes parciales y cero excepciones no catalogadas.
- **SC-007**: Una auditoria automatizada encuentra cero codigos completos y cero bytes de imagen en mensajes o registros de todos los escenarios de prueba.
- **SC-008**: Todas las reglas y resultados se validan sin abrir la interfaz grafica, acceder a Firebird, validar una licencia, cargar reportes o usar una impresora.

## Assumptions

- Las reglas de deteccion y dimensiones observadas en el modulo Java actual son la referencia funcional de compatibilidad.
- `96385074`, `4006381333931` y `036000291452` pueden usarse como muestras iniciales validas de EAN-8, EAN-13 y UPC-A, junto con una matriz mas amplia calculada independientemente.
- El codigo de barras del producto ya llega como texto desde las features anteriores y puede contener ceros iniciales.
- El limite maximo de entrada y el limite de dimensiones se fijaran durante planificacion con base en la capacidad del formato y seguridad de memoria, sin cambiar el comportamiento operativo normal.
- La imagen se genera para consumo posterior por etiquetas; esta feature no decide como se escala, presenta o imprime dentro de una plantilla.
- La seleccion manual de simbologia, correccion automatica de checksum, QR, Data Matrix y otros formatos estan fuera de alcance.
- Las dependencias concretas para codificacion/decodificacion se decidiran en el plan tecnico; la especificacion exige resultados compatibles, no una biblioteca determinada.
