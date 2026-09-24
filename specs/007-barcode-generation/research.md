# Research: Generacion de codigos de barras

## Decision 1: Separar clasificacion de codificacion grafica

**Decision**: Business normaliza, valida checksums, elige `BarcodeType`, calcula dimensiones y coordina el caso de uso. Define `IBarcodeImageEncoder`; Data lo implementa con ZXing.Net. Entities solo contiene enums y el resultado inmutable.

**Rationale**: La clasificacion es una regla pura y comprobable, mientras ZXing y System.Drawing son detalles de infraestructura. Esta frontera conserva la arquitectura de 3 capas y mantiene Business libre de dependencias graficas.

**Alternatives considered**: Referenciar ZXing directamente desde Business o generar desde un ViewModel. La primera opcion viola las pruebas arquitectonicas actuales; la segunda viola MVVM y FR-022.

## Decision 2: Usar el binding Windows oficial de ZXing.Net

**Decision**: Agregar `ZXing.Net.Bindings.Windows.Compatibility` 0.16.16 a Data y al proyecto de pruebas de barcode. El encoder crea la matriz/bitmap monocromatico con margen cero y lo serializa a PNG en memoria; las pruebas usan el lector del mismo binding como verificador independiente del servicio.

**Rationale**: El paquete oficial declara lectura y escritura mediante System.Drawing.Common, soporta `net10.0` y mantiene la dependencia ZXing.Net exigida por la constitucion. SysTools ya es una aplicacion Windows Desktop, por lo que el alcance de plataforma es coherente.

**Alternatives considered**: ImageSharp introduce otra licencia/dependencia; SkiaSharp agrega activos nativos; un encoder PNG propio aumenta codigo criptico y superficie de error sin beneficio para una aplicacion solo Windows.

