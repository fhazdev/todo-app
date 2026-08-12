using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sprout.Api.Controllers;

/// <summary>
/// The shared shape of every Sprout controller: authenticated by default, routed
/// under /api, and holding nothing but a mediator. Controllers translate HTTP to a
/// request object and back; all behaviour lives in the Application layer.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase(ISender mediator) : ControllerBase
{
    protected ISender Mediator { get; } = mediator;
}
