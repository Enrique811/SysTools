# Por donde empezar la migracion

## Recomendacion principal

No empieces migrando la pantalla completa ni los reportes.

La migracion debe iniciar por la base tecnica y por las piezas que bloquean todo lo demas:

```text
1. Arquitectura base
2. Configuracion local
3. Conexion Firebird
4. Consultas de productos
5. Reglas del verificador
6. Licencia
7. UI
8. Reportes e impresion
```

La razon es simple: si primero haces pantallas, puedes terminar con una UI bonita pero sin saber si Firebird, licencia, codigos de barras e impresion van a funcionar bien en C#.

## Punto de partida correcto

El primer bloque a migrar debe ser:

```text
00-base-arquitectura-shell
```

Este bloque no migra todavia la logica del Java. Su objetivo es preparar el nuevo proyecto C# para que todo lo demas tenga un lugar correcto.

Debe incluir:

- Proyecto WPF en C#.
- .NET 10 LTS.
- Arquitectura de 3 capas + Entities.
- Shell principal para futuras utilerias.
- Modulo inicial `Verificador de precios`.
- Inyeccion de dependencias.
- Logging.
- Estructura limpia de carpetas.

Resultado esperado:

```text
La app abre, muestra la shell SysUtilerias y tiene un modulo Verificador de precios vacio o placeholder.
```

Todavia no debe conectar a Firebird.
Todavia no debe imprimir.
Todavia no debe validar licencia.

## Segundo bloque

Despues sigue:

```text
01-configuracion-local-secretos
```

Aqui se migra la configuracion que en Java vive en:

```text
%APPDATA%\VerificadorPrecios\configuracion.properties
```

Pero en C# debe quedar como:

```text
%APPDATA%\SysUtilerias\configuracion.json
```

Debe incluir:

- `AppConfiguration`.
- Lectura y escritura JSON.
- Valores por defecto.
- Validacion de campos requeridos.
- Password protegido con DPAPI.

Campos a migrar:

```text
ipEmpresa
rutaEmpresa
usuario
password
ambiente
impresora
formatoPrecio
reporte
columnas
informacion
clave/licencia
```

Resultado esperado:

```text
La app puede guardar y leer configuracion local sin depender del Java.
```

## Tercer bloque

Luego sigue:

```text
02-conexion-firebird
```

Aqui ya se conecta a la base de datos.

Debe incluir:

- `FirebirdConnectionFactory`.
- `ConnectionTestService`.
- Construccion de cadena de conexion.
- Timeout.
- Logging de errores.
- Prueba de conexion desde la UI de configuracion.

Equivalente Java:

```text
Conexion.probarConexion(config)
```

Resultado esperado:

```text
Con una configuracion valida, la app confirma conexion exitosa.
Con datos incorrectos, muestra error controlado y escribe log.
```

## Cuarto bloque

Despues:

```text
03-productos-fecha-servidor
```

Aqui se migran las consultas reales.

Debe incluir:

- `Product`.
- `ProductRepository`.
- `ServerClockRepository`.
- Buscar producto por codigo de barras.
- Buscar productos por descripcion.
- Obtener fecha/hora del servidor Firebird.

Equivalentes Java:

```text
SQLArticulo.buscarArticuloPorCodigoBarra
SQLArticulo.buscarArticuloPorDescripcion
SQLFechaHora.obtenerFechayHoraActualDelServidor
```

Resultado esperado:

```text
Desde una prueba o pantalla simple puedes consultar productos reales de Firebird.
```

## Quinto bloque

Luego:

```text
04-reglas-verificador
```

Aqui se migra la logica sin UI.

Debe incluir:

- `ProductService`.
- `PriceFormatterService`.
- `LabelQueueService`.
- Formato de precio `CO`.
- Formato de precio `MX`.
- Cola de etiquetas para 1, 2 y 3 columnas.

Equivalentes Java:

```text
PrecioFormatter
VentanaInicio.capturarArticuloParaColumnas
VentanaInicio.imprimirEtiqueta
```

Resultado esperado:

```text
La logica del verificador funciona en servicios, sin depender de ventanas WPF.
```

## Sexto bloque

Despues:

```text
05-licenciamiento
```

Este bloque es critico y no conviene dejarlo al final.

Debe incluir:

- Lectura de licencia JSON o `.lic`.
- Validacion RSA.
- Validacion de UUID local.
- Validacion de fecha contra Firebird.
- Compatibilidad con licencias actuales del Java.