**References**: [NuGet del binding Windows](https://www.nuget.org/packages/ZXing.Net.Bindings.Windows.Compatibility), [repositorio oficial ZXing.Net](https://github.com/micjahn/ZXing.Net).

## Decision 3: Conservar exactamente las reglas Java de deteccion

**Decision**: Tras retirar espacios exteriores una vez, considerar numerico solo `0` a `9`. Validar EAN-8 para longitud 8, UPC-A para 12 y EAN-13 para 13 con los pesos documentados; cualquier otra longitud, caracteres o checksum usa CODE128 si el valor es representable.

**Rationale**: Coincide con `BarcodeService.java`, conserva ceros iniciales y satisface FR-003 a FR-011. No se corrige ni descarta silenciosamente un checksum invalido.

**Alternatives considered**: Inferir tipo solo por longitud, corregir el verificador o convertir a numero. Todas cambian comportamiento operativo y pueden alterar el valor impreso.

## Decision 4: Limitar la entrada normalizada a 50 caracteres

**Decision**: Rechazar como `InputTooLong` cualquier valor normalizado mayor de 50 caracteres antes de clasificar o llamar al encoder. Con la formula heredada, CODE128 queda entre 500 y 900 px de ancho y a 60 px de alto.

**Rationale**: `Product` y `FirebirdProductRepository` ya fijan el codigo de producto en 50 caracteres. El limite cubre todos los valores que pueden llegar del flujo operativo y resuelve el crecimiento de memoria sin reducir compatibilidad real.

**Alternatives considered**: 80 o 128 caracteres amplian un caso que el dominio actual no puede producir y elevan dimensiones; no imponer limite deja FR-014/FR-018 sin proteccion efectiva.

## Decision 5: Aceptar el repertorio ASCII reproducible de CODE128

**Decision**: CODE128 admite caracteres ASCII `U+0000` a `U+007F` que ZXing puede codificar y decodificar literalmente. Se rechazan caracteres superiores a 127, incluidos los marcadores privados `U+00F1` a `U+00F4` que ZXing interpreta como FNC y que no preservarian el texto exacto.

**Rationale**: La implementacion `Code128Writer` de ZXing.Net rechaza caracteres mayores a 127 salvo marcadores FNC. Restringir antes de codificar produce un estado estable `UnsupportedValue` y protege SC-002, que exige recuperar exactamente el texto normalizado.

**Alternatives considered**: Enviar UTF-8 como hint, transliterar o aceptar FNC. CODE128 no transporta texto Unicode arbitrario; transliterar cambia datos y FNC cambia la semantica decodificada.

**Reference**: [Code128Writer de ZXing.Net](https://github.com/micjahn/ZXing.Net/blob/master/Source/lib/oned/Code128Writer.cs).

## Decision 6: Resultado inmutable con estados excluyentes

**Decision**: Usar `BarcodeGenerationStatus` con `Success`, `MissingInput`, `UnsupportedValue`, `InputTooLong` y `GenerationFailed`. Solo `Success` contiene texto normalizado, tipo, dimensiones y bytes. Cada estado expone un mensaje amigable estable; el resultado copia los bytes al construir y devuelve una copia a cada consumidor.

**Rationale**: Impide estados contradictorios, no expone excepciones y corrige el arreglo mutable que devolvia Java. Un fallo nunca conserva bytes parciales ni declara dimensiones de exito.

**Alternatives considered**: Un arreglo vacio como unica señal o exponer `byte[]` directamente. El primero no distingue causas; el segundo permite modificar resultados ya entregados.

## Decision 7: Generacion sin asincronia artificial y con cancelacion cooperativa

**Decision**: `IBarcodeService.Generate` es sincrono y recibe `CancellationToken`. Se llama `ThrowIfCancellationRequested` antes de validar, inmediatamente antes de ZXing y al regresar del encoder; Data hace las mismas comprobaciones alrededor de serializacion. Una cancelacion observada se propaga como `OperationCanceledException` y nunca se registra como fallo.

**Rationale**: El trabajo es CPU/memoria acotado, sin I/O, y debe completar en menos de 100 ms. `Task.Run` dentro del servicio agregaria planificacion y no podria interrumpir ZXing; las comprobaciones garantizan que una cancelacion durante la llamada no se convierta en exito.

**Alternatives considered**: API `async` sin operaciones asincronas o capturar `OperationCanceledException`. Ambas ocultan la naturaleza del trabajo o rompen FR-019.

## Decision 8: Validar el artefacto PNG antes de declarar exito

**Decision**: El encoder debe devolver PNG no vacio con firma, `IHDR`, limites de bloques, CRC, datos, `IEND`, dimensiones solicitadas y tamaño maximo de 1 MiB validos. Business trata cualquier violacion del contrato como `GenerationFailed`; las pruebas decodifican el resultado y comparan formato/texto.

**Rationale**: El limite de 50 caracteres acota el bitmap crudo a 900 x 60, por lo que 1 MiB deja margen amplio y detecta resultados anormales. Validar estructura evita imagenes parciales o ambiguas.

**Alternatives considered**: Confiar solo en ausencia de excepcion o validar unicamente longitud mayor que cero. Ninguna demuestra que el resultado sea un PNG utilizable.

## Decision 9: Logging estructurado sin el codigo

**Decision**: `BarcodeService` registra eventos estables con etapa, tipo elegido cuando exista, categoria de fallo y longitud normalizada. No interpola entrada, bytes, excepciones completas en mensajes visibles ni hashes del codigo.

**Rationale**: Cumple FR-020 y mejora el Java, que incluia el valor completo en logs. La longitud y categoria bastan para diagnosticar limites o fallos del encoder.

**Alternatives considered**: Registrar el codigo, base64 o hash. El contenido y bytes filtran datos; el hash no aporta valor operativo para este caso.

## Decision 10: Reproducir exactamente `String.trim()` del Java

**Decision**: Normalizar retirando solo caracteres `U+0000` a `U+0020` en ambos extremos mediante una rutina explicita. No usar directamente el recorte Unicode predeterminado de C#.

**Rationale**: Java `String.trim()` y C# `String.Trim()` no comparten exactamente el mismo repertorio. La rutina explicita preserva compatibilidad, contenido interior y espacios Unicode que despues se validan como representables o no.

**Alternatives considered**: Usar `Trim()` de C# o retirar solo `U+0020`. Ambas cambian casos limite del comportamiento heredado.
