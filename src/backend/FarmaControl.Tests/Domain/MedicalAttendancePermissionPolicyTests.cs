using FarmaControl.Domain.Care;
using FarmaControl.Domain.Users;

namespace FarmaControl.Tests.Domain;

public sealed class MedicalAttendancePermissionPolicyTests
{
    [Theory]
    [MemberData(nameof(CareTeamRoles))]
    public void CareTeam_CanEditInitialDataVitalSignsAndClinicalHistory(UserRole role)
    {
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(role, MedicalAttendanceSection.InitialData));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(role, MedicalAttendanceSection.VitalSigns));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(role, MedicalAttendanceSection.ClinicalHistory));
    }

    [Fact]
    public void OnlyPhysician_CanEditPhysicalExam()
    {
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Admin, MedicalAttendanceSection.PhysicalExam));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Gerente, MedicalAttendanceSection.PhysicalExam));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Medico, MedicalAttendanceSection.PhysicalExam));
        Assert.False(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Enfermeira, MedicalAttendanceSection.PhysicalExam));
        Assert.False(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Farmaceutico, MedicalAttendanceSection.PhysicalExam));
    }

    [Fact]
    public void OnlyPhysician_CanEditPrescription()
    {
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Admin, MedicalAttendanceSection.Prescription));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Gerente, MedicalAttendanceSection.Prescription));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Medico, MedicalAttendanceSection.Prescription));
        Assert.False(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Enfermeira, MedicalAttendanceSection.Prescription));
        Assert.False(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Farmaceutico, MedicalAttendanceSection.Prescription));
    }

    [Fact]
    public void OutputRoles_CanEditDispensation()
    {
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Admin, MedicalAttendanceSection.Dispensation));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Gerente, MedicalAttendanceSection.Dispensation));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Movimentacao, MedicalAttendanceSection.Dispensation));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Saida, MedicalAttendanceSection.Dispensation));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Farmaceutico, MedicalAttendanceSection.Dispensation));
        Assert.False(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Enfermeira, MedicalAttendanceSection.Dispensation));
        Assert.False(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Medico, MedicalAttendanceSection.Dispensation));
    }

    [Fact]
    public void Nurse_CanEditNursingCheck()
    {
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Admin, MedicalAttendanceSection.NursingCheck));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Gerente, MedicalAttendanceSection.NursingCheck));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Enfermeira, MedicalAttendanceSection.NursingCheck));
        Assert.False(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Medico, MedicalAttendanceSection.NursingCheck));
        Assert.False(MedicalAttendancePermissionPolicy.CanEdit(UserRole.Farmaceutico, MedicalAttendanceSection.NursingCheck));
    }

    [Theory]
    [MemberData(nameof(AdministrativeRoles))]
    public void AdministrativeRoles_CanEditLegacyCareTeamSections(UserRole role)
    {
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(role, MedicalAttendanceSection.VitalSigns));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(role, MedicalAttendanceSection.PhysicalExam));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(role, MedicalAttendanceSection.Prescription));
        Assert.True(MedicalAttendancePermissionPolicy.CanEdit(role, MedicalAttendanceSection.Dispensation));
    }

    [Fact]
    public void EnsureCanEdit_ThrowsForUnauthorizedRole()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MedicalAttendancePermissionPolicy.EnsureCanEdit(
                UserRole.Farmaceutico,
                MedicalAttendanceSection.Prescription));
    }

    public static TheoryData<UserRole> CareTeamRoles()
    {
        return
        [
            UserRole.Medico,
            UserRole.Enfermeira,
            UserRole.Farmaceutico,
            UserRole.Admin,
            UserRole.Gerente
        ];
    }

    public static TheoryData<UserRole> AdministrativeRoles()
    {
        return
        [
            UserRole.Admin,
            UserRole.Gerente
        ];
    }
}
