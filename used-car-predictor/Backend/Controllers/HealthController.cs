using Microsoft.AspNetCore.Mvc;
using used_car_predictor.Backend.Services;

namespace used_car_predictor.Backend.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromServices] ActiveModel active)
        => active.IsLoaded
            ? Ok(new { status = "ok", trainedAt = active.TrainedAt })
            : StatusCode(503);
}