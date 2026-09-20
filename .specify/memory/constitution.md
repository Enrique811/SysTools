<!--
Sync Impact Report
Version change: 1.1.0 -> 1.1.0
Modified principles:
- I. Arquitectura De 3 Capas + Entities -> expanded dependency boundary rules
- II. WPF + MVVM Sin Logica De Negocio En Vistas -> unchanged
- III. Migracion Incremental Guiada Por Spec Kit -> sprint rule softened
- IV. Compatibilidad Funcional Con El Sistema Java -> IV. Compatibilidad Funcional Y Mejora Del Legado
- V. Seguridad, Logging Y Configuracion Controlada -> expanded error handling rules
Added sections:
- Criterios for report engine adoption
- Firebird data and encoding validation requirements
- Minimum validation expectations by layer
Removed sections:
- SemVer governance policy
Follow-up TODOs:
- Ninguno
-->
# SysTools Constitution

## Core Principles

### I. Arquitectura De 3 Capas + Entities

SysTools DEBE mantener una arquitectura de 3 capas: Presentation, Business y
Data, con Entities como conjunto compartido de objetos del sistema. Presentation
DEBE depender de Business para ejecutar reglas de negocio. Business DEBE
coordinar reglas y casos de uso. Data DEBE concentrar acceso a Firebird,
archivos, impresoras y recursos externos. Entities DEBE contener modelos
simples y compartidos sin depender de WPF, Firebird ni librerias de UI.

Business DEBE depender de abstracciones para consumir capacidades de Data
cuando la feature requiera persistencia, Firebird, configuracion, impresoras o
recursos externos. Data DEBE implementar esas abstracciones. Las Entities
PUEDEN contener validaciones simples e invariantes del dominio, pero NO DEBEN
contener acceso a infraestructura ni comportamiento de UI.

La UI NO DEBE consultar Firebird directamente, NO DEBE validar licencias
directamente y NO DEBE generar reportes directamente. Cualquier excepcion DEBE
documentarse en el plan de la feature y justificarse antes de implementarse.

Rationale: el proyecto Java actual mezcla UI, datos, licencia e impresion en
ventanas. Esta regla evita repetir ese acoplamiento durante la migracion.

### II. WPF + MVVM Sin Logica De Negocio En Vistas

Toda interfaz de escritorio DEBE implementarse con WPF y patron MVVM. Las Views
DEBEN limitarse a XAML, estilos, bindings y comportamiento visual. Los
ViewModels DEBEN exponer estado, comandos y coordinacion de pantalla, pero no
DEBEN ejecutar SQL, criptografia, impresion directa ni acceso a archivos de
configuracion.

La aplicacion DEBE organizar Presentation como una shell de utilerias. El modulo
Verificador de precios DEBE vivir como un modulo dentro de SysTools, no como la
aplicacion completa. Nuevas utilerias DEBEN agregarse como modulos sin romper
el verificador existente.

Rationale: SysTools esta planeado como suite de utilerias. Una shell modular
permite crecer sin reescribir el inicio ni mezclar responsabilidades.

### III. Migracion Incremental Guiada Por Spec Kit

La migracion DEBE realizarse por features pequenas y verificables usando el
flujo de Spec Kit: specify, clarify, plan, tasks, implement, analyze y converge
cuando aplique. Ninguna etapa de migracion DEBE agrupar toda la aplicacion como
una sola feature.

El orden base de migracion DEBE iniciar con arquitectura y shell, configuracion
local, conexion Firebird, repositorios de productos, reglas del verificador,
licenciamiento, codigos de barras, UI, busqueda, configuracion visual, reportes,
soporte e instalador. Cualquier cambio de orden DEBE justificarse por riesgo,
dependencia tecnica o bloqueo operativo.

Rationale: el sistema tiene puntos de alto riesgo como Firebird, licencias,
reportes e impresion. Dividir por etapas permite validar cada riesgo antes de
construir sobre el.

### IV. Compatibilidad Funcional Y Mejora Del Legado

SysTools DEBE preservar el comportamiento funcional del sistema Java
VerificadorPrecios mientras se migra a C#. La migracion cambia la tecnologia,
pero NO DEBE cambiar reglas operativas validas sin una decision explicita en la
feature correspondiente.

Las consultas de productos, formato de precios, licencia, UUID, fecha del
servidor, codigos de barras, columnas de etiquetas, ambiente de pruebas,
ambiente productivo, vista previa e impresion DEBEN conservar resultados
equivalentes al sistema Java actual.

SysTools NO DEBE replicar defectos, acoplamientos, fallos de seguridad,
mensajes confusos, cierres abruptos, deuda tecnica ni limitaciones accidentales
del sistema Java. Cada migracion DEBE buscar una mejora clara en al menos una de
estas dimensiones: separacion de responsabilidades, testabilidad, seguridad,
trazabilidad, experiencia operativa, mantenibilidad o manejo de errores.

Las licencias existentes DEBEN seguir siendo validas sin regenerarse. La firma
RSA, la canonicalizacion de fechas, la comparacion de UUID y la validacion con
fecha del servidor Firebird DEBEN probarse contra casos reales antes de aceptar
el modulo de licenciamiento.

Rationale: la migracion busca reemplazar tecnologia y elevar calidad tecnica,
sin alterar procesos criticos que ya funcionan para usuarios actuales.

### V. Seguridad, Logging Y Configuracion Controlada

