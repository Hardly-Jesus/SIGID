# EP-05 — Análisis y predicción de demanda: modelo SSA, evaluación y procedimientos

Este documento explica cómo funciona la EP-05 en SIGID (HU-17 a HU-21), con la optimización posterior incluida (los cambios y su justificación están en `EP-05_Optimizacion.md`): de dónde salen los datos, cómo se prepara la serie, cómo se entrena el modelo SSA de ML.NET, cómo se evalúa y qué limitaciones tiene. También incluye los procedimientos para usarla durante el desarrollo.

## 1. Recorrido de los datos

```
Ventas reales (Sales + SaleDetails)  ─┐
                                      ├─► DemandHistoryService ─► serie diaria ─┬─► SalesAnalysisService (HU-18)
Datos de prueba (SyntheticSales*)   ──┘   (un origen por consulta)              ├─► DemandForecastService (HU-19/20) ─► SsaDemandForecastEngine (ML.NET)
                                                                                └─► DemandForecastEvaluationService (HU-21) ─► SSA + modelo de referencia
```

- Los controladores solo reciben filtros y llaman a los servicios. Ninguno accede al `DbContext` ni entrena el modelo.
- El modelo SSA vive en `SIGID.Infrastructure/Forecasting/SsaDemandForecastEngine.cs` detrás de la interfaz `IDemandForecastEngine` (Application). Así la lógica de negocio no depende de ML.NET y se puede probar con un motor simulado.
- Ninguna consulta modifica ventas, existencias ni movimientos. No hay reposiciones automáticas.

## 2. Datos de prueba (HU-17)

### 2.1 Dónde se guardan

| Tabla | Contenido |
|---|---|
| `SyntheticSalesBatches` | Un registro por lote: fecha de creación, período, patrón, semilla, productos (`ProductIds`), cantidad de registros y una clave única con los parámetros (`ParametersKey`). |
| `SyntheticSalesRecords` | Producto, día y cantidad vendida (> 0). Índice único (lote, producto, día). Se borran en cascada con su lote. |

Se eligieron tablas propias para que los datos de prueba **nunca** aparezcan en el historial de ventas, el dashboard ni las existencias: no se crean ventas, no se cambian existencias y no se registran movimientos.

### 2.2 Cómo se generan

Para cada producto y día del período:

```
cantidad = redondear( nivel_base × factor_patrón × factor_fin_de_semana × (1 + ruido) )
```

| Elemento | Valor |
|---|---|
| Nivel base diario | Según la unidad de medida: `CAJA` 1–3, `KG`/`LT` 2–8, otras 3–12 (se venden menos cajas que unidades sueltas). |
| Patrón **Estable** | factor 1 |
| Patrón **Creciente** | de 0,6 al inicio a 1,4 al final del período |
| Patrón **Decreciente** | de 1,4 a 0,6 |
| Patrón **Estacional** | 1 + 0,4 × sen(2π × día del año / 365,25 + fase propia del producto) |
| Fin de semana | factor entre 1,0 y 1,3 (propio de cada producto) |
| Ruido diario | ±15 % a ±35 % (propio de cada producto) |

- Las cantidades son **enteras**, como en las ventas reales. Un día con resultado 0 no se guarda: dentro del período del lote significa «sin ventas».
- Cada producto usa su propio generador `Random(semilla × 7919 + Id del producto)`. Con la misma semilla, productos y fechas se obtienen siempre los mismos datos; con otra semilla, datos distintos.
- Controles: fechas obligatorias, inicial ≤ final, sin fechas futuras, máximo 731 días (24 meses), 1 a 20 productos existentes, semilla ≥ 0, máximo 15 000 registros por lote. Se rechaza un lote con los mismos parámetros y también un lote que se superponga en fechas con otro del mismo producto (se sumaría dos veces).
- El lote y sus registros se guardan con un único `SaveChanges`, es decir, en una sola transacción: si algo falla no queda nada a medias.

### 2.3 Cómo habilitar la herramienta (solo desarrollo)

La generación está **deshabilitada por defecto** (`SyntheticData:Enabled = false` en `appsettings.json`). Además, `Program.cs` la deja apagada si el entorno no es `Development`, aunque la configuración diga lo contrario.

```bash
dotnet user-secrets set "SyntheticData:Enabled" "true" --project src/SIGID.Web
```

Con eso aparece **Demanda → Datos de prueba** (`/SyntheticData`), que exige sesión iniciada y token antiforgery. Si está deshabilitada, la ruta responde 404. Para volver a apagarla:

```bash
dotnet user-secrets remove "SyntheticData:Enabled" --project src/SIGID.Web
```

Procedimiento sugerido: generar un lote de 12 meses con patrón Estacional para dos productos y otro de 24 meses con patrón Creciente para un tercero; luego consultar Análisis, Predicción y Precisión con el origen «Datos de prueba». Un lote se puede eliminar desde la misma pantalla.

> Consultar datos de prueba (análisis, predicción, evaluación) siempre está disponible para el administrador; lo restringido es **generarlos**. Todas las pantallas muestran el aviso «Estás utilizando datos de prueba. Estos resultados no representan ventas reales.».

## 3. Preparación de la serie diaria

`DemandHistoryService.GetDailySeriesAsync(producto, origen, desde, hasta)`:

1. Consulta las cantidades **de un solo origen** (reales o de prueba), sumadas por día y solo del producto pedido. En una venta con varios productos solo cuenta la línea de ese producto.
2. Determina los días con información conocida:
   - **Ventas reales**: desde la primera venta registrada **del producto** hasta hoy. Supuesto: desde entonces todas sus ventas se registran en el sistema, así que un día sin ventas es un 0 confirmado. Los días anteriores son desconocidos y no se rellenan con 0.
   - **Datos de prueba**: los períodos de los lotes que incluyen al producto.
3. Recorre día por día en orden: un día conocido sin registros vale **0**; un día **sin información** (por ejemplo, entre dos lotes) **no se convierte en 0**: se cuenta aparte (`UnknownDayCount`). Una cantidad negativa se descarta y se cuenta como inválida.

El resultado está ordenado, sin días repetidos y con frecuencia diaria uniforme.

## 4. Análisis histórico (HU-18)

- Agrupación por día, semana (de lunes a domingo) o mes (desde el día 1).
- Promedio = total de los períodos ÷ cantidad de períodos, **incluidos los períodos sin ventas**.
- Una semana o un mes incompleto (porque el rango elegido o la información disponible no lo cubre entero) se muestra marcado como «Período incompleto». Si hay períodos completos, el promedio, el máximo, el mínimo y la tendencia se calculan solo con ellos para no distorsionarlos; el total incluye todo.
- **Tendencia**: recta de regresión lineal sobre las cantidades de cada período. Cambio relativo = pendiente × (n − 1) ÷ promedio. ≥ +10 % creciente, ≤ −10 % decreciente, lo demás estable. Se necesitan al menos 3 períodos y alguna venta; si no, «Sin datos suficientes». Describe el pasado, no es una predicción.
- Sin ventas en el período no se muestran indicadores (un promedio o una tendencia en 0 serían engañosos).

## 5. Modelo predictivo SSA (HU-19)

### 5.1 Algoritmo

*Singular Spectrum Analysis* descompone la serie en componentes (tendencia, ciclos como el semanal y ruido) a partir de una matriz de trayectorias de `WindowSize` columnas, y pronostica proyectando los componentes principales. Se usa `MLContext.Forecasting.ForecastBySsa` de **ML.NET 4.0.3** (`Microsoft.ML` + `Microsoft.ML.TimeSeries`) y `CreateTimeSeriesEngine(...).Predict()`.

### 5.2 Parámetros (sección `DemandForecast` de appsettings.json)

| Parámetro | Valor | Motivo |
|---|---|---|
| `WindowSize` | 7 | Ventana con menos de 180 días de entrenamiento: una semana, detecta el ciclo semanal. |
| `LongWindowSize` / `LongWindowMinTrainingDays` | 14 / 180 | Con 6 meses o más se usan dos semanas: en las pruebas dio errores iguales o menores. |
| `SeriesLength` | 30 | Memoria mínima del modelo; siempre se usa al menos el doble de la ventana. |
| `MinimumTrainingDays` | 56 | 8 semanas: con un mes de datos los errores fueron claramente mayores. |
| `MinimumHistoryToHorizonRatio` | 2 | El historial debe ser al menos el doble del horizonte. Regla efectiva: máx(56, 2 × ventana + 1, 2 × horizonte). |
| `MinimumDaysWithSales` | 7 | Evita entrenar con series formadas casi solo por ceros. |
| `MaxTrainingDays` | 1096 | Máximo de la opción «Todo el historial» (3 años). |
| `ConfidenceLevel` | 0,95 | Nivel de los límites de predicción que calcula SSA. |
| `MinHorizonDays` / `MaxHorizonDays` / `DefaultHorizonDays` | 1 / 60 / 14 | Horizonte de predicción en días. |
| `MinEvaluationDays` / `MaxEvaluationDays` / `DefaultEvaluationDays` | 7 / 30 / 14 | Días reservados para evaluar (HU-21). |
| `FrequentSalesMinShare` / `ModerateSalesMinShare` | 0,60 / 0,25 | Clasificación del comportamiento de las ventas. |
| `InactiveDaysWarning` | 28 | Días sin ventas al final del historial para avisar que el producto pudo dejar de venderse. |
| `RecentHistoryDays` | 90 | Días del entrenamiento que se dibujan junto al pronóstico. |
| `MaxAnalysisDays` | 1096 | Rango máximo de una consulta de análisis (3 años). |