Equivalente Java:

```text
LicenseJsonValidator
LicenseValidationResult
Conexion.tieneLicenciavalida
```

Resultado esperado:

```text
Una licencia valida en Java tambien debe ser valida en C#.
```

## Septimo bloque

Luego:

```text
06-codigo-barras
```

Debe incluir:

- `BarcodeService`.
- Deteccion EAN-8.
- Deteccion EAN-13.
- Deteccion UPC-A.
- Fallback a CODE128.
- Generacion de imagen.

Equivalente Java:

```text
com.project.barcode.newimpl
```

Resultado esperado:

```text
El sistema puede generar imagen de codigo de barras para una etiqueta.
```

## Octavo bloque

Ahora si:

```text
07-ui-verificador
```

En este punto ya existen:

- Configuracion.
- Conexion.
- Productos.
- Reglas.
- Licencia.
- Codigos de barras.

Entonces la UI solo consume servicios.

Debe incluir:

- Pantalla del verificador.
- Captura de codigo.
- Informacion adicional.
- Descripcion.
- Presentacion.
- Existencia.
- Precio.
- Estados.
- Atajos.

Resultado esperado:

```text
El modulo Verificador de precios ya opera de punta a punta, excepto impresion final si reportes aun no estan listos.
```

## Noveno bloque

Despues:

```text
08-busqueda-productos
```

Debe incluir:

- Dialogo/modal de busqueda.
- Buscar por descripcion.
- Tabla de resultados.
- Seleccionar producto y cargarlo en el verificador.

Resultado esperado:

```text
El usuario puede buscar productos sin escanear codigo.
```

## Decimo bloque

Luego:

```text
09-configuracion-visual
```

Aqui se terminan las pantallas de configuracion.

Debe incluir:

- Configuracion de conexion.
- Configuracion avanzada.
- Licencia.
- Impresora.
- Reporte.
- Ambiente.
- Formato de precio.
- Columnas.

Resultado esperado:

```text
El usuario puede configurar todo desde WPF.
```

## Ultimo bloque grande

Finalmente:

```text
10-reportes-impresion
```

Este bloque se deja despues porque tiene mas incertidumbre tecnica.

Debe incluir:

- Prueba tecnica con FastReport.NET.
- Plantilla de etiqueta.
- Vista previa.
- Impresion directa.
- Soporte 1, 2 y 3 columnas.

Resultado esperado:

```text
La etiqueta impresa en C# debe ser equivalente a la del Java.
```

## Orden definitivo recomendado

```text
00-base-arquitectura-shell
01-configuracion-local-secretos
02-conexion-firebird
03-productos-fecha-servidor
04-reglas-verificador
05-licenciamiento
06-codigo-barras
07-ui-verificador
08-busqueda-productos
09-configuracion-visual
10-reportes-impresion
11-soporte-logs
12-instalador-despliegue
```

## Primer sprint recomendado

El primer sprint debe cubrir solo:

```text
00-base-arquitectura-shell
01-configuracion-local-secretos
02-conexion-firebird
```

Con eso ya tienes una base real:

- App nueva en C#.
- Arquitectura correcta.
- Configuracion persistente.
- Conexion Firebird validada.

Ese es el primer punto donde vale la pena decir:

```text
La migracion ya empezo bien.
```

## Primer prompt para Spec Kit

Usa este prompt para iniciar:

```text
Crear la base de SysUtilerias como aplicacion Windows Desktop en C# con .NET 10 LTS y WPF. Debe usar arquitectura de 3 capas + Entities, patron MVVM, una shell principal preparada para multiples utilerias con menu lateral, un modulo inicial llamado Verificador de precios como placeholder, configuracion de inyeccion de dependencias con Microsoft.Extensions.DependencyInjection y logging con Serilog. La UI no debe contener logica de negocio ni acceso directo a datos.
```

## Despues de completar la primera feature

Continuar con:

```text
Implementar configuracion local de SysUtilerias guardada como JSON en AppData, con entidad AppConfiguration, repositorio de configuracion, servicio de configuracion, valores por defecto, validacion de campos requeridos y proteccion del password de Firebird usando DPAPI. La configuracion debe incluir ipEmpresa, rutaEmpresa, usuario, password, ambiente, impresora, formatoPrecio, reporte, columnas, informacion y licencia.
```

