# Contract: Product search workflow and modal interaction

## Business contract

```csharp
Task<PriceVerifierSearchResult> SearchAsync(
    string? descriptionPrefix,
    CancellationToken cancellationToken = default);
```

### SearchAsync

1. Requiere la sesión Ready privada; sin ella devuelve OperationalFailure sin catálogo.
2. Retira espacios exteriores una vez y conserva contenido interior/especial.
3. Devuelve MissingInput o InputTooLong (>255) antes de acceder a productos.
4. Invoca `IProductService.SearchByDescriptionAsync` exactamente una vez.
5. Conserva el orden estable entregado por Data.
6. Devuelve NoMatches para colección vacía.
7. Proyecta cada producto a item seguro y formatea únicamente precios existentes con la configuración vigente.
8. Ante fallo esperado de catálogo/formato invalida solo la sesión vigente y devuelve OperationalFailure sin items.
9. Propaga cancelación solicitada y descarta completaciones cuyo epoch dejó de ser vigente.
10. Registra solamente Stage, Status, Category y DurationMs.

## Repository prefix contract

- El prefijo es literal; `%`, `_` y otros símbolos no actúan como comodines.
- La comparación no distingue mayúsculas/minúsculas conforme a la capacidad existente.
- La consulta permanece parametrizada y de solo lectura.
- El orden autoritativo es descripción ascendente con id como desempate.
- Null/vacío/espacios produce lista vacía sin abrir conexión.

## Dialog service contract

```csharp
ProductSearchDialogResult ShowDialog(CancellationToken cancellationToken = default);
void CloseActive();
```

- Crea una instancia transitoria de ventana/ViewModel por apertura.
- Usa la shell como owner, centra en owner y no aparece en taskbar.
- Devuelve exactamente uno de Selected, Canceled u OperationalFailure.
- Cerrar/cancelar desactiva el ViewModel, cancela trabajo y suprime publicación tardía.
- Nunca devuelve `Product`, configuración ni estado visual interno.

## ProductSearchViewModel contract

- Apertura solicita foco al criterio.
- SearchCommand es awaitable y single-flight.
- Input inválido/NoMatches conserva ventana abierta, lista vacía y foco de criterio.
- Success publica lista atómica, selecciona primer item y solicita foco de tabla.
- SelectCommand solo puede ejecutar con item válido y tiene guardia contra doble confirmación.
- CancelCommand cierra sin selección.
- OperationalFailure limpia items y solicita cierre con resultado operativo.

## Parent integration contract

- `IsSearchAvailable` equivale a Ready, no Busy y ningún diálogo activo.
- Buscar y Ctrl+F7 ejecutan el mismo `OpenSearchCommand`.
- Selected asigna el código exacto y llama una vez al lookup existente.
- Canceled conserva producto/mensaje previo y solo solicita foco/select-all del código.
- OperationalFailure limpia producto, bloquea captura/búsqueda y habilita Retry.
- Deactivate/cierre de shell llama `CloseActive` antes de invalidar lifecycle.

## Keyboard and visual contract

- Ctrl+F7: abrir búsqueda.
- Enter en criterio: buscar.
- Enter en tabla: seleccionar.
- Doble clic sobre fila: mismo SelectCommand.
- Escape: cancelar.
- Tabla read-only, selección de fila completa/única, scroll y columnas Código, Descripción, Precio, Existencia.
- Estado ocupado, vacío y error tiene texto accesible; no depende solo del color.

## Privacy and scope

Prohibido en estado/logs nuevos: criterio capturado, código seleccionado, producto completo, host, ruta, usuario, password, licencia, configuración, excepción o stack trace.

Prohibido en Presentation: Data/Firebird/SQL, validación de licencia, archivos, criptografía, barcode gráfico, etiquetas, reportes, vista previa, impresión o guardado de configuración.