`DemandForecastOptions.Validate()` se ejecuta al iniciar y detiene la aplicación si los valores son incompatibles entre sí. El motor usa `MLContext(seed: 1)`, así que los mismos datos dan siempre el mismo resultado.

**Serie centrada.** SSA de ML.NET pronostica cada día como combinación de los últimos días y no resta el nivel medio. Si esos días fueron 0, todo el pronóstico salía en 0. Por eso el motor resta el promedio del entrenamiento antes de entrenar y lo vuelve a sumar a las estimaciones y a sus límites.

### 5.3 Pasos de una predicción

1. Se valida producto, origen, horizonte (1–60) e **Historial de entrenamiento** (último mes, 3, 6 o 12 meses, todo el historial o personalizado).
2. Historial: solo el período elegido, que termina **ayer** como máximo (las ventas de hoy todavía no están completas).
3. Se revisa que alcance (`DemandDataRules`, sección 7). Si no, se informa el motivo y **no se ejecuta el modelo**.
4. Se entrena SSA solo con ese historial: el modelo nunca ve datos posteriores.
5. El pronóstico empieza el día siguiente al último dato. Los días que ya pasaron (hoy incluido) se calculan pero **no se muestran como futuros**: la tabla empieza mañana.
6. Regla de ajuste: un valor negativo se lleva a **0** (no existe demanda negativa). Lo mismo con el límite inferior; el superior nunca queda por debajo de la estimación. Se redondea a 2 decimales.
7. Si el modelo devuelve una cantidad de valores distinta del horizonte, o algún NaN o infinito, el resultado se descarta con un mensaje genérico, sin inventar valores ni mostrar detalles técnicos.

Total estimado = suma de los días mostrados; promedio = total ÷ días.

Los límites del 95 % son los que estima SSA con sus propios supuestos; en la interfaz se presentan como «Rango estimado (no garantizado)», no como garantía de exactitud.

## 6. Evaluación de precisión (HU-21)

### 6.1 Procedimiento

1. Período evaluado: los **últimos N días** hasta ayer (7–30, por defecto 14). Con un historial personalizado, los N días que siguen a su fecha final.
2. Entrenamiento: el **Historial de entrenamiento** elegido, contado hacia atrás desde el día anterior al período evaluado. La división es cronológica (`ChronologicalSplit`), nunca aleatoria, y la serie debe estar ordenada y sin días repetidos ni saltos.
3. SSA se entrena **solo con el entrenamiento** y pronostica los N días reservados (sin fuga de información futura).
4. Modelo de referencia ingenuo: repite el último valor del entrenamiento durante los N días.
5. Se calculan las métricas de ambos sobre exactamente los mismos días.

### 6.2 Métricas

Con e = real − estimado y n días evaluados:

| Métrica | Fórmula | Lectura |
|---|---|---|
| MAE | Σ\|e\| ÷ n | Unidades que se equivoca por día, en promedio. |
| RMSE | √(Σe² ÷ n) | Como el MAE, pero castiga más los errores grandes. |
| WAPE | Σ\|e\| ÷ Σ\|real\| × 100 | Error total en % de lo vendido. **No disponible** si en el período no hubo ventas (se evita dividir entre 0). |

Las métricas rechazan secuencias de distinta longitud y valores NaN o infinitos. La predicción SSA se ajusta a ≥ 0 antes de medir, igual que en la consulta de predicciones.

**Comparación**: por MAE. Si la diferencia es menor que 0,005 se consideran equivalentes; si no, gana el de menor MAE. El resultado vale **solo para el período evaluado**: un error menor no garantiza la precisión futura. También se muestran lo vendido y el total estimado por cada modelo, porque en productos con muchos días sin ventas el MAE favorece estimar 0.

