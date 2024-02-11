using System.Collections.Concurrent;
using FluentValidation;
using FluentValidation.Results;
using OneOf;
using OneOf.Types;

namespace Movies.Api.Movies;

public class MovieService : IMovieService
{
    private readonly IValidator<Movie> _movieValidator;
    private readonly ConcurrentDictionary<Guid, Movie> _movies = new();
    private readonly ConcurrentDictionary<string, Guid> _slugToIdJoin = new();

    public MovieService(IValidator<Movie> movieValidator)
    {
        _movieValidator = movieValidator;
    }

    public OneOf<Success, ValidationFailed> Create(Movie movie)
    {
        var validationResult = _movieValidator.Validate(movie);
        if (!validationResult.IsValid)
        {
            return new ValidationFailed(validationResult.Errors);
        }

        if (_slugToIdJoin.ContainsKey(movie.Slug))
        {
            var error = new ValidationFailure("Slug", "This movie already exists in the system");
            return new ValidationFailed(error);
        }

        _slugToIdJoin[movie.Slug] = movie.Id;
        _movies[movie.Id] = movie;
        return new Success();
    }

    public OneOf<Movie, None> GetById(Guid id)
    {
        var movie = _movies.GetValueOrDefault(id);
        if (movie is null)
        {
            return new None();
        }

        return movie;
    }

    public OneOf<Movie, None> GetBySlug(string slug)
    {
        var id = _slugToIdJoin.GetValueOrDefault(slug);
        if (id == Guid.Empty)
        {
            return new None();
        }

        var movie = _movies.GetValueOrDefault(id);
        if (movie is null)
        {
            return new None();
        }

        return movie;
    }

    public IEnumerable<Movie> GetAll()
    {
        return _movies.Values;
    }

    public OneOf<Movie, NotFound, ValidationFailed> Update(Movie movie)
    {
        var validationResult = _movieValidator.Validate(movie);
        if (!validationResult.IsValid)
        {
            return new ValidationFailed(validationResult.Errors);
        }
        
        var movieExists = _movies.ContainsKey(movie.Id);
        if (!movieExists)
        {
            return new NotFound();
        }

        var oldSlug = _movies[movie.Id].Slug;
        _slugToIdJoin.Remove(oldSlug, out _);

        _slugToIdJoin[movie.Slug] = movie.Id;
        _movies[movie.Id] = movie;
        return movie;
    }

    public OneOf<Success, NotFound> DeleteById(Guid id)
    {
        _movies.Remove(id, out var movie);
        if (movie is null)
        {
            return new NotFound();
        }

        _slugToIdJoin.Remove(movie.Slug, out _);
        return new Success();
    }
}

public interface IMovieService
{
    OneOf<Success, ValidationFailed> Create(Movie movie);

    OneOf<Movie, None> GetById(Guid id);

    OneOf<Movie, None> GetBySlug(string slug);

    IEnumerable<Movie> GetAll();

    OneOf<Movie, NotFound, ValidationFailed> Update(Movie movie);

    OneOf<Success, NotFound> DeleteById(Guid id);
}
