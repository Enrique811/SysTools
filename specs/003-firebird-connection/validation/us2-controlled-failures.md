# Validación US2: fallos controlados

Fecha: 2026-09-23

## Suite Firebird

Comando:

```powershell
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj --configuration Debug --no-restore
```

Resultado: PASS. 44 pruebas superadas, 0 fallidas y 3 integraciones omitidas.

Cobertura verificada:

- Validación conjunta y ordenada de `ipEmpresa`, `rutaEmpresa`, `usuario` y
  `password`, sin invocar Data cuando falta algún valor.
- Códigos de autenticación `335544472`, `335545106`; red `335544421`,
  `335544721`, `335544726`, `335544727`; base `335544323`, `335544344`,
  `335544375`, `335544379`.
- Precedencia autenticación, red, base y fallback `UnexpectedFailure`.
- Timeout por excepción y por deadline interno, clasificación del proveedor y
  disposición única en cada fallo.
- Catálogo exacto y seguro de mensajes para los ocho estados.

## Aislamiento de composición

Comando:

```powershell
dotnet test tests/SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~FirebirdDependencyInjectionTests|FullyQualifiedName~FirebirdFailureIsolationTests"
```

Resultado: PASS. 2 pruebas superadas. Configuración inválida, autenticación,
servidor, base, timeout, cancelación y fallo inesperado no impiden resolver y usar
la shell.

## Fallos reales autorizados

Estado: SKIPPED. El entorno no habilitó `SYSTOOLS_FIREBIRD_RUN_INTEGRATION`; por
ello no se ejecutaron password inválido ni base inexistente contra un servidor
real. Las pruebas aceptan el estado específico cuando existe un código confiable y
`UnexpectedFailure` como fallback conservador. No se copiaron valores del entorno.
