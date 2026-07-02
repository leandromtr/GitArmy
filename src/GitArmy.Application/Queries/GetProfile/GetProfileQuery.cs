using GitArmy.Application.DTOs;
using MediatR;

namespace GitArmy.Application.Queries.GetProfile;

public record GetProfileQuery(string Username) : IRequest<ProfileDto?>;
