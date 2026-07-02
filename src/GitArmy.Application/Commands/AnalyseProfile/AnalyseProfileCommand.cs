using GitArmy.Application.DTOs;
using MediatR;

namespace GitArmy.Application.Commands.AnalyseProfile;

public record AnalyseProfileCommand(string Username) : IRequest<ProfileDto>;
