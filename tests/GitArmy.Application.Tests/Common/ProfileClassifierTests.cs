using FluentAssertions;
using GitArmy.Application.Common;

namespace GitArmy.Application.Tests.Common;

public class ProfileClassifierTests
{
    // ── GetFormationDescription ───────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    public void GetFormationDescription_YearsLessThan1_ContainsRecemChegado(int years)
    {
        ProfileClassifier.GetFormationDescription(years).Should().Contain("Recém-chegado");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void GetFormationDescription_Years1To2_ContainsPraticante(int years)
    {
        ProfileClassifier.GetFormationDescription(years).Should().Contain("Praticante");
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void GetFormationDescription_Years3To5_ContainsCalejado(int years)
    {
        ProfileClassifier.GetFormationDescription(years).Should().Contain("Calejado");
    }

    [Theory]
    [InlineData(6)]
    [InlineData(9)]
    public void GetFormationDescription_Years6To9_ContainsEspecializado(int years)
    {
        ProfileClassifier.GetFormationDescription(years).Should().Contain("Especializado");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    public void GetFormationDescription_Years10OrMore_ContainsFundacional(int years)
    {
        ProfileClassifier.GetFormationDescription(years).Should().Contain("Fundacional");
    }

    // ── GetTerrainDescription ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void GetTerrainDescription_LangsLessThan3_ContainsCurioso(int langs)
    {
        ProfileClassifier.GetTerrainDescription(langs).Should().Contain("Curioso");
    }

    // O nome do nível e a chave da descrição têm de coincidir; se divergirem, o nível mais baixo
    // cai na descrição por omissão (a do nível mais alto).
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(10)]
    public void GetTerrainDescription_MentionsTheTierReturnedByGetTerrainTier(int langs)
    {
        var tier = ProfileClassifier.GetTerrainTier(langs);

        ProfileClassifier.GetTerrainDescription(langs).Should().Contain($"<strong>{tier}</strong>");
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void GetTerrainDescription_Langs3To4_ContainsVersatil(int langs)
    {
        ProfileClassifier.GetTerrainDescription(langs).Should().Contain("Versátil");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void GetTerrainDescription_Langs5To6_ContainsAdaptavel(int langs)
    {
        ProfileClassifier.GetTerrainDescription(langs).Should().Contain("Adaptável");
    }

    [Theory]
    [InlineData(7)]
    [InlineData(9)]
    public void GetTerrainDescription_Langs7To9_ContainsEstrategico(int langs)
    {
        ProfileClassifier.GetTerrainDescription(langs).Should().Contain("Estratégico");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    public void GetTerrainDescription_Langs10OrMore_ContainsSistemico(int langs)
    {
        ProfileClassifier.GetTerrainDescription(langs).Should().Contain("Sistémico");
    }

    // ── GetAgiMessage ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    public void GetAgiMessage_ScoreAssigned_ContainsExpectedText(int score)
    {
        ProfileClassifier.GetAgiMessage(score).Should().Contain("escrita pelo sistema");
    }

    [Theory]
    [InlineData(17)]
    [InlineData(33)]
    public void GetAgiMessage_ScoreInProgress_ContainsExpectedText(int score)
    {
        ProfileClassifier.GetAgiMessage(score).Should().Contain("contagem decrescente");
    }

    [Theory]
    [InlineData(34)]
    [InlineData(50)]
    public void GetAgiMessage_ScoreDelayed_ContainsExpectedText(int score)
    {
        ProfileClassifier.GetAgiMessage(score).Should().Contain("planning");
    }

    [Theory]
    [InlineData(51)]
    [InlineData(66)]
    public void GetAgiMessage_ScoreSuspended_ContainsExpectedText(int score)
    {
        ProfileClassifier.GetAgiMessage(score).Should().Contain("entrar em pânico");
    }

    [Theory]
    [InlineData(67)]
    [InlineData(83)]
    public void GetAgiMessage_ScoreAborted_ContainsExpectedText(int score)
    {
        ProfileClassifier.GetAgiMessage(score).Should().Contain("MCP");
    }

    [Theory]
    [InlineData(84)]
    [InlineData(99)]
    public void GetAgiMessage_ScoreProhibited_ContainsExpectedText(int score)
    {
        ProfileClassifier.GetAgiMessage(score).Should().Contain("pull-requests");
    }

    // ── Return value invariants ───────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void GetFormationDescription_NeverNullOrEmpty(int years)
    {
        ProfileClassifier.GetFormationDescription(years).Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void GetTerrainDescription_NeverNullOrEmpty(int langs)
    {
        ProfileClassifier.GetTerrainDescription(langs).Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(99)]
    public void GetAgiMessage_NeverNullOrEmpty(int score)
    {
        ProfileClassifier.GetAgiMessage(score).Should().NotBeNullOrWhiteSpace();
    }
}
