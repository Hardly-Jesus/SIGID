# EP-05 — Optimización del análisis y la predicción de demanda

Este documento resume la revisión del módulo de demanda: qué problemas se encontraron, qué se cambió, con qué evidencia y qué limitaciones quedan. Complementa `EP-05_Modelo_SSA.md`, que explica el funcionamiento completo del modelo.

## 1. Problemas identificados

| # | Problema observado | Causa encontrada |
|---|---|---|
| 1 | Predicciones de 14 días completamente en 0 (ALI-0001, TEC-0005). | **Error real en el uso de SSA.** ML.NET pronostica cada día como una combinación lineal de los últimos días observados y **no resta el nivel medio de la serie**. Si los últimos días del entrenamiento fueron 0 (frecuente en productos de venta intermitente), todas las estimaciones salen 0, sin importar cuánto se vendió antes. Se comprobó con una serie de nivel 2 cuyos últimos 7 días eran 0: el pronóstico fue `0,00` los 7 días. |
| 2 | WAPE de 100 % en la evaluación. | Consecuencia del punto 1: si SSA estima 0 y hubo ventas, el error total es igual a lo vendido (100 %). |
| 3 | SSA empataba con la referencia. | La referencia repite el último valor del entrenamiento. Cuando ese día fue 0 y SSA también daba 0, ambos estimaban 0 y sus métricas coincidían exactamente. |
| 4 | Predicción futura positiva, pero evaluación en 0. | El resultado de SSA dependía de si los últimos días de cada entrenamiento tenían ventas. La predicción entrena hasta ayer y la evaluación hasta antes del período evaluado: con distintos «últimos días», un caso daba positivo y el otro 0. |
| 5 | Siempre se usaba todo el historial. | No existía un selector; el período de entrenamiento era fijo. |
| 6 | Lentitud con miles de registros. | El historial de ventas y el de movimientos traían **todos** los registros (con sus productos) y generaban páginas enormes: con 7.600 ventas, el historial de ventas devolvía 10,5 MB de HTML. |
| 7 | Textos y aviso poco visibles. | Etiqueta «Agrupar» y aviso azul de la evaluación. |

También se revisó, sin encontrar errores: la obtención del historial (solo el producto y el origen elegidos), la agrupación diaria, el orden de las fechas, la separación cronológica, el cálculo de MAE, RMSE y WAPE (división entre 0 controlada) y la referencia (usa solo el entrenamiento). Las estimaciones negativas se llevan a 0; esa regla no elimina valores positivos pequeños (0,15 se mantiene; ver prueba `Forecast_SmallPositiveValues_AreKept`).

Un punto sí se corrigió en la preparación de datos: con ventas reales, la serie empezaba en la **primera venta de todo SIGID**, así que un producto incorporado después tenía meses de ceros artificiales al inicio. Ahora empieza en la **primera venta registrada de ese producto**: antes de esa fecha no se sabe si el producto existía, y esos días no se rellenan con 0.

## 2. Cambios realizados

1. **Serie centrada** (`SsaDemandForecastEngine`): se resta el promedio del entrenamiento, SSA modela las variaciones (tendencia y ciclo semanal) y luego se vuelve a sumar el promedio. SSA sigue siendo el único modelo; no se agregan promedios fijos ni valores manuales.
2. **Ventana según la cantidad de datos**: 7 días con menos de 180 días de entrenamiento y 14 días desde 180.
3. **Mínimo de datos según el horizonte** (sección 5).
4. **Selector «Historial de entrenamiento»** en Predicción y en Precisión del modelo (sección 4).
5. **Comportamiento de las ventas** (frecuentes, moderadas, esporádicas, historial insuficiente) y mensajes de interpretación (sección 7).
6. **Evaluación**: totales del período (vendido, estimado por SSA y por la referencia), aviso de evaluación desfavorable, rango exacto de entrenamiento y de evaluación.
7. **Paginación** de 50 registros en Historial de ventas, Historial de movimientos, Entradas y Salidas (sección 8).
8. **Interfaz**: aviso de precisión en ámbar suave con botón «Cerrar aviso»; etiqueta «Mostrar por» en Análisis de ventas.

No se agregaron paquetes ni migraciones, y no se modificaron datos.

## 3. Parámetros de SSA revisados

Se hizo una retroevaluación con series simuladas de cinco comportamientos (alta, media, baja rotación, intermitente y decreciente), 20 cortes cada una y horizontes de 7, 14 y 30 días.

