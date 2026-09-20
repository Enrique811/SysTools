# Planeacion de migracion - Flujo Spec Kit

## Objetivo general

Migrar el proyecto Java `VerificadorPrecios` a una aplicacion C# Windows Desktop basada en:

| Area | Decision |
| --- | --- |
| Lenguaje | C# |
| Framework | .NET 10 LTS |
| Escritorio | WPF |
| Patron UI | MVVM |
| Arquitectura | 3 capas + Entities |
| Base de datos | Firebird |
| Acceso a datos | FirebirdSql.Data.FirebirdClient |
| ORM | No usar EF Core inicialmente |
| Patron datos | Repository |
| DI | Microsoft.Extensions.DependencyInjection |
| Logging | Serilog |
| Configuracion | JSON en AppData |
| Secretos | DPAPI |
| Codigo de barras | ZXing.Net |
| Etiquetas | Evaluar FastReport.NET primero |
| Instalador | Inno Setup |
| Versionamiento | Git |
| Desarrollo | Spec Kit + agente |

## Arquitectura objetivo

```text
SysUtilerias
│
├── Presentation
│   ├── Shell
│   ├── Modules
│   │   └── PriceVerifier
│   ├── Views
│   ├── ViewModels
│   ├── Commands
│   ├── Converters
│   └── Styles
│
├── Business
│   ├── Services
│   ├── Validators
│   └── Formatters
│
├── Data
│   ├── Connection
│   └── Repositories
│
├── Entities
│
├── Reports
│   └── Templates
│
└── Infrastructure
    ├── Logging
    ├── Security
    └── Installation
```

Flujo permitido:

```text
Presentation -> Business -> Data
        \          |        /
             Entities
```

Reglas:

- La UI no consulta Firebird directamente.
- La UI no valida licencias directamente.
- La UI no genera reportes directamente.
- Business coordina reglas y casos de uso.
- Data ejecuta SQL, archivos y recursos externos.
- Entities contiene objetos compartidos del sistema.

## Como trabajar cada etapa en Spec Kit

Cada etapa debe pasar por este flujo:

```text
1. specify
2. clarify
3. plan
4. tasks
5. implement
6. analyze
7. converge, si quedan pendientes
```

Para cada etapa se debe generar una feature independiente. La migracion no debe manejarse como una sola feature gigante.

## Etapa 0 - Base del producto y arquitectura

### Objetivo

Crear la base tecnica del nuevo sistema C# sin migrar aun funcionalidad compleja.

### Feature Spec Kit sugerida

```text
Crear la base de SysUtilerias como aplicacion WPF en C# con .NET 10 LTS, arquitectura de 3 capas + Entities, shell principal preparada para multiples utilerias, inyeccion de dependencias, logging con Serilog y estructura inicial de carpetas.
```

### Alcance

- Crear solucion/proyecto WPF.
- Crear estructura `Presentation`, `Business`, `Data`, `Entities`, `Reports`.
- Crear shell inicial con menu lateral.
- Crear modulo placeholder `Verificador de precios`.
- Configurar DI.
- Configurar Serilog.
- Configurar archivo base de settings.
- Preparar Git.

### Resultado esperado

Aplicacion abre con shell base y navegacion inicial, aunque todavia no conecte a Firebird.

### Criterios de aceptacion

- La app compila.
- La app abre en Windows.
- Existe shell con menu lateral.
- Existe modulo `Verificador de precios` como vista inicial.
- Serilog escribe logs.
- DI resuelve servicios basicos.

## Etapa 1 - Configuracion local y secretos

### Objetivo

Migrar la configuracion local del sistema Java a JSON en AppData, protegiendo secretos con DPAPI.

### Feature Spec Kit sugerida

```text
Implementar administracion de configuracion local en AppData para SysUtilerias, con lectura/escritura JSON, proteccion de password con DPAPI, valores por defecto y validacion de campos requeridos para conexion Firebird.
```

### Alcance

- Crear `AppConfiguration`.
- Crear `ConfigurationRepository`.
- Crear `ConfigurationService`.
- Guardar configuracion en:

```text
%APPDATA%\SysUtilerias\configuracion.json
```

- Proteger password con DPAPI.
- Migrar campos equivalentes:
  - ipEmpresa.
  - rutaEmpresa.
  - usuario.
  - password.
  - ambiente.
  - impresora.
  - formatoPrecio.
  - reporte.
  - columnas.
  - informacion.

### Resultado esperado

El sistema puede leer, modificar y guardar configuracion sin depender del archivo Java antiguo.

### Criterios de aceptacion

- Si no existe configuracion, se crea estructura default.
- Password no queda en texto plano.
- Configuracion puede guardarse y recargarse.
- Campos requeridos se validan.

## Etapa 2 - Conexion Firebird

### Objetivo

Implementar conexion a Firebird y prueba de conexion.

### Feature Spec Kit sugerida

```text
Implementar conexion Firebird usando FirebirdSql.Data.FirebirdClient, con fabrica de conexiones, prueba de conectividad, timeout, logging de errores y compatibilidad con charset ISO8859_1 usado por el sistema Java.
```

### Alcance

- Crear `FirebirdConnectionFactory`.
- Crear `ConnectionTestService`.
- Construir connection string desde configuracion.
- Probar conexion con timeout.
- Registrar errores en log.
- Mostrar detalle controlado a UI.

### Resultado esperado

La app puede validar si la configuracion de Firebird funciona.

### Criterios de aceptacion

- Conexion exitosa con credenciales validas.
- Error controlado con credenciales invalidas.
- Error controlado con ruta `.fdb` incorrecta.
- Log generado con detalle tecnico.

## Etapa 3 - Repositorios de productos y fecha servidor

### Objetivo

Migrar las consultas SQL principales del verificador.

### Feature Spec Kit sugerida

```text
Implementar repositorios Firebird para consultar productos por codigo de barras, buscar productos por descripcion y obtener fecha/hora del servidor Firebird, devolviendo entidades limpias para la capa Business.
```

### Alcance

- Crear `Product`.
- Crear `ProductRepository`.
- Crear `ServerClockRepository`.
- Migrar consulta por codigo de barras.
- Migrar busqueda por descripcion.
- Migrar consulta `CURRENT_TIMESTAMP`.
- Normalizar stock vacio como `Sin registro`.

### Resultado esperado

Business puede pedir productos sin conocer SQL.

### Criterios de aceptacion

- Producto encontrado por codigo.
- Producto no encontrado devuelve resultado controlado.
- Busqueda por descripcion devuelve lista ordenada.
- Fecha servidor se obtiene desde Firebird.

## Etapa 4 - Reglas de negocio del verificador

### Objetivo

Implementar la logica funcional del verificador sin UI.

### Feature Spec Kit sugerida

```text
Implementar servicios de negocio para consulta de productos, formateo de precios, manejo de informacion adicional y cola de etiquetas de 1, 2 o 3 columnas.
```

### Alcance

- Crear `ProductService`.
- Crear `PriceFormatterService`.
- Crear `LabelQueueService`.
- Crear `LabelData`.
- Crear `PendingLabel`.
- Formato precio:
  - `CO`: sin decimales.
  - `MX`: dos decimales.
- Manejo de columnas:
  - 1: etiqueta individual.
  - 2: cola de dos.
  - 3: cola de tres.

### Resultado esperado

Las reglas actuales de consulta y preparacion de etiquetas funcionan sin depender de WPF.

### Criterios de aceptacion

- Precio `CO` se formatea sin decimales.
- Precio `MX` se formatea con dos decimales.
- Cola de 2 etiquetas se completa correctamente.
- Cola de 3 etiquetas se completa correctamente.
- Cancelar cola limpia pendientes.

## Etapa 5 - Licenciamiento

### Objetivo

Migrar la validacion de licencia Java a C# manteniendo compatibilidad con licencias existentes.

### Feature Spec Kit sugerida

```text
Implementar validacion de licencia JSON firmada RSA, compatible con el sistema Java actual, ligada al UUID local de Windows y validada contra fecha/hora del servidor Firebird.
```

### Alcance

- Crear `License`.
- Crear `LicenseValidationResult`.
- Crear `LicenseService`.
- Crear `HardwareIdService`.
- Leer licencia `.lic`.
- Validar JSON.
- Validar firma RSA SHA256.
- Validar UUID local.
- Validar vigencia contra Firebird.
- Mostrar diagnostico controlado.

