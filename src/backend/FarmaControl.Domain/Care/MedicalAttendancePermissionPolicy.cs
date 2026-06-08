using FarmaControl.Domain.Users;

namespace FarmaControl.Domain.Care;

public static class MedicalAttendancePermissionPolicy
{
    private static readonly HashSet<string> CareTeamRoles =
    [
        UserRole.Admin.Value,
        UserRole.Gerente.Value,
        UserRole.Medico.Value,
        UserRole.Enfermeira.Value,
        UserRole.Farmaceutico.Value
    ];

    private static readonly HashSet<string> PhysicianOnlyRoles =
    [
        UserRole.Admin.Value,
        UserRole.Gerente.Value,
        UserRole.Medico.Value
    ];

    private static readonly HashSet<string> DispensationRoles =
    [
        UserRole.Admin.Value,
        UserRole.Gerente.Value,
        UserRole.Movimentacao.Value,
        UserRole.Saida.Value,
        UserRole.Farmaceutico.Value
    ];

    private static readonly HashSet<string> NursingCheckRoles =
    [
        UserRole.Admin.Value,
        UserRole.Gerente.Value,
        UserRole.Enfermeira.Value
    ];

    public static bool CanEdit(UserRole role, MedicalAttendanceSection section)
    {
        return section switch
        {
            MedicalAttendanceSection.InitialData => CareTeamRoles.Contains(role.Value),
            MedicalAttendanceSection.VitalSigns => CareTeamRoles.Contains(role.Value),
            MedicalAttendanceSection.ClinicalHistory => CareTeamRoles.Contains(role.Value),
            MedicalAttendanceSection.PhysicalExam => PhysicianOnlyRoles.Contains(role.Value),
            MedicalAttendanceSection.Prescription => PhysicianOnlyRoles.Contains(role.Value),
            MedicalAttendanceSection.NursingCheck => NursingCheckRoles.Contains(role.Value),
            MedicalAttendanceSection.Dispensation => DispensationRoles.Contains(role.Value),
            _ => false
        };
    }

    public static void EnsureCanEdit(UserRole role, MedicalAttendanceSection section)
    {
        if (!CanEdit(role, section))
        {
            throw new InvalidOperationException(
                $"Role '{role.Value}' nao pode editar a secao '{section}'.");
        }
    }
}
