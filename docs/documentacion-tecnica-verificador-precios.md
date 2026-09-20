# Documentacion tecnica - VerificadorPrecios

## 1. Resumen ejecutivo

`VerificadorPrecios` es una aplicacion de escritorio Java/Swing orientada a consulta rapida de precios, busqueda de productos e impresion de etiquetas con codigo de barras. La aplicacion se conecta a una base de datos Firebird, lee productos desde tablas operativas, valida licencia local atada al UUID del equipo Windows y genera etiquetas mediante JasperReports.

El sistema actual esta construido como proyecto NetBeans Ant, con Java 8 como nivel de compilacion y JDK 21 como plataforma de ejecucion/empaquetado. La distribucion final se genera como JAR y opcionalmente como instalador `.exe` mediante `jpackage` y un runtime embebido.

Desde una perspectiva de migracion a C#, el proyecto debe tratarse como una aplicacion desktop con reglas de negocio embebidas en UI. La migracion no debe limitarse a copiar pantallas: hay que separar explicitamente configuracion, conexion, consultas, licenciamiento, codigos de barras, reportes e impresion.

## 2. Ubicacion y tecnologia actual

Ruta analizada:

```text
C:\Users\gaming\Documents\NetBeansProjects\Proyectos\VerificadorPrecios
```

Tecnologias principales:

- Lenguaje: Java.
- UI: Swing.
- IDE/proyecto: NetBeans Ant.
- Base de datos: Firebird via Jaybird JDBC.
- Reportes: JasperReports `.jasper` / `.jrxml`.
- Codigos de barras: ZXing.
- Empaquetado: JAR + `jpackage` para instalador Windows.
- Sistema operativo objetivo: Windows.

Punto de entrada:

```text
src/App/Main.java
```

Clase principal configurada:

```text
App.Main
```

Artefacto principal:

```text
dist/Verificador.jar
Ejecutable/VerificadorPrecios.jar
```

## 3. Estructura funcional del proyecto

Paquetes principales:

```text
src/App
src/Conexion
src/SQL
src/Metodos
src/Ventanas
src/com/project/barcode/newimpl
reportes
lib
fonts
```

Responsabilidades por paquete:

| Paquete / carpeta | Responsabilidad |
| --- | --- |
| `App` | Arranque de aplicacion y servicios auxiliares de correo. |
| `Conexion` | Conexion global a Firebird, prueba de conexion y validacion de licencia desde capa de infraestructura. |
| `SQL` | Consultas SQL para productos y fecha/hora del servidor. |
| `Metodos` | Configuracion, modelos simples, formateo, rutas, reportes, impresoras, licencia y utilidades. |
| `Ventanas` | UI Swing y gran parte de la orquestacion de negocio. |
| `com.project.barcode.newimpl` | Deteccion y generacion de codigos de barras. |
| `reportes` | Plantillas Jasper para etiquetas. |
| `lib` | Dependencias Java incluidas como JAR locales. |

## 4. Flujo de arranque

Clase: `App.Main`

Flujo:

1. Inicia en el Event Dispatch Thread mediante `SwingUtilities.invokeLater`.
2. Sincroniza `contacto.properties` desde el directorio de la app hacia `%APPDATA%\VerificadorPrecios`.
3. Carga configuracion con `ConfigManager`.
4. Si no existe configuracion completa, abre `VentanaConfiguracion`.
5. Si existe configuracion completa, muestra dialogo modal de carga.
6. Prueba conexion Firebird en `SwingWorker`.
7. Si la conexion es exitosa, abre `VentanaInicio`.
8. Si falla, muestra detalle, registra log y abre `VentanaConfiguracion`.

Configuracion minima requerida:

```text
ipEmpresa
usuario
password
rutaEmpresa
```

## 5. Persistencia local de configuracion

Clase principal: `Metodos.ConfigManager`

Ruta de configuracion:

```text
%APPDATA%\VerificadorPrecios\configuracion.properties
```

Fallbacks:

- Windows sin `APPDATA`: `%USERPROFILE%\AppData\Roaming\VerificadorPrecios`.
- Otros sistemas: `~/.verificadorprecios`.
- Ultimo recurso: directorio de ejecucion.

