# Contrato: exportación CSV

**Endpoint**: `GET /api/export.csv` ([api.yaml](api.yaml)) · **Requisitos**: RF-38, RF-39, AC-58, AC-59

## Alcance

- Desde el primer período con al menos un gasto (puntual o recurrente) hasta el período actual
  (Buenos Aires) inclusive.
- Una fila por gasto y por período en que aparece, incluidos los meses en que un recurrente solo
  está proyectado. No incluye períodos futuros.
- Sin gastos hasta el período actual: el archivo contiene solo el encabezado.

## Formato

- Codificación UTF-8 **con BOM** (`EF BB BF`); fin de línea `\r\n` (RFC 4180).
- Separador `,`; un campo que contiene `,`, `"`, `\r` o `\n` va entre comillas dobles y las
  comillas internas se duplican.
- Encabezado exacto: `periodo,nombre,monto,categoria,vencimiento,estado,fecha_pago`.
- Filas ordenadas por `periodo` ascendente; dentro de un período, por `vencimiento` ascendente
  (vacíos al final) y luego por `nombre`.

| Columna | Formato | Origen |
|---|---|---|
| periodo | `aaaa-mm` | mes del gasto |
| nombre | texto | valor efectivo del mes |
| monto | punto decimal, 2 decimales, sin miles (`30000.00`) | valor efectivo del mes |
| categoria | nombre actual de la categoría; `Sin categoría` si no tiene | FK (refleja renombres y eliminaciones, RF-20/21) |
| vencimiento | `aaaa-mm-dd` o vacío | valor efectivo del mes |
| estado | `Pagado` \| `Pendiente` | según `fecha_pago` |
| fecha_pago | `aaaa-mm-dd` o vacío | |

## Ejemplo (AC-59)

```text
periodo,nombre,monto,categoria,vencimiento,estado,fecha_pago
2026-10,"Luz, gas",1234.56,Sin categoría,2026-10-10,Pendiente,
```
