# Quickstart: Validar reglas de negocio del verificador

## Prerequisites

- Windows con el SDK fijado por `global.json` disponible en `.dotnet/` o instalado.
- Repositorio en la raiz de `SysTools`.
- No se requiere base Firebird, licencia, impresora ni configuracion local real.

## 1. Restaurar y compilar

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln --no-restore
```

Expected: toda la solucion compila sin warnings nuevos ni dependencias de infraestructura en Business/Entities.

## 2. Ejecutar pruebas de la feature

```powershell
dotnet test tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj --no-build
```

Expected:

- Consulta vacia no invoca el doble de repositorio.
- Consultas validas recortan solo extremos y conservan resultados/fallos/cancelacion.
- `MX` y `CO` son estables bajo varias culturas activas y usan redondeo `ToEven`.
- Precio nulo no llega a convertirse en etiqueta.
- Colas 1/2/3 completan exactamente en la captura correcta, en orden y sin residuos.
- Capacidad invalida o cambio durante una fila incompleta no altera pendientes.
- Cancelar informa el conteo y reinicia posiciones.

## 3. Ejecutar composicion y regresion completa

```powershell
dotnet test SysTools.sln --no-build
```

Expected: las pruebas de composicion resuelven los tres servicios y todas las suites 001-004 siguen pasando.

## 4. Revision estructural

```powershell
dotnet list src/Business/SysTools.Business.csproj reference
dotnet list src/Entities/SysTools.Entities.csproj reference
```

Expected:

- Business referencia Entities y no referencia Data o Presentation.
- Entities no referencia otros proyectos de la solucion.
- No aparecen paquetes de WPF, Firebird, barcode o reportes en Business/Entities.

## 5. Escenario manual reproducible en pruebas

1. Crear tres productos inmutables con codigos `001`, `002`, `003` y precios no nulos.
2. Formatearlos con `MX` y crear tres `LabelData` usando informacion `"Árbol  ñ  50%"`.
3. Capturar los tres con capacidad 3.
4. Confirmar que las dos primeras respuestas estan pendientes en posiciones 1 y 2.
5. Confirmar que la tercera respuesta contiene una fila completa `001, 002, 003`, conserva exactamente la informacion y deja `GetPending()` vacio.
6. Capturar una etiqueta con capacidad 2, ejecutar `Cancel()` y confirmar retorno `1`.
7. Capturar otra con capacidad 3 y confirmar que inicia en posicion 1.

Este escenario prueba el resultado funcional sin anticipar UI, barcode o impresion.
