# Research: Correos, soporte y logs

## Decision 1: Componer y delegar, nunca enviar

**Decision**: Crear borradores locales y abrir `mailto`, Gmail compose u Outlook compose. El usuario revisa, completa destinatario y envía.

**Rationale**: Evita OAuth, SMTP, almacenamiento de credenciales y comportamiento silencioso; satisface los tres canales del legado con menor riesgo.

**Alternatives considered**: SMTP directo, Microsoft Graph, Gmail API y solo portapapeles.

## Decision 2: Codificación por componentes

**Decision**: Codificar destinatario, asunto y cuerpo como componentes independientes y construir únicamente esquemas y hosts fijos administrados.

**Rationale**: Preserva Unicode y saltos de línea sin permitir que texto del usuario cambie el destino o agregue parámetros.

**Alternatives considered**: concatenación manual, codificación de formulario y plantillas HTML.

## Decision 3: Catálogo de logs por identificador simple

**Decision**: Business y Presentation manejan solo nombres simples `.log`; Data resuelve el root administrado, rechaza reparse points y subdirectorios, y aplica containment.

**Rationale**: Impide traversal y evita exponer `%LocalAppData%` en UI o contratos.

**Alternatives considered**: rutas completas, selector de archivos y búsqueda recursiva.

## Decision 4: Tail acotado

**Decision**: Leer como máximo los últimos 256 KiB de archivos de hasta 50 MiB con lectura compartida, decodificación UTF-8 tolerante y máximo 500 líneas.

**Rationale**: Tolera rotación y escritura concurrente, mantiene responsividad y evita cargar archivos arbitrarios completos.

**Alternatives considered**: lectura completa, streaming de todo el archivo y visor externo.

## Decision 5: Privacidad del reporte

**Decision**: El reporte incluye solo texto explícito del operador, versión y código opaco por operación. El UUID completo se limita a solicitud de licencia y copia explícita.

**Rationale**: Separa activación de diagnóstico y reduce exposición de información del equipo.

**Alternatives considered**: adjuntar log, incluir configuración o incluir automáticamente UUID en todo reporte.

## Decision 6: Apertura externa sustituible

**Decision**: Un puerto de Business representa apertura; Data usa asociación de Windows y devuelve estados tipados sin propagar excepciones.

**Rationale**: Mantiene MVVM testeable y centraliza la interacción con Windows.

**Alternatives considered**: abrir procesos desde code-behind o ViewModel y automatizar navegador.