### Resultado esperado

Una licencia valida en Java debe ser valida en C# sin regenerarse.

### Criterios de aceptacion

- Licencia valida permite operar.
- Licencia vencida bloquea operacion.
- UUID incorrecto bloquea operacion.
- Firma invalida bloquea operacion.
- Sin conexion a Firebird no se puede validar vigencia.

## Etapa 6 - Codigo de barras

### Objetivo

Migrar generacion de codigos de barras.

### Feature Spec Kit sugerida

```text
Implementar generacion de codigos de barras con ZXing.Net, detectando EAN-8, EAN-13, UPC-A y CODE128 con las mismas reglas funcionales del sistema Java.
```

### Alcance

- Crear `BarcodeService`.
- Crear `BarcodeResult`.
- Validar checksums.
- Generar PNG/imagen en memoria.
- Fallback a CODE128 si no cumple EAN/UPC.

### Resultado esperado

Las etiquetas pueden recibir imagen de codigo de barras generada en C#.

### Criterios de aceptacion

- EAN-8 valido se detecta como EAN-8.
- EAN-13 valido se detecta como EAN-13.
- UPC-A valido se detecta como UPC-A.
- Codigo no numerico se genera como CODE128.
- Checksum invalido cae a CODE128.

## Etapa 7 - UI del modulo Verificador de precios

### Objetivo

Construir la interfaz WPF del modulo principal usando MVVM.

### Feature Spec Kit sugerida

```text
Implementar el modulo WPF Verificador de precios dentro de la shell SysUtilerias, con captura de codigo de barras, informacion adicional, visualizacion de producto, precio final, estados, atajos y comandos MVVM.
```

### Alcance

- Crear `PriceVerifierView`.
- Crear `PriceVerifierViewModel`.
- Captura codigo barras.
- Informacion adicional.
- Datos producto:
  - descripcion.
  - presentacion.
  - existencia.
  - precio.
- Estados:
  - listo.
  - producto encontrado.
  - producto no encontrado.
  - licencia invalida.
- Atajos:
  - Enter.
  - Ctrl+F7.
  - Ctrl+F8.
  - Ctrl+F9.
  - Ctrl+F10.
  - Esc.

### Resultado esperado

La pantalla principal funciona con servicios reales.

### Criterios de aceptacion

- Enter consulta producto.
- UI no se congela durante consulta.
- Producto encontrado actualiza datos.
- Producto no encontrado limpia datos.
- Licencia invalida bloquea operacion.

## Etapa 8 - Busqueda de productos

### Objetivo

Migrar el dialogo de busqueda por descripcion.

### Feature Spec Kit sugerida

```text
Implementar busqueda modal de productos por descripcion en WPF, mostrando resultados en tabla y permitiendo seleccionar un producto para cargarlo en el verificador.
```

### Alcance

- Crear `ProductSearchView`.
- Crear `ProductSearchViewModel`.
- Buscar por descripcion.
- Tabla de resultados.
- Seleccion con Enter/doble click.
- Retornar codigo de barras al verificador.

### Resultado esperado

El usuario puede buscar un producto sin conocer el codigo de barras.

### Criterios de aceptacion

- Busca por prefijo de descripcion.
- Muestra codigo, descripcion, precio y existencia.
- Seleccion carga producto en pantalla principal.
- Sin resultados muestra mensaje controlado.

## Etapa 9 - Configuracion visual

### Objetivo

Migrar pantallas de configuracion inicial y avanzada.

### Feature Spec Kit sugerida

```text
Implementar pantallas WPF de configuracion de conexion y configuracion del sistema, permitiendo editar Firebird, ambiente, formato de precio, columnas, licencia, impresora y plantilla de etiqueta.
```

### Alcance

- Crear `ConnectionSettingsView`.
- Crear `SystemSettingsView`.
- Seleccionar `.fdb`.
- Probar conexion.
- Importar licencia `.lic`.
- Copiar UUID.
- Seleccionar impresora.
- Seleccionar reporte.
- Guardar configuracion.

### Resultado esperado

El usuario puede configurar el sistema completo desde la UI.

