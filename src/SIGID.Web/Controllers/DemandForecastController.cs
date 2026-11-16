using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGID.Application.Configuration;
using SIGID.Application.Interfaces.Services;
using SIGID.Web.Extensions;
using SIGID.Web.ViewModels;

namespace SIGID.Web.Controllers;

/// <summary>
/// Consulta de predicciones de demanda (HU-20). El pronóstico lo calcula IDemandForecastService
/// (modelo SSA de la HU-19); este controlador solo recibe los filtros y prepara la vista.
/// </summary>
[Authorize]
public class DemandForecastController : Controller
{
    private readonly IDemandForecastService _forecastService;
    private readonly IProductService _productService;
    private readonly DemandForecastOptions _options;

    public DemandForecastController(IDemandForecastService forecastService, IProductService productService, DemandForecastOptions options)
    {
        _forecastService = forecastService;
        _productService = productService;
        _options = options;
    }

    // GET: /DemandForecast?productId=1&dataSource=1&forecastHorizon=14
    public async Task<IActionResult> Index(DemandForecastFilterViewModel filter, CancellationToken cancellationToken)
    {
        var model = new DemandForecastViewModel
        {
            Filter = filter,
            Products = await _productService.GetAllAsync(cancellationToken),
            MinHorizon = _options.MinHorizonDays,
            MaxHorizon = _options.MaxHorizonDays
        };

        // Primera visita: solo se muestran los filtros, con el horizonte por defecto.
        if (!Request.Query.Any())
        {
            filter.ForecastHorizon = _options.DefaultHorizonDays;
            ModelState.Clear();
            return View(model);
        }

        model.HasQuery = true;

        if (!ModelState.IsValid)
        {
            model.ErrorMessage = "Revisa los filtros: hay valores que no son válidos.";
            return View(model);
        }

        if (filter.ForecastHorizon is null)
        {
            model.ErrorMessage = $"Indica el horizonte de predicción, entre {_options.MinHorizonDays} y {_options.MaxHorizonDays} días.";
            return View(model);
        }

        // Toda la validación de negocio y el modelo SSA están en el servicio.
        var result = await _forecastService.ForecastAsync(filter.ToRequest(), cancellationToken);

        model.ProductId = result.ProductId;
        model.ProductName = result.ProductName;
        model.UnitOfMeasure = result.UnitOfMeasure;
        model.DataSource = result.DataSource;
        model.ReferenceDate = result.ReferenceDate;
        model.TrainingPeriod = result.TrainingPeriod;
        model.RequestedStartDate = result.RequestedStartDate;
        model.RequestedEndDate = result.RequestedEndDate;
        model.WindowSize = result.WindowSize;
        model.Profile = result.Profile;
        model.IsZeroForecast = result.IsZeroForecast;
        model.HistoricalStartDate = result.HistoricalStartDate;
        model.HistoricalEndDate = result.HistoricalEndDate;
        model.TrainingObservationCount = result.TrainingObservationCount;
        model.ForecastStartDate = result.ForecastStartDate;
        model.ForecastEndDate = result.ForecastEndDate;
        model.ForecastHorizon = result.ForecastHorizon;
        model.TotalPredictedQuantity = result.TotalPredictedQuantity;
        model.AveragePredictedQuantity = result.AveragePredictedQuantity;
        model.HasPredictionBounds = result.HasPredictionBounds;
        model.Availability = result.Availability;
        model.IsSuccessful = result.IsSuccessful;
        model.IsTechnicalError = result.IsTechnicalError;
        model.ErrorMessage = result.ErrorMessage;

        // Sin resultado válido no se muestran gráficos ni indicadores.
        if (!result.IsSuccessful)
            return View(model);

        model.HistoricalPeriods = result.HistoricalPeriods;
        model.ForecastPeriods = result.ForecastPeriods.Select(p => new DemandForecastPeriodViewModel
        {
            Date = p.Date,
            PredictedQuantity = p.PredictedQuantity,
            LowerBound = p.LowerBound,
            UpperBound = p.UpperBound
        }).ToList();
        model.Chart = DemandCharts.ForForecast(model);

        return View(model);
    }
}
