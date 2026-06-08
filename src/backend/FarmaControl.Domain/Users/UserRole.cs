namespace FarmaControl.Domain.Users;

public sealed record UserRole
{
    public static readonly UserRole Admin = new("admin");
    public static readonly UserRole Gerente = new("gerente");
    public static readonly UserRole Atendente = new("atendente");
    public static readonly UserRole Medico = new("medico");
    public static readonly UserRole Enfermeira = new("enfermeira");
    public static readonly UserRole Farmaceutico = new("farmaceutico");
    public static readonly UserRole Movimentacao = new("movimentacao");
    public static readonly UserRole Entrada = new("entrada");
    public static readonly UserRole Saida = new("saida");
    public static readonly UserRole Visualizacao = new("visualizacao");

    private static readonly IReadOnlyDictionary<string, UserRole> Roles =
        BuildRoles();

    private UserRole(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static UserRole From(string value)
    {
        if (Roles.TryGetValue(value, out UserRole? role))
        {
            return role;
        }

        throw new ArgumentException("Role de usuario invalida.", nameof(value));
    }

    private static IReadOnlyDictionary<string, UserRole> BuildRoles()
    {
        var roles = new Dictionary<string, UserRole>(StringComparer.OrdinalIgnoreCase)
        {
            [Admin.Value] = Admin,
            [Gerente.Value] = Gerente,
            [Atendente.Value] = Atendente,
            ["atendimento"] = Atendente,
            [Medico.Value] = Medico,
            [Enfermeira.Value] = Enfermeira,
            ["enfermagem"] = Enfermeira,
            ["enfermeiro"] = Enfermeira,
            [Farmaceutico.Value] = Farmaceutico,
            [Movimentacao.Value] = Movimentacao,
            [Entrada.Value] = Entrada,
            [Saida.Value] = Saida,
            [Visualizacao.Value] = Visualizacao
        };

        return roles;
    }

    public override string ToString()
    {
        return Value;
    }
}
