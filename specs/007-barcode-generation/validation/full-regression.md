# Regresion completa

Fecha: 2026-09-23

Comandos:

```powershell
dotnet build SysTools.sln --no-restore
dotnet test SysTools.sln --no-build
```

- Compilacion: 0 errores, 0 advertencias.
- Pruebas: 487 aprobadas, 6 omitidas, 0 fallidas.
- Las 6 omisiones pertenecen a escenarios de integracion Firebird que requieren infraestructura externa; no son regresiones de esta feature.
- Suite nueva de barcodes: 163 aprobadas.

Resultado: PASS.
