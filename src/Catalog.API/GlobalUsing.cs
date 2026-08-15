global using Microsoft.EntityFrameworkCore;

global using System.ComponentModel.DataAnnotations;
global using Catalog.API.Domain.Enum;

global using Catalog.API.Domain.Entities;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;

global using Catalog.API.Infrastucture;
global using Catalog.API.Extensions;
global using ServiceDefaults;
global using Catalog.API.Application.Moviess.Commands.CreateMovie;
global using FluentValidation;

global using MediatR;
global using Catalog.API.Application.Movies.Commands.CreateMovie;
global using Catalog.API.Application.Movies.Commands.UpdateMovie;
global using Catalog.API.Application.Movies.Commands.DeleteMovie;

global using System.ComponentModel;
global using Catalog.APi.Application.Movies.Queries.GetMovies;
global using EventBus.Events;

global using Catalog.API.IntegrationEvents;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Options;
global using Catalog.API.Application.Showtimes.Commands.CreateShowtime;