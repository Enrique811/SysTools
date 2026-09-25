# Engine adoption decision

FastReport WPF 2026.2 es técnicamente viable, pero no se adopta en 011: OpenSource carece de preview/print local, el paquete público WPF es Demo y la edición productiva requiere licencia comercial no autorizada. Se seleccionó un adaptador nativo Windows, sustituible mediante puertos Business y sin dependencia nueva. Los templates incluidos tienen `productionApproved=false` hasta validar goldens, fidelidad heredada e impresión física.

Fuentes: [comparison](https://github.com/FastReports/FastReport.Documentation/blob/master/COMPARISON.md), [FastReport WPF](https://www.fast-report.com/products/fast-report-net), [WPF printing](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/documents/printing-overview).
