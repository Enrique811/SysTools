# Services

Las reglas del verificador se implementan fuera de la UI en carpetas de dominio de Business:

- `Products/ProductService`: normaliza consultas y consume `IProductRepository`.
- `Labels/PriceFormatterService`: formatea `MX` y `CO` con cultura y redondeo explicitos.
- `Labels/LabelQueueService`: mantiene filas efimeras de una, dos o tres etiquetas y permite cancelar pendientes.

Los valores compartidos de etiqueta viven en `Entities/Labels`. Esta etapa no incluye WPF funcional, licencia, imagen de codigo de barras, reportes, vista previa ni impresion.