Archivo de contacto:

```text
%APPDATA%\VerificadorPrecios\contacto.properties
```

Los valores de `configuracion.properties` se guardan codificados en Base64. Esto no es cifrado real, solo ofuscacion reversible.

Campos conocidos:

| Campo | Uso |
| --- | --- |
| `ipEmpresa` | Host/IP del servidor Firebird. |
| `rutaEmpresa` | Ruta de archivo `.fdb`. |
| `usuario` | Usuario Firebird. Default logico: `SYSDBA`. |
| `password` | Password Firebird. Default logico: `masterkey`. |
| `clave` | Licencia JSON o referencia de licencia. |
| `ambiente` | `a` pruebas, `b` productivo. |
| `impresora` | Nombre exacto de impresora Windows. |
| `formatoPrecio` | `CO` sin decimales, `MX` con 2 decimales. |
| `reporte` | Nombre de plantilla `.jasper`. |
| `columnas` | Cantidad de etiquetas por fila/logica: `1`, `2` o `3`. |
| `informacion` | Texto adicional persistido para etiqueta. |

## 6. Conexion a base de datos

Clase: `Conexion.Conexion`

Driver:

```text
org.firebirdsql.jdbc.FBDriver
```

URL construida:

```text
jdbc:firebirdsql://{ipEmpresa}/{rutaEmpresa_normalizada}?lc_ctype=ISO8859_1
```

Caracteristicas:

- Conexion global estatica: `Connection conexion`.
- PreparedStatement y ResultSet tambien son estaticos.
- Login timeout de prueba: 5 segundos.
- En conexion real se reutiliza si sigue abierta.
- El cierre de recursos se maneja con `cerrarRecursosConsulta`.
- Los errores de prueba se registran en:

```text
%APPDATA%\VerificadorPrecios\logs\conexion.log
```

Riesgo arquitectonico:

La conexion global estatica simplifica Swing, pero dificulta pruebas, concurrencia y migracion limpia. En C# conviene reemplazarla por servicios inyectables:

- `IDatabaseConnectionFactory`
- `IProductRepository`
- `IServerClock`
- `ILicenseValidator`

## 7. Modelo de datos y consultas

Clase: `SQL.SQLArticulo`

Consulta principal de producto:

```sql
SELECT
 A.ID AS CODIGO,
 A.ID AS IDENTIFICACION,
 A.CODIGO AS CODIGO_BARRAS,
 A.DESCRIPCION AS DESCRIPCION,
 A.TVENTA AS FORMATO,
 A.TVENTA AS PRESENTACION,
 A.PFINAL AS PRECIO_IVA,
 B.CANTIDAD_ACTUAL AS STOCK
FROM PRODUCTOS A
LEFT JOIN INVENTARIO_BALANCES B ON A.ID = B.PRODUCTO_ID
```

Busqueda por codigo de barras:

```sql
WHERE A.CODIGO = ?
```

Busqueda por descripcion:

```sql
WHERE UPPER(A.DESCRIPCION) LIKE UPPER(?)
ORDER BY A.DESCRIPCION
```

El patron usado en descripcion es:

```text
{texto}%
```

Es decir, busqueda por prefijo, no por contiene.

Precio especial:

```sql
SELECT PRECIO_IVA
FROM PRECIOS_ESPECIALES
WHERE ARTICULO = ?
  AND TARIFA = ?
  AND FECHA_INI <= ?
  AND ? < FECHA_FIN
```

Observacion:

Existe una consulta auxiliar heredada contra tablas `ARTICULO`, `LINTARIF`, `TIPOSIVA`, `TIPOSIEPS`, `COD_BARRAS`, pero el flujo principal actual usa `PRODUCTOS` e `INVENTARIO_BALANCES`.

Modelo usado en busqueda:

```text
Metodos.Articulos
```

Campos:

- codigo.
- codigoBarras.
- identificacion.
- descripcion.
- stock.
- presentacion.
- precioIva.

## 8. Fecha/hora del servidor