## 7. Reglas de suficiencia de datos (`DemandDataRules`)

| Estado | Condición | Qué se muestra |
|---|---|---|
| Sin historial | No hay información en el período elegido, o hay días conocidos pero ninguno con ventas (mensajes distintos). | Mensaje; sin predicción ni métricas. |
| Datos inválidos | Hay cantidades negativas o días sin información dentro del período. | Mensaje; el modelo necesita días continuos. |
| Historial insuficiente | El período elegido es más corto que el mínimo, la información empieza tarde o hay menos de 7 días con ventas. Mínimo = máx(56, 2 × ventana + 1, 2 × horizonte). | Mensaje con lo que hay, lo que falta y qué probar. |
| Predicción disponible sin evaluación suficiente | Alcanza para pronosticar, pero el tramo anterior a los días evaluados no cumple el mínimo. | Se pronostica; la evaluación informa que falta historial. |
| Evaluación disponible | El entrenamiento cumple el mínimo y el período evaluado tiene información completa. | Predicción y evaluación. |

Con los valores por defecto, evaluar 14 días requiere al menos **70 días** de historial (56 + 14).

## 8. Limitaciones conocidas

- SSA aprende patrones repetitivos (tendencia y ciclo semanal). Con historiales cortos, ventas muy esporádicas o cambios bruscos (promociones, quiebres de stock) las estimaciones pierden calidad.
- Los quiebres de stock se ven como «demanda 0» aunque hubiera demanda no atendida.
- Ventas reales: se asume que todas las ventas de un producto se registran en SIGID desde su primera venta. Los días anteriores se consideran desconocidos.
- Con ventas muy esporádicas (menos del 5 % de los días) las estimaciones diarias quedan cerca de 0; el sistema lo informa con mensajes de interpretación.
- Los resultados con datos de prueba sirven para probar el sistema, no como evidencia de precisión en condiciones reales.
- Se usa un solo modelo; la ventana se elige según la cantidad de días de entrenamiento, sin ajuste automático por producto.

## 9. Procedimientos

### 9.1 Base de datos

La migración `Sprint4DemandForecast` solo **crea** las tablas `SyntheticSalesBatches` y `SyntheticSalesRecords` (con sus índices y restricciones). No modifica ni borra nada existente.

```powershell
Update-Database -Project SIGID.Infrastructure -StartupProject SIGID.Web
```

```bash
dotnet ef database update --project src/SIGID.Infrastructure --startup-project src/SIGID.Web
```

No usar `Drop-Database`, `EnsureDeleted` ni borrar migraciones o `__EFMigrationsHistory`.

### 9.2 Pruebas automatizadas

```bash
dotnet test
```

- `SIGID.Application.Tests`: generador, serie diaria, análisis, reglas de datos, división cronológica, métricas, predicción y evaluación (con repositorios en memoria y un motor simulado).
- `SIGID.Infrastructure.Tests`: el **motor SSA real** de ML.NET (tamaño del horizonte, límites, patrón semanal, reproducibilidad, series constantes y en 0, rechazos) y las consultas EF Core de los repositorios de demanda sobre **SQLite en memoria**. SQLite se usa solo como base temporal de pruebas; no reemplaza a SQL Server.

### 9.3 Nota sobre ML.NET en Linux

En Windows (Visual Studio) no hace falta nada adicional: el paquete incluye las bibliotecas nativas. En Linux, el algoritmo SSA de ML.NET necesita la biblioteca OpenMP de Intel (`libiomp5.so`); en Ubuntu se puede instalar `libomp-dev` y crear un enlace `libiomp5.so` hacia la biblioteca de LLVM.

### 9.4 Pruebas pendientes con SQL Server

El entorno donde se desarrolló este sprint no tenía SQL Server. Quedan pendientes, para ejecutar en un equipo del equipo con SQL Server:

1. Aplicar la migración `Sprint4DemandForecast` sobre la base existente y revisar las tablas creadas.
2. Generar un lote de prueba, reiniciar la aplicación y comprobar que sigue disponible.
3. Ejecutar análisis, predicción y evaluación con ventas reales y con datos de prueba (las consultas se probaron con EF Core sobre SQLite; falta confirmar su traducción en SQL Server).
4. Intentar un lote duplicado y comprobar que el índice único lo rechaza sin dejar registros parciales.