| Parámetro | Antes | Ahora | Motivo |
|---|---|---|---|
| Centrado de la serie | No | Sí | Elimina los pronósticos en 0 causados por días recientes sin ventas. Sin centrar: el 60 % al 90 % de los pronósticos de productos de baja rotación salían en 0. |
| Ventana (`WindowSize`) | 7 fija | 7, o 14 desde 180 días (`LongWindowSize`, `LongWindowMinTrainingDays`) | Con 6 meses o más, la ventana de 14 días dio errores iguales o menores en la mayoría de los casos; con historiales cortos, 7 fue más estable. Ambas detectan el ciclo semanal. |
| Memoria (`SeriesLength`) | 30 | máx(30, 2 × ventana) | 30 y 60 dieron resultados idénticos; solo debe ser mayor que la ventana. |
| Entrenamiento mínimo | 28 días | máx(56, 2 × ventana + 1, 2 × horizonte) | Con un mes de datos los errores fueron claramente mayores (por ejemplo, MAE 1,26 frente a 1,15 en rotación media con 14 días de horizonte). |
| Entrenamiento máximo | 730 días | 1.096 días (opción «Todo el historial») | Permite usar hasta 3 años. |
| Límites de predicción | 95 % | 95 % | Se muestran como «rango estimado (no garantizado)»; no se usan como medida de precisión. |
| Valores negativos | Se llevan a 0 | Se llevan a 0 | No existe demanda negativa. |
| Semilla | 1 | 1 | Mismos datos, mismo resultado. |

Todos los valores están en la sección `DemandForecast` de `appsettings.json`.

## 4. Selector «Historial de entrenamiento»

Opciones: **Último mes, Últimos 3 meses, Últimos 6 meses, Últimos 12 meses (predeterminada), Todo el historial y Personalizado** (con campos Desde y Hasta, que solo aparecen al elegir esa opción).

- **Predicción**: los períodos relativos terminan **ayer** (las ventas de hoy no están completas). El personalizado no puede incluir hoy ni fechas futuras y no puede superar 3 años.
- **Precisión del modelo**: los períodos relativos terminan **el día anterior al período evaluado**. Por ejemplo, con 14 días de evaluación hasta ayer, «Últimos 3 meses» son los 3 meses anteriores a esos 14 días. En el personalizado, la fecha Hasta es el final del entrenamiento y se evalúan los días siguientes, que deben terminar antes de hoy. Así los días evaluados nunca forman parte del entrenamiento.
- La selección se aplica de verdad: la consulta pide solo ese rango, la serie, el entrenamiento SSA, los indicadores y el gráfico usan solo esos días. «Historial utilizado» muestra el rango real, la cantidad de días, la opción y el origen. Si el producto tiene información desde una fecha posterior al inicio elegido, se informa.
- Los filtros se conservan después de consultar y también cuando hay un error de validación.

## 5. Mínimo de información

| Regla | Valor |
|---|---|
| Días de entrenamiento | máx(56, 2 × ventana + 1, 2 × horizonte). Por ejemplo: 56 días para 1–28 días de horizonte, 60 para 30 y 120 para 60. |
| Días con ventas | al menos 7. |
| Continuidad | sin días sin información ni cantidades inválidas dentro del período. |
| Evaluación | el entrenamiento anterior a los días evaluados debe cumplir lo mismo. Con 14 días de evaluación se necesitan 70 días en total. |

Por eso «Último mes» (28–31 días) nunca alcanza y muestra: «El período seleccionado es demasiado corto para generar una predicción confiable… Prueba con los últimos 3 o 6 meses.». Con 3 meses se puede pronosticar hasta 46 días; para 60 días hacen falta al menos 4 meses.

Los mensajes distinguen:
- **Sin información**: no hay registros en el período.
- **Sin ventas**: hay días conocidos, todos en 0.
- **Información desde una fecha posterior**: el producto empezó a venderse después del inicio elegido.
- **Pocos días con ventas**.

## 6. Predicciones en cero

Una predicción en 0 puede ser legítima (por ejemplo, un producto que casi no se vende). Ya no se produce por el error del punto 1, y no se fuerzan valores positivos. Si todos los días estimados son 0, se muestra como observación, no como error: «La demanda estimada para este período es cero. Esto no significa que el producto no vaya a venderse…». En la evaluación, si SSA estimó 0 habiendo ventas, se indica explícitamente.

## 7. Demanda intermitente

`DemandProfileRules` calcula, con el historial de entrenamiento: días con ventas, porcentaje de días sin ventas, total, promedio diario, promedio de los días con venta y días seguidos sin ventas al final.

| Clasificación | Regla (configurable) |
|---|---|
| Ventas frecuentes | ventas en al menos el 60 % de los días (`FrequentSalesMinShare`). |
| Ventas moderadas | entre el 25 % y el 60 % (`ModerateSalesMinShare`). |
| Ventas esporádicas | menos del 25 %. Se muestra «Este producto presenta ventas poco frecuentes…». |
| Historial insuficiente | el historial no cumple el mínimo de la sección 5. |

Si no hubo ventas en los últimos 28 días (`InactiveDaysWarning`), se avisa que el producto pudo dejar de venderse y que la estimación puede ser mayor que la demanda real.

No se incorporó una clasificación de confiabilidad alta, media o baja: no hay evidencia suficiente para asignarla de forma fundamentada en cada consulta. La precisión se revisa en la pantalla de evaluación.