Clase: `SQL.SQLFechaHora`

Consulta:

```sql
SELECT CURRENT_TIMESTAMP AS HORAFECHA FROM RDB$DATABASE
```

Usos:

- Fecha impresa en etiquetas.
- Validacion de vigencia de licencia.

Regla importante:

La licencia se valida contra la fecha del servidor Firebird, no contra la hora local del equipo. Esta decision reduce manipulacion local de fecha y debe conservarse en C#.

## 9. Licenciamiento

Clases:

- `Metodos.LicenseJsonValidator`
- `Metodos.LicenseValidationResult`
- `Conexion.Conexion.tieneLicenciavalida`
- `Ventanas.ConfiguracionWindow`

Formato esperado de licencia:

```json
{
  "uuid": "...",
  "inicio": "yyyy-MM-dd HH:mm:ss",
  "fin": "yyyy-MM-dd HH:mm:ss",
  "firma": "..."
}
```

Validaciones:

1. Entrada no vacia.
2. Puede venir como JSON directo o ruta de archivo.
3. Debe contener campos `uuid`, `inicio`, `fin`, `firma`.
4. Valida firma RSA `SHA256withRSA`.
5. Soporta dos llaves publicas embebidas:
   - Distribuidor Colombia.
   - Desarrollador.
6. Obtiene UUID local del equipo Windows.
7. Compara UUID de licencia contra UUID local normalizado.
8. Obtiene fecha actual desde Firebird.
9. Valida que fecha servidor este entre inicio y fin.

Comandos usados para UUID:

```text
powershell.exe -NoProfile -Command "(Get-CimInstance Win32_ComputerSystemProduct).UUID"
WMIC.exe csproduct get uuid
```

Se intentan rutas `System32` y `Sysnative`.

Resultado final:

```text
archivoLeido && jsonValido && firmaValida && uuidValido && vigente
```

Implicacion para C#:

La migracion debe preservar compatibilidad binaria/criptografica de la firma. El payload firmado se arma asi:

```text
uuid={UUID}
inicio={yyyy-MM-dd 00:00:00}
fin={yyyy-MM-dd 23:59:59}
```

Usar `RSA.VerifyData` con SHA-256 y padding PKCS#1 v1.5 en .NET.

## 10. Interfaz grafica

### 10.1 Ventana de conexion inicial

Clase: `Ventanas.VentanaConfiguracion`

Uso:

- Primera configuracion de base de datos.
- Captura `rutaEmpresa`, `ipEmpresa`, `usuario`, `password`.
- Permite seleccionar archivo `.fdb`.
- Guarda configuracion y prueba conexion.
- Si conecta, abre ventana principal.

### 10.2 Ventana principal

Clase: `Ventanas.VentanaInicio`

Responsabilidades:

- Mostrar captura de codigo de barras.
- Mostrar informacion adicional para etiqueta.
- Consultar producto.
- Mostrar descripcion, presentacion, existencia y precio.
- Abrir busqueda modal.
- Abrir configuracion avanzada.
- Generar/imprimir etiquetas.
- Validar licencia antes de permitir operacion.
- Administrar cola de etiquetas para 2 o 3 columnas.

Atajos:

| Atajo | Accion |
| --- | --- |
| Enter | Consultar producto. |
| Ctrl + F7 | Buscar productos. |
| Ctrl + F8 | Imprimir etiqueta. |
| Ctrl + F9 | Configuracion. |
| Ctrl + F10 | Cancelar lista pendiente. |
| Esc | Salir o cerrar vista previa. |

Flujo de consulta:

1. Verifica licencia.
2. Lee `codigoBarras`.
3. Si esta vacio, limpia UI.
4. Ejecuta `SQLArticulo.buscarArticuloPorCodigoBarra`.
5. Si encuentra:
   - Formatea precio.
   - Actualiza UI.
   - Muestra estado exito.
   - Si columnas > 1, captura articulo en cola.
6. Si no encuentra:
   - Limpia UI.
   - Muestra aviso.

### 10.3 Busqueda de productos

Clases:

- `Ventanas.BusquedaDialog`
- `Ventanas.VentanaBuscarArticulo`

