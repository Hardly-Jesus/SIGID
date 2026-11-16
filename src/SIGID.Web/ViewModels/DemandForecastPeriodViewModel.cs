namespace SIGID.Web.ViewModels;

/// <summary>
/// Un día pronosticado, tal como lo devuelve el servicio predictivo.
/// </summary>
public class DemandForecastPeriodViewModel
{
    public DateTime Date { get; set; }

    public double PredictedQuantity { get; set; }

    public double? LowerBound { get; set; }

    public double? UpperBound { get; set; }
}
