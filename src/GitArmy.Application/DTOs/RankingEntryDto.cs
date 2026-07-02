namespace GitArmy.Application.DTOs;

public record RankingEntryDto(
    int Position,
    string Username,
    string ProfileName,
    int SubstitutionYear,
    DateTime ProcessedAt);