Comportamiento:

- Dialogo modal sin decoracion.
- Busca por descripcion.
- Llena tabla con codigo de barras, descripcion, precio y existencia.
- Doble click o Enter selecciona articulo.
- Al seleccionar, llama `VentanaInicio.cargarArticuloDesdeBusqueda`.

### 10.4 Configuracion avanzada

Clase: `Ventanas.ConfiguracionWindow`

Campos:

- Ambiente:
  - `a`: pruebas, muestra vista previa Jasper.
  - `b`: productivo, imprime directo.
- Formato de precio:
  - `CO`: moneda Colombia sin decimales.
  - `MX`: pesos con 2 decimales.
- Columnas: `1`, `2`, `3`.
- Licencia `.lic`.
- Impresora instalada.
- Reporte `.jasper`.

Acciones adicionales:

- Copiar UUID del equipo.
- Importar licencia `.lic`.
- Solicitar licencia por correo.
- Reportar error por correo.

## 11. Codigos de barras

Paquete:

```text
com.project.barcode.newimpl
```

Clases:

- `BarcodeFacade`
- `BarcodeService`
- `BarcodeGenerator`
- `BarcodeResult`
- `BarcodeType`

Tipos soportados:

- EAN-8.
- EAN-13.
- UPC-A.
- CODE128.

Regla de deteccion:

- Si el valor no es numerico: CODE128.
- 8 digitos con checksum valido: EAN-8.
- 12 digitos con checksum valido: UPC-A.
- 13 digitos con checksum valido: EAN-13.
- Si falla checksum: CODE128.

Dimensiones:

| Tipo | Dimension |
| --- | --- |
| EAN/UPC | 340 x 56 |
| CODE128 | max(500, longitud * 18) x 60 |

Salida:

```text
PNG en byte[]
```

## 12. Reportes e impresion

Clases:

- `Metodos.ReporteManager`
- `Metodos.DatosReporte`
- `Ventanas.VentanaInicio`
- `Metodos.PrinterUtils`

Directorio de reportes:

```text
reportes
```

Extension requerida:

```text
.jasper
```

Plantillas detectadas:

- Etiquetas de 31x25 mm.
- Etiquetas de 57x40 mm.
- Etiquetas de 57x50 mm.
- Etiquetas de 70x40 mm.
- Etiquetas de 80x50 mm.
- Etiquetas de 100x50 mm.
- Etiquetas de 100x70 mm.
- Etiquetas de 120x60 mm.

Modelo `DatosReporte` expone propiedades para:

- Una etiqueta principal.
- Segunda etiqueta.
- Tercera etiqueta.

Campos de reporte:

- `CODIGO_BARRAS` como imagen.
- `CODIGO_BARRAS_TEXTO`.
- `IS_CODE128`.
- `IDENTIFICACION`.
- `DESCRIPCION`.
- `PRECIO_VENTA`.
- `FECHA`.
- `INFORMACION`.
- Sufijos `_2` y `_3` para columnas adicionales.

Salida por ambiente:

| Ambiente | Comportamiento |
| --- | --- |
| `a` | Muestra `JasperViewer` como vista previa. |
| `b` | Busca impresora por nombre exacto e imprime sin dialogos. |

Logica de columnas:

- `columnas = 1`: imprime articulo actual.
- `columnas = 2` o `3`: acumula articulos en memoria.
- En pruebas, al completar la cola, habilita boton de impresion.
- En productivo, al completar la cola, imprime automaticamente.

## 13. Formato de precios

Clase: `Metodos.PrecioFormatter`

Reglas:

- `MX`: `$` + dos decimales usando separador decimal con `Locale.US`.
- `CO`: moneda Colombia sin decimales usando `Locale("es", "CO")`.
- Default: `CO`.

Ejemplos esperados:

```text
MX -> $123.45
CO -> $ 123 o formato equivalente local sin decimales
```

## 14. Dependencias Java principales

Definidas en `nbproject/project.properties`:

| Dependencia | Uso probable |
| --- | --- |
| `jaybird-full-2.2.4.jar` | Driver Firebird JDBC. |
| `jasperreports-6.3.0.jar` | Generacion de reportes. |
| `core-3.5.3.jar`, `javase-3.5.3.jar` | ZXing. |
| `javax.mail`, `jakarta.mail-api`, `javax.activation` | Correo/activacion. |
| `jcalendar-1.4.jar` | Componentes de fecha heredados. |
| `jfreechart`, `jcommon` | Graficos/reportes heredados. |
| `poi` | Excel/heredado. |
| `commons-*`, `com.lowagie.text` | Dependencias transitivas Jasper. |

Nivel Java:

```text
javac.source=1.8
javac.target=1.8
platform.active=JDK_21
```

## 15. Empaquetado y distribucion

Build Ant:

```text
build.xml
```

Post build:

- Crea carpeta `Ejecutable`.
- Copia `dist/Verificador.jar` como `Ejecutable/VerificadorPrecios.jar`.
- Copia `contacto.properties`.

Script instalador:

```text
generar-exe-jpackage.bat
```

Requisitos:

- `VerificadorPrecios.jar`.
- Carpeta `lib`.
- Carpeta `reportes`.
- JDK en `C:\Program Files\Java\jdk-21.0.10\bin`.

Proceso:

1. Copia JAR, `lib` y `reportes` a carpeta temporal `build`.
2. Genera runtime con `jlink`.
3. Genera instalador `.exe` con `jpackage`.
4. Limpia temporales.

Modulos incluidos en runtime:

```text
java.base
java.desktop
java.logging
java.sql
java.xml
java.naming
java.management
java.datatransfer
java.prefs
```

## 16. Riesgos tecnicos identificados

1. Logica de negocio dentro de Swing.
   - La consulta, impresion, licencia y estado de cola viven en ventanas.
   - Para C# conviene extraer servicios antes o durante la migracion.

2. Estado global estatico.
   - `Conexion`, `SQLArticulo` y varios campos UI usan estado estatico.
   - Riesgo de errores ocultos al abrir/cerrar ventanas o consultar en paralelo.

3. Base64 para credenciales.
   - No protege usuario/password.
   - En C# conviene usar DPAPI/Windows Credential Manager si se requiere seguridad real.

4. Dependencia fuerte de Windows.
   - UUID via PowerShell/WMIC.
   - Impresoras Windows.
   - Instalador `.exe`.

5. Reportes Jasper.
   - JasperReports no es nativo en .NET.
   - Hay que decidir entre mantener reportes existentes por compatibilidad o migrar layout a otra tecnologia.

6. Codificacion Firebird.
   - La conexion usa `lc_ctype=ISO8859_1`.
   - La migracion debe probar caracteres especiales en descripciones.

7. Licencia criptografica.
   - Cualquier cambio en canonicalizacion rompe firmas existentes.
   - Deben crearse pruebas de compatibilidad con licencias reales.

8. Impresion directa.
   - En productivo se imprime sin dialogo.
   - En C# hay que validar permisos, nombres de impresora y tamano de papel.

## 17. Recomendacion de arquitectura para C#

Arquitectura sugerida:

```text
VerificadorPrecios.App          UI desktop
VerificadorPrecios.Application  Casos de uso
VerificadorPrecios.Domain       Entidades/reglas puras
VerificadorPrecios.Infrastructure Firebird, archivos, impresoras, reportes
VerificadorPrecios.Tests        Pruebas unitarias/integracion
```

Servicios recomendados:

| Servicio | Responsabilidad |
| --- | --- |
| `IConfigurationStore` | Leer/guardar configuracion local. |
| `IFirebirdConnectionFactory` | Crear conexiones Firebird. |
| `IProductRepository` | Buscar productos por codigo/descripcion. |
| `IServerClock` | Obtener fecha/hora Firebird. |
| `ILicenseValidator` | Validar firma, UUID y vigencia. |
| `IHardwareIdProvider` | Obtener UUID Windows. |
| `IBarcodeService` | Generar imagen de codigo de barras. |
| `ILabelReportService` | Renderizar etiqueta. |
| `IPrinterService` | Listar e imprimir. |
| `IEmailLauncher` | Abrir cliente/Gmail/Outlook. |