## 8. Rendimiento

| Cambio | Detalle |
|---|---|
| Paginación | Historial de ventas, historial de movimientos, entradas y salidas: 50 registros por página, con «Anterior / Siguiente» y los filtros conservados. Se consulta el total con `COUNT` y solo las filas de la página (`OFFSET/FETCH`). Si la página pedida no existe, se muestra la última. |
| Totales en la base de datos | Las sumas de entradas y salidas del historial filtrado por producto se calculan con `GROUP BY` en SQL, para que sigan siendo correctas aunque no se vean todas las filas. |
| Ventas por página | Se eligen primero las ventas de la página y después se cargan sus detalles (`AsSplitQuery`). |
| Primera venta del producto | Consulta `MIN` sobre `SaleDetails.ProductId` (usa el índice existente). |
| Índices | Se revisaron: ya existen en `Sales.Date`, `SaleDetails.ProductId`, `InventoryMovements (ProductId, Date)` e `InventoryMovements.Date`. No hizo falta una migración. |
| SSA | Entrenar con 2 años y pronosticar 60 días tarda unos 20 ms; no se agregó caché. |

Medición con los mismos datos (7.600 ventas y sus movimientos, SQLite en memoria):

| Página | Antes | Ahora |
|---|---|---|
| Historial de ventas | 543 ms · 10.534 KB | 13 ms · 82 KB |
| Historial de movimientos | 141 ms · 8.635 KB | 7 ms · 70 KB |
| Salidas | 125 ms · 6.292 KB | 5 ms · 52 KB |

Sobre la `TaskCanceledException` en `ToListAsync()`: las consultas reciben el `CancellationToken` de la petición. Si el navegador cancela la carga (por ejemplo, al cambiar de página antes de que termine), ASP.NET Core cancela la consulta y se registra esa excepción. Es esperable y no indica un fallo de datos. Con la paginación, esas consultas ahora son mucho más cortas.

## 9. Métricas de evaluación

Con e = real − estimado y n días evaluados:
- MAE = Σ|e| ÷ n.
- RMSE = √(Σe² ÷ n).
- WAPE = Σ|e| ÷ Σ|real| × 100 (no disponible si no hubo ventas).

SSA y la referencia (repite el último valor del entrenamiento) se comparan sobre exactamente las mismas fechas. Se considera empate si la diferencia de MAE es menor que 0,005. El empate y los resultados peores se informan tal cual. Se muestra «Durante este período, el modelo no consiguió anticipar adecuadamente las ventas registradas» si SSA no supera a la referencia, si su WAPE llega al 100 % o si estimó 0 habiendo ventas.

Además se muestra **lo vendido y el total estimado** por cada modelo. En productos con muchos días sin ventas, el MAE premia estimar 0. Por ejemplo, en un producto de baja rotación con 30 días evaluados se vendieron 12 unidades: SSA estimó 6,66 y la referencia 0; aun así, la referencia tuvo menor MAE. El total ayuda a no descartar el modelo solo por esa métrica.

Resultados de la comparación de ventanas (datos simulados de demostración, 3 horizontes × 4 historiales por producto):

| Producto | SSA mejor | Referencia mejor | Empate | No evaluable |
|---|---|---|---|---|
| Alta rotación (ALI-0001) | 11 | 1 | 0 | 0 |
| Rotación media (BEB-0002) | 8 | 4 | 0 | 0 |
| Baja rotación (LIM-0003) | 4 | 8 | 0 | 0 |
| Decreciente (ALI-0006) | 4 | 8 | 0 | 0 |
| Intermitente, 3 % de días con ventas (TEC-0005) | 0 | 5 | 1 | 6 |

En estos datos, cambiar la ventana de entrenamiento movió poco las métricas en los productos estables. En los productos que cambian con el tiempo conviene comparar 3, 6 y 12 meses en la pantalla de precisión antes de decidir.

## 10. Limitaciones que siguen existiendo

- SSA modela tendencia y ciclos; con ventas muy esporádicas (menos del 5 % de los días) sus estimaciones diarias son poco útiles y pueden quedar cerca de 0. El sistema lo informa, pero no lo resuelve otro modelo (no se agregaron modelos adicionales por decisión del proyecto).
- El centrado usa el promedio del período elegido. Si el producto dejó de venderse, la estimación puede quedar por encima de la demanda real: por eso existe el aviso de «sin ventas recientes» y la posibilidad de entrenar con períodos más cortos.
- MAE, RMSE y WAPE diarios penalizan mucho la demanda intermitente; un buen total estimado puede convivir con un WAPE alto.
- Las pruebas de rendimiento y las funcionales se hicieron con SQLite en memoria y datos simulados; falta repetirlas con SQL Server y los datos reales del equipo.
- Las ventas anteriores a la primera venta registrada de un producto se consideran desconocidas: si el producto existía pero no se vendía, esos días no se cuentan como 0.
