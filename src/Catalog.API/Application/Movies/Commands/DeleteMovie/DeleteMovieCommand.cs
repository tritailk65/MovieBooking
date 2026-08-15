namespace Catalog.API.Application.Movies.Commands.DeleteMovie;

public record DeleteMovieCommand(int Id) : IRequest<bool>; // Trả về true / false

//Contructor for api endpoint