SysTools DEBE guardar configuracion local en JSON bajo AppData. Secretos como
password de Firebird DEBEN protegerse con DPAPI u otro mecanismo Windows
aprobado antes de persistirse. No se permite guardar credenciales sensibles en
texto plano.

La aplicacion DEBE usar logging estructurado con Serilog. Errores de conexion,
licencia, reportes e impresion DEBEN registrarse con detalle tecnico suficiente
para diagnostico, sin exponer secretos. Los mensajes al usuario DEBEN ser
claros y controlados.

Todo error recuperable DEBE producir un mensaje amigable para el usuario y un
registro tecnico en log. La aplicacion NO DEBE mostrar stack traces al usuario.
La aplicacion NO DEBE cerrarse de forma abrupta salvo en errores fatales no
recuperables, y ese caso DEBE quedar registrado.

Rationale: el sistema opera con credenciales de base de datos, licencias,
impresoras y archivos locales. La seguridad y observabilidad reducen riesgo en
soporte y produccion.

## Restricciones Tecnicas

El stack tecnico aprobado para SysTools es obligatorio salvo decision
documentada en una feature:

- Lenguaje: C#.
- Framework: .NET 10 LTS.
- Escritorio: WPF.
- Patron UI: MVVM.
- Arquitectura: 3 capas + Entities.
- Base actual: Firebird.
- Acceso a datos: FirebirdSql.Data.FirebirdClient.
- ORM: no usar EF Core inicialmente.
- Patron datos: Repository.
- Inyeccion de dependencias: Microsoft.Extensions.DependencyInjection.
- Logging: Serilog.
- Configuracion: JSON en AppData.
- Secretos: DPAPI.
- Codigo de barras: ZXing.Net.
- Etiquetas: evaluar FastReport.NET primero.
- Instalador: Inno Setup.
- Versionamiento: Git.
- Desarrollo: Spec Kit + agente.

FastReport.NET solo podra adoptarse como motor de etiquetas si una prueba
tecnica demuestra que reproduce etiquetas con fidelidad suficiente, soporta
vista previa, impresion directa, plantillas de 1, 2 y 3 columnas, seleccion de
impresora y distribucion mediante instalador. Si no cumple esos criterios, la
feature de reportes DEBE documentar alternativas antes de continuar.

La integracion con Firebird DEBE validar explicitamente datos con acentos,
caracteres especiales, descripciones largas, precios decimales, valores nulos,
stock sin registro y compatibilidad de encoding con la base actual. El sistema
NO DEBE asumir que todos los campos vienen completos o en formato ideal.

La aplicacion objetivo es Windows Desktop. No se permite convertir esta
migracion en aplicacion web, microservicio o Clean Architecture completa sin una
enmienda de constitucion o una decision tecnica formal aprobada.

## Flujo De Desarrollo

Toda feature DEBE iniciar con especificacion funcional antes de implementar
codigo. Las dudas de negocio o arquitectura DEBEN resolverse durante clarify.
El plan DEBE declarar tecnologia, dependencias, entidades, riesgos y criterios
de validacion. Las tareas DEBEN ser pequenas, ordenadas y verificables.

Cada feature DEBE incluir validacion proporcional al riesgo. Son obligatorias
las validaciones de:

- Conexion Firebird.
- Consultas de productos.
- Licenciamiento.
- Formato de precios.
- Codigos de barras.
- Reportes e impresion.
- Configuracion local y proteccion de secretos.

No se exige TDD estricto para toda la aplicacion, pero los servicios criticos
DEBEN contar con pruebas o escenarios de validacion reproducibles antes de
considerarse completos. Las features que toquen Firebird, licencia, impresion o
configuracion DEBEN documentar pasos de prueba manual o automatizada.

Las validaciones minimas por capa son:

- Business: pruebas unitarias o escenarios automatizables para reglas puras.
- Data: pruebas de integracion controladas cuando exista dependencia Firebird.
- Presentation: validacion manual guiada por quickstart o checklist funcional.
- Reports: comparacion visual o impresa contra muestras aceptadas.

El primer sprint de migracion DEBE priorizar:

- Base de arquitectura y shell.
- Configuracion local y secretos.
- Conexion Firebird.

No se debe iniciar reportes ni UI completa del verificador antes de tener esas
tres bases verificadas, salvo que una prueba tecnica acotada sea necesaria para
reducir riesgo y quede justificada en el plan.

## Governance

Esta constitucion gobierna decisiones de arquitectura, tecnologia y flujo de
trabajo para SysTools. Las especificaciones, planes y tareas DEBEN cumplirla.
Cuando una feature proponga una excepcion, el plan DEBE documentar la razon, el
riesgo, el alcance y el camino de retorno.

Las enmiendas a esta constitucion DEBEN realizarse mediante el flujo de
constitucion de Spec Kit. La constitucion vigente es la fuente de verdad del
proyecto. La version indicada al final del documento identifica la version
actual aprobada; no se requiere politica SemVer para clasificar cambios. Cada
enmienda DEBE actualizar la fecha de ultima modificacion y el reporte de impacto
cuando aplique.

Antes de cerrar una feature, se DEBE revisar cumplimiento contra esta
constitucion durante plan, tasks o analyze. Si analyze detecta desviaciones, se
DEBE corregir la feature o registrar una enmienda antes de continuar.

**Version**: 1.1.0 | **Ratified**: 2026-09-20 | **Last Amended**: 2026-09-20