UI recomendada:

- Si el nuevo proyecto es Windows desktop moderno: WPF.
- Si se busca continuidad rapida y simple: WinForms.
- Si se requiere multiplataforma futura: Avalonia, validando impresion y Firebird.

Dependencias .NET candidatas:

| Necesidad | Opcion C# |
| --- | --- |
| Firebird | `FirebirdSql.Data.FirebirdClient`. |
| Codigos de barras | `ZXing.Net` o `SkiaSharp` + ZXing. |
| Reportes | FastReport.NET, Stimulsoft, RDLC, QuestPDF o motor propio. |
| Configuracion | JSON + DPAPI para secretos. |
| Impresion | `System.Drawing.Printing`, Win32 printing, libreria de reportes. |
| Criptografia | `System.Security.Cryptography.RSA`. |

## 18. Plan de migracion recomendado

Fase 1 - Documentar y congelar comportamiento:

- Crear casos de prueba manuales con productos reales.
- Guardar ejemplos de licencia valida, vencida, UUID incorrecto y firma invalida.
- Guardar muestras de etiquetas impresas por cada plantilla usada.
- Registrar nombres reales de impresoras y formatos.

Fase 2 - Extraer modelo funcional:

- Definir DTOs:
  - `Product`
  - `AppConfiguration`
  - `LicenseValidationResult`
  - `LabelData`
  - `PendingLabel`
- Definir interfaces de servicios.
- Implementar pruebas de precio, barcode y licencia.

Fase 3 - Implementar infraestructura C#:

- Conexion Firebird.
- Repositorio de productos.
- Fecha servidor.
- Configuracion local.
- UUID Windows.
- Validador RSA compatible.

Fase 4 - Implementar UI:

- Pantalla conexion inicial.
- Pantalla principal.
- Dialogo busqueda.
- Configuracion avanzada.
- Estados visuales y atajos.

Fase 5 - Reportes e impresion:

- Decidir tecnologia de reportes.
- Reproducir plantillas criticas.
- Validar impresion en ambiente `a` y `b`.
- Validar 1, 2 y 3 columnas.

Fase 6 - Empaquetado:

- Crear instalador Windows.
- Incluir runtime .NET si aplica.
- Incluir reportes/plantillas.
- Definir carpeta de datos en `%APPDATA%`.

## 19. Criterios de aceptacion de migracion

La version C# se puede considerar funcionalmente equivalente cuando:

- Inicia sin configuracion y obliga a configurar conexion.
- Guarda y recarga configuracion local.
- Prueba conexion Firebird con timeout.
- Consulta producto por codigo de barras.
- Busca producto por descripcion.
- Formatea precios igual que Java para `CO` y `MX`.
- Valida licencia existente sin requerir regenerarla.
- Bloquea operacion con licencia invalida.
- Obtiene fecha desde servidor Firebird.
- Genera codigos EAN-8, EAN-13, UPC-A y CODE128.
- Imprime/vista previa segun ambiente.
- Maneja columnas 1, 2 y 3 igual que la version Java.
- Conserva informacion adicional en etiqueta.
- Lista impresoras instaladas.
- Registra errores de conexion.

## 20. Decision tecnica clave antes de codificar

La decision mas importante es el motor de reportes en C#.

Opciones:

1. Rehacer etiquetas en una libreria .NET.
   - Mejor integracion futura.
   - Requiere replicar visualmente cada `.jrxml`.

2. Mantener Jasper como proceso externo temporal.
   - Menor riesgo visual inicial.
   - Mantiene dependencia Java.
   - No es migracion completa.

3. Generar etiquetas directamente con dibujo/printing.
   - Control total.
   - Mas trabajo para soportar varias medidas y columnas.

Recomendacion senior:

Para una migracion limpia a C#, extraer primero reglas y datos; despues elegir reportes. Si la prioridad es salir rapido con bajo riesgo, mantener compatibilidad visual de etiquetas debe pesar mas que cambiar el motor de reportes desde el primer sprint.