### Criterios de aceptacion

- Primera ejecucion abre configuracion si falta conexion.
- Probar conexion valida datos.
- Guardar actualiza AppData.
- Importar licencia muestra fechas/UUID.

## Etapa 10 - Reportes, etiquetas e impresion

### Objetivo

Migrar la impresion de etiquetas evaluando primero FastReport.NET.

### Feature Spec Kit sugerida

```text
Implementar generacion, vista previa e impresion de etiquetas de precio en C#, evaluando FastReport.NET como reemplazo de JasperReports y soportando plantillas de 1, 2 y 3 columnas.
```

### Alcance

- Evaluar FastReport.NET con prueba tecnica.
- Crear `LabelPrintService`.
- Crear `ReportTemplateService`.
- Vista previa para ambiente `a`.
- Impresion directa para ambiente `b`.
- Seleccion de impresora.
- Soporte 1/2/3 columnas.

### Resultado esperado

El sistema imprime etiquetas equivalentes a Java.

### Criterios de aceptacion

- Vista previa funciona.
- Impresion directa funciona.
- Plantilla 1 columna correcta.
- Plantilla 2 columnas correcta.
- Plantilla 3 columnas correcta.
- Codigo de barras y precio salen legibles.

## Etapa 11 - Correos, soporte y logs

### Objetivo

Migrar funciones auxiliares de soporte.

### Feature Spec Kit sugerida

```text
Implementar funciones auxiliares para solicitud de licencia, reporte de errores, apertura de cliente de correo, Gmail/Outlook y consulta de logs del sistema.
```

### Alcance

- Solicitar licencia por correo.
- Reportar error por correo.
- Abrir Gmail.
- Abrir Outlook.
- Abrir cliente predeterminado.
- Vista de logs basica.

### Resultado esperado

El flujo de soporte operativo queda cubierto.

### Criterios de aceptacion

- Copia UUID.
- Genera correo de solicitud.
- Genera correo de reporte.
- Permite localizar logs.

## Etapa 12 - Instalador y despliegue

### Objetivo

Preparar instalador Windows.

### Feature Spec Kit sugerida

```text
Implementar publicacion e instalador Windows con Inno Setup para SysUtilerias, incluyendo ejecutable WPF, dependencias, plantillas de reportes, configuracion inicial y accesos directos.
```

### Alcance

- Publicar app.
- Crear script Inno Setup.
- Incluir plantillas.
- Incluir assets.
- Crear acceso directo.
- Probar instalacion limpia.

### Resultado esperado

Instalador funcional para usuarios finales.

### Criterios de aceptacion

- Instala en equipo limpio.
- Ejecuta app.
- Crea accesos directos.
- No requiere Visual Studio.
- Conserva configuracion en AppData.

## Orden recomendado de ejecucion

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

## Regla de avance

No pasar a la siguiente etapa si la actual no tiene:

- `spec.md` claro.
- Clarificaciones resueltas.
- `plan.md` aprobado.
- `tasks.md` generado.
- Implementacion verificada.
- Criterios de aceptacion cumplidos.

## Primera feature recomendada

Empezar con:

```text
00-base-arquitectura-shell
```

Prompt sugerido para Spec Kit:

```text
Crear la base de SysUtilerias como aplicacion Windows Desktop en C# con .NET 10 LTS y WPF. Debe usar arquitectura de 3 capas + Entities, patron MVVM, una shell principal preparada para multiples utilerias con menu lateral, un modulo inicial llamado Verificador de precios como placeholder, configuracion de inyeccion de dependencias con Microsoft.Extensions.DependencyInjection y logging con Serilog. La UI no debe contener logica de negocio ni acceso directo a datos.
```

## Segunda feature recomendada

Despues continuar con:

```text
01-configuracion-local-secretos
```

Prompt sugerido:

```text
Implementar configuracion local de SysUtilerias guardada como JSON en AppData, con entidad AppConfiguration, repositorio de configuracion, servicio de configuracion, valores por defecto, validacion de campos requeridos y proteccion del password de Firebird usando DPAPI. La configuracion debe incluir ipEmpresa, rutaEmpresa, usuario, password, ambiente, impresora, formatoPrecio, reporte, columnas e informacion.
```

