# Cumplimiento de la constitución

Fecha: 2026-09-23

| Gate | Estado | Evidencia final |
|---|---:|---|
| 3 capas + Entities | PASS | Entities contiene resultados; Business valida/coordina; Data usa Firebird; Presentation solo compone. |
| WPF + MVVM | PASS | No se agregó UI, comando ni lógica en Views/ViewModels. |
| Spec Kit incremental | PASS | Existen spec, clarify incorporado, plan, tasks e implementación por US1–US3. |
| Compatibilidad/mejora del legado | PASS | Conserva host/base/usuario/password, `ISO8859_1`, puerto y timeout; elimina conexión global y mensajes crudos. |
| Seguridad y logging | PASS | Password solo en memoria; resultados/logs seguros; prueba y auditoría de centinelas en verde. |
| Stack aprobado | PASS | C#/.NET 10, WPF, DI y FirebirdSql 10.3.4; sin EF Core. |
| Validación proporcional | PASS | Pruebas Business/Data/Presentation, 100 ciclos, concurrencia, quickstart y 10 conexiones reales exitosas; fallos reales deliberados permanecen opt-in. |
| Encoding y datos Firebird | PASS con diferimiento | La fábrica y pruebas fijan `ISO8859_1`; acentos, textos largos, decimales, nulos y stock requieren consultas y se validarán en la feature de repositorios. |
| Orden del primer sprint | PASS | Shell y configuración preceden a conexión; no se iniciaron productos, licencia, UI completa ni reportes. |

El diferimiento de datos de producto no es una desviación: esta feature prohíbe
SQL por alcance, por lo que esos casos solo pueden validarse responsablemente en
repositorios. No hay excepciones constitucionales ni deuda que requiera una
enmienda.
