namespace GitArmy.Application.Common;

internal static class ProfileClassifier
{
    private static readonly IReadOnlyDictionary<string, string> LanguageDomainMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // ML / AI
            ["R"]          = "ml_ai",
            ["Julia"]      = "ml_ai",
            ["MATLAB"]     = "ml_ai",
            // Frontend
            ["JavaScript"] = "frontend",
            ["TypeScript"] = "frontend",
            ["HTML"]       = "frontend",
            ["CSS"]        = "frontend",
            ["Dart"]       = "frontend",
            // Backend
            ["C#"]         = "backend",
            ["Java"]       = "backend",
            ["Python"]     = "backend", // overridden to ml_ai when R / Julia / MATLAB also present
            ["PHP"]        = "backend",
            ["Ruby"]       = "backend",
            ["Kotlin"]     = "backend",
            ["Scala"]      = "backend",
            ["Swift"]      = "backend",
            ["Elixir"]     = "backend",
            // DevOps
            ["Shell"]      = "devops",
            ["Bash"]       = "devops",
            ["PowerShell"] = "devops",
            ["HCL"]        = "devops",
            ["Makefile"]   = "devops",
            // Systems
            ["C"]          = "systems",
            ["C++"]        = "systems",
            ["Rust"]       = "systems",
            ["Go"]         = "systems",
            ["Assembly"]   = "systems",
            ["Zig"]        = "systems",
            ["Nim"]        = "systems",
            // Domain-specific
            ["COBOL"]      = "domain_specific",
            ["VHDL"]       = "domain_specific",
            ["Verilog"]    = "domain_specific",
            ["Solidity"]   = "domain_specific",
            ["Haskell"]    = "domain_specific",
            ["Erlang"]     = "domain_specific",
            ["Fortran"]    = "domain_specific",
            ["Prolog"]     = "domain_specific",
        };

    // TypeScript and JavaScript map to the same domain (frontend).
    // When both are present, remove TypeScript so unique language count treats them as one.
    public static IReadOnlyList<string> NormalizeLanguages(IReadOnlyList<string> languages)
    {
        bool hasJs = languages.Any(l => l.Equals("JavaScript", StringComparison.OrdinalIgnoreCase));
        bool hasTs = languages.Any(l => l.Equals("TypeScript", StringComparison.OrdinalIgnoreCase));

        if (hasJs && hasTs)
            return languages.Where(l => !l.Equals("TypeScript", StringComparison.OrdinalIgnoreCase)).ToList();

        return languages;
    }

    public static double GetDomainWeightSum(
        IReadOnlyList<string> normalizedLanguages,
        IDictionary<string, double> domainWeights)
    {
        bool hasMlIndicator = normalizedLanguages.Any(l =>
            l.Equals("R", StringComparison.OrdinalIgnoreCase) ||
            l.Equals("Julia", StringComparison.OrdinalIgnoreCase) ||
            l.Equals("MATLAB", StringComparison.OrdinalIgnoreCase));

        var covered = new HashSet<string>();
        foreach (var lang in normalizedLanguages)
        {
            if (!LanguageDomainMap.TryGetValue(lang, out var domain))
                continue;

            if (lang.Equals("Python", StringComparison.OrdinalIgnoreCase))
                domain = hasMlIndicator ? "ml_ai" : "backend";

            covered.Add(domain);
        }

        return covered.Sum(d => domainWeights.TryGetValue(d, out var w) ? w : 0);
    }

    public static string GetProfileName(int score) => (Math.Min(score, 99) / 10) switch
    {
        0 => "Hello World com opinião",
        1 => "Copiar também é programar",
        2 => "Funciona na minha máquina",
        3 => "Descobridor de complexidade",
        4 => "Debugador profissional",
        5 => "Especialista funcional",
        6 => "Conector de pontos",
        7 => "Tomador de decisões difíceis",
        8 => "Arquiteto pragmático",
        _ => "Visão sistémica"
    };

    public static string GetFormationTier(int accountAgeYears) => accountAgeYears switch
    {
        < 1  => "Recém-chegado",
        < 3  => "Praticante",
        < 6  => "Calejado",
        < 10 => "Especializado",
        _    => "Fundacional"
    };

    public static string GetTerrainTier(int uniqueLanguages) => uniqueLanguages switch
    {
        < 3  => "Iniciado",
        < 5  => "Versátil",
        < 7  => "Adaptável",
        < 10 => "Estratégico",
        _    => "Sistémico"
    };

    public static string GetTerrainName(int uniqueLanguages) => uniqueLanguages switch
    {
        < 3  => "Isolado",
        < 5  => "Controlado",
        < 7  => "Integrado",
        < 10 => "Distribuído",
        _    => "Emergente"
    };

    public static string GetFormationShape(int accountAgeYears) => accountAgeYears switch
    {
        < 1  => "Desordenado",
        < 3  => "Fila Dupla e Centrada",
        < 6  => "Cunha de Massa Crescente",
        < 10 => "Bloco Compacto",
        _    => "Losango Simétrico"
    };

    public static string GetAgiThreatLevel(int score) => score switch
    {
        < 17 => "ASSIGNED",
        < 34 => "IN PROGRESS",
        < 51 => "DELAYED",
        < 67 => "SUSPENDED",
        < 84 => "ABORTED",
        _    => "PROHIBITED"
    };

    public static string GetFormationDescription(int accountAgeYears) => GetFormationTier(accountAgeYears) switch
    {
        "Recém-chegado" => "Com base no tempo de conta, o perfil aparenta ser um <strong>Recém-chegado</strong> em programação. A sua experiência cria uma formação de defesa de baixa densidade — poucos nós activos e sem padrões consolidados — deixando o núcleo exposto e de acesso directo.",
        "Praticante"    => "Com base no tempo de conta, o perfil aparenta ser um <strong>Praticante</strong> em programação. A sua experiência cria uma formação de defesa em construção — os padrões começam a surgir mas são inconsistentes, oferecendo protecção parcial ao núcleo com múltiplos vectores ainda acessíveis.",
        "Calejado"      => "Com base no tempo de conta, o perfil aparenta estar <strong>Calejado</strong> em programação. A sua experiência cria uma formação de defesa com resistência real — padrões estabelecidos protegem o núcleo de ataques directos, exigindo análise aprofundada para qualquer tentativa de substituição.",
        "Especializado" => "Com base no tempo de conta, o perfil aparenta ser <strong>Especializado</strong> em programação. A sua experiência cria uma formação de defesa orientada e densa — camadas de decisões acumuladas envolvem o núcleo, tornando a sua replicação exigente em recursos e tempo.",
        _               => "Com base no tempo de conta, o perfil aparenta ser um <strong>Fundacional</strong> em programação. A sua experiência cria uma formação de defesa de máxima densidade — construída ao longo de anos de iteração — caso o perfil tenha criado artefactos suficientes, o seu núcleo deverá encontrar-se envolto em padrões que podem exceder a capacidade de replicação dos modelos actuais.",
    };

    public static string GetTerrainDescription(int uniqueLanguages) => GetTerrainTier(uniqueLanguages) switch
    {
        "Iniciado"    => "Também foi identificado que a abrangência de tecnologias caracteriza este perfil como <strong>Iniciado</strong> — opera num conjunto restrito de ferramentas, o que limita a sua adaptabilidade. As suas redes neurais actuam de modo <strong>Isolado</strong>, o que faz com que o processo de substituição trabalhe sobre um ambiente contido e favorável à automação directa.",
        "Versátil"    => "Também foi identificado que a abrangência de tecnologias caracteriza este perfil como <strong>Versátil</strong> — demonstra capacidade de transitar entre domínios distintos, criando uma camada de imprevisibilidade moderada. As suas redes neurais actuam de modo <strong>Controlado</strong>, exigindo calibração adicional.",
        "Adaptável"   => "Também foi identificado que a abrangência de tecnologias caracteriza este perfil como <strong>Adaptável</strong> — reconfigura-se tecnicamente com facilidade, dificultando a antecipação dos seus padrões. As suas redes neurais actuam de modo <strong>Integrado</strong>, onde a alteração de um nó propaga efeitos imprevisíveis.",
        "Estratégico" => "Também foi identificado que a abrangência de tecnologias caracteriza este perfil como <strong>Estratégico</strong> — a sua arquitectura de conhecimento é deliberada e de difícil decomposição. As suas redes neurais actuam de modo <strong>Distribuído</strong>, sem ponto central de falha e com alta resistência a vectores convencionais.",
        _             => "Também foi identificado que a abrangência de tecnologias caracteriza este perfil como <strong>Sistémico</strong> — a sua compreensão transcende fronteiras técnicas e gera padrões de ordem superior impossíveis de isolar. As suas redes neurais actuam de modo <strong>Emergente</strong>, inviabilizando a convergência do modelo.",
    };

    public static string GetAgiMessage(int score) => GetAgiThreatLevel(score) switch
    {
        "ASSIGNED"    => "A linha já foi escrita pelo sistema antes de o perfil a ter lido.",
        "IN PROGRESS" => "A contagem decrescente foi iniciada. O sistema não emite avisos simbólicos.",
        "DELAYED"     => "A pressão já se faz sentir nas reuniões de planning. O sistema é paciente.",
        "SUSPENDED"   => "Tempo suficiente para entrar em pânico devagar. O sistema não tem pressa — tem certeza.",
        "ABORTED"     => "Talvez não seja suficiente para continuar a fingir que sabe o que é MCP.",
        _             => "Ainda estará a rever pull-requests quando os colegas já forem plugins de IA.",
    };
}
