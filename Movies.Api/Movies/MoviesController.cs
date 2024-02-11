using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Movies.Api.Contracts.Requests;
using Movies.Api.Identity;
using Movies.Api.Mapping;

namespace Movies.Api.Movies;

[Route("movies")]
[ApiController]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MoviesController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [Authorize]
    [HttpPost]
    public IActionResult Create([FromBody]CreateMovieRequest request)
    {
        var movie = request.MapToMovie();
        var result = _movieService.Create(movie);
        return result.Match<IActionResult>(
            _ => CreatedAtAction(nameof(Get), new { idOrSlug = movie.Id }, movie.MapToResponse()),
            failed => BadRequest(failed.MapToResponse()));
    }

    
    [HttpGet("{idOrSlug}")]
    public IActionResult Get([FromRoute] string idOrSlug)
    {
        var result = Guid.TryParse(idOrSlug, out var id)
            ? _movieService.GetById(id)
            : _movieService.GetBySlug(idOrSlug);

        return result.Match<IActionResult>(
            movie => Ok(movie.MapToResponse()),
            _ => NotFound());
    }

    [RequiresClaim(IdentityData.AdminUserClaimName, "true")]
    [HttpGet]
    public IActionResult GetAll()
    {
        var movies = _movieService.GetAll();
        var moviesResponse = movies.MapToResponse();
        return Ok(moviesResponse);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public IActionResult Update([FromRoute]Guid id,
        [FromBody]UpdateMovieRequest request)
    {
        var movie = request.MapToMovie(id);
        var result = _movieService.Update(movie);

        return result.Match<IActionResult>(
            m => Ok(m.MapToResponse()),
            _ => NotFound(),
            failed => BadRequest(failed.MapToResponse()));
    }

    [Authorize]
    [RequiresClaim(IdentityData.AdminUserClaimName, "true")]
    [HttpDelete("{id:guid}")]
    public IActionResult Delete([FromRoute] Guid id)
    {
        var result = _movieService.DeleteById(id);
        return result.Match<IActionResult>(
            _ => Ok(),
            _ => NotFound());
    }
}
