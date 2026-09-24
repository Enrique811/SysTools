# Resultados del quickstart

Fecha: 2026-09-23

Se ejecutaron los escenarios de `quickstart.md` desde la raiz del repositorio:

1. Restauracion y compilacion: exitosas.
2. Clasificacion y checksums: 105 pruebas Compatibility verdes; EAN-8, EAN-13, UPC-A y fallback CODE128 correctos.
3. PNG y round-trip: 12 pruebas Data verdes; dimensiones, margen cero, contraste y recuperacion exacta verificados.
4. Resultados y fallos: 48 pruebas Business/Entities verdes; estados, mensajes seguros, copia defensiva, PNG estructuralmente invalido y cancelacion verificados.
5. Seguridad y arquitectura: 2 pruebas Security y 17 Architecture/Composition verdes; DI correcta, cero filtraciones y dependencias confinadas.
6. Rendimiento: 1 prueba con 1,000 generaciones correctas y p95 menor de 100 ms.
7. Regresion: 487 aprobadas, 6 omitidas por dependencia externa y 0 fallidas.

Resultado: PASS.
