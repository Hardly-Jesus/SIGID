namespace SIGID.Application.Configuration;

/// <summary>
/// Parámetros del análisis y de la predicción de demanda (sección "DemandForecast").
/// Están todos aquí para no repartir números sueltos por el código.
/// Ver docs/EP-05_Modelo_SSA.md y docs/EP-05_Optimizacion.md para la explicación de cada uno.
/// </summary>
public class DemandForecastOptions
{
    // ---------- Algoritmo SSA (ML.NET) ----------

    /// <summary>
    /// Ventana de análisis de SSA para historiales cortos, en días. 7 permite detectar el ciclo semanal.
    /// </summary>
    public int WindowSize { get; set; } = 7;

    /// <summary>
    /// Ventana para historiales largos (dos semanas): con más datos, SSA puede separar mejor
    /// el ciclo semanal del ruido. En las pruebas dio errores iguales o menores desde 6 meses.
    /// </summary>
    public int LongWindowSize { get; set; } = 14;

    /// <summary>
    /// Días de entrenamiento a partir de los cuales se usa la ventana larga.
    /// </summary>
    public int LongWindowMinTrainingDays { get; set; } = 180;

    /// <summary>
    /// Memoria mínima de SSA (observaciones recientes). Siempre se usa al menos el doble de la ventana.
    /// </summary>
    public int SeriesLength { get; set; } = 30;

    /// <summary>
    /// Días mínimos de historial para entrenar: 8 semanas, para que el ciclo semanal se repita
    /// varias veces. Con un mes de datos los errores fueron claramente mayores en las pruebas.
    /// </summary>
    public int MinimumTrainingDays { get; set; } = 56;

    /// <summary>
    /// El historial debe ser al menos este número de veces más largo que el horizonte pedido.
    /// </summary>
    public int MinimumHistoryToHorizonRatio { get; set; } = 2;

    /// <summary>
    /// Días mínimos con ventas (cantidad mayor que cero) dentro del historial.
    /// Evita entrenar con series formadas casi solo por ceros.
    /// </summary>
    public int MinimumDaysWithSales { get; set; } = 7;

    /// <summary>
    /// Máximo de días de historial usados para entrenar (opción «Todo el historial»: 3 años).
    /// </summary>
    public int MaxTrainingDays { get; set; } = 1096;

    /// <summary>
    /// Nivel de confianza de los límites de predicción que calcula SSA.
    /// </summary>
    public float ConfidenceLevel { get; set; } = 0.95f;

    // ---------- Horizonte de predicción ----------

    public int MinHorizonDays { get; set; } = 1;

    public int MaxHorizonDays { get; set; } = 60;

    public int DefaultHorizonDays { get; set; } = 14;

    // ---------- Evaluación (HU-21) ----------

    public int MinEvaluationDays { get; set; } = 7;

    public int MaxEvaluationDays { get; set; } = 30;

    public int DefaultEvaluationDays { get; set; } = 14;

    // ---------- Comportamiento de la demanda ----------

    /// <summary>
    /// Proporción mínima de días con ventas para considerar las ventas «frecuentes» (60 %).
    /// </summary>
    public double FrequentSalesMinShare { get; set; } = 0.60;

    /// <summary>
    /// Proporción mínima de días con ventas para considerar las ventas «moderadas» (25 %).
    /// Por debajo, las ventas son «esporádicas».
    /// </summary>
    public double ModerateSalesMinShare { get; set; } = 0.25;

    /// <summary>
    /// Días seguidos sin ventas al final del historial para avisar que el producto dejó de venderse.
    /// </summary>
    public int InactiveDaysWarning { get; set; } = 28;

    // ---------- Consultas ----------

    /// <summary>
    /// Días del historial de entrenamiento que se muestran en el gráfico junto al pronóstico
    /// (si el período elegido es más corto, se muestra completo).
    /// </summary>
    public int RecentHistoryDays { get; set; } = 90;

    /// <summary>
    /// Máximo de días que se pueden analizar en una consulta histórica (3 años).
    /// </summary>
    public int MaxAnalysisDays { get; set; } = 1096;

    /// <summary>
    /// Días mínimos de entrenamiento sin tener en cuenta el horizonte.
    /// </summary>
    public int RequiredTrainingDays => Math.Max(MinimumTrainingDays, 2 * LongWindowSize + 1);

    /// <summary>
    /// Días mínimos de entrenamiento para un horizonte: el mayor entre el mínimo general,
    /// lo que exige ML.NET (más del doble de la ventana) y el doble del horizonte.
    /// </summary>
    public int RequiredTrainingDaysFor(int horizon) => Math.Max(RequiredTrainingDays, MinimumHistoryToHorizonRatio * horizon);

    /// <summary>
    /// Ventana de SSA según la cantidad de días de entrenamiento.
    /// </summary>
    public int WindowSizeFor(int trainingDays) => trainingDays >= LongWindowMinTrainingDays ? LongWindowSize : WindowSize;

    /// <summary>
    /// Memoria de SSA para una ventana: al menos SeriesLength y al menos el doble de la ventana.
    /// </summary>
    public int SeriesLengthFor(int windowSize) => Math.Max(SeriesLength, 2 * windowSize);

    /// <summary>
    /// Revisa que los valores configurados sean compatibles entre sí.
    /// </summary>
    public void Validate()
    {
        if (WindowSize < 2 || LongWindowSize < WindowSize)
            throw new InvalidOperationException("DemandForecast:WindowSize debe ser 2 o mayor y LongWindowSize no puede ser menor.");
        if (SeriesLength <= WindowSize)
            throw new InvalidOperationException("DemandForecast:SeriesLength debe ser mayor que WindowSize.");
        if (MinimumTrainingDays <= 2 * WindowSize || LongWindowMinTrainingDays <= 2 * LongWindowSize)
            throw new InvalidOperationException("DemandForecast: el entrenamiento mínimo debe ser mayor que el doble de la ventana.");
        if (MinimumHistoryToHorizonRatio < 1)
            throw new InvalidOperationException("DemandForecast:MinimumHistoryToHorizonRatio debe ser 1 o mayor.");
        if (MaxTrainingDays < RequiredTrainingDaysFor(MaxHorizonDays))
            throw new InvalidOperationException("DemandForecast:MaxTrainingDays es menor que el mínimo de entrenamiento.");
        if (MinHorizonDays < 1 || MaxHorizonDays < MinHorizonDays || DefaultHorizonDays < MinHorizonDays || DefaultHorizonDays > MaxHorizonDays)
            throw new InvalidOperationException("Revise los valores de horizonte en DemandForecast.");
        if (MinEvaluationDays < 1 || MaxEvaluationDays < MinEvaluationDays || DefaultEvaluationDays < MinEvaluationDays || DefaultEvaluationDays > MaxEvaluationDays)
            throw new InvalidOperationException("Revise los valores de evaluación en DemandForecast.");
        if (ConfidenceLevel <= 0 || ConfidenceLevel >= 1)
            throw new InvalidOperationException("DemandForecast:ConfidenceLevel debe estar entre 0 y 1.");
        if (ModerateSalesMinShare <= 0 || FrequentSalesMinShare <= ModerateSalesMinShare || FrequentSalesMinShare > 1)
            throw new InvalidOperationException("Revise los umbrales de comportamiento de la demanda en DemandForecast.");
    }
}
