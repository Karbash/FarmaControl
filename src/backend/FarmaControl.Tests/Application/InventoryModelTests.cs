using FarmaControl.Application.Inventory.Models;
using FarmaControl.Domain.Inventory;

namespace FarmaControl.Tests.Application;

public sealed class InventoryModelTests
{
    [Fact]
    public void MedicationInputModel_RequiresGenericOrCommercialName()
    {
        var model = ValidMedicationModel() with
        {
            GenericName = null,
            CommercialName = null
        };

        var errors = model.Validate();

        Assert.Contains(errors, error => error.Message == "Informe nome generico ou nome comercial.");
    }

    [Fact]
    public void MedicationInputModel_RejectsNegativeQuantity()
    {
        var model = ValidMedicationModel() with { Quantity = -1 };

        var errors = model.Validate();

        Assert.Contains(errors, error => error.Message == "Quantidade nao pode ser negativa.");
    }

    [Fact]
    public void MedicationInputModel_ConvertsToDomain()
    {
        Medication medication = ValidMedicationModel().ToDomain("Doacao", "Fabricante", "Farmacia");

        Assert.Equal("Dipirona", medication.GenericName);
        Assert.Equal(10, medication.Quantity);
        Assert.False(medication.IsControlled);
        Assert.Equal(1, medication.LocationId);
    }

    [Fact]
    public void CreateStockLocationModel_RequiresName()
    {
        var model = new CreateStockLocationModel("");

        var errors = model.Validate();

        Assert.Contains(errors, error => error.Message == "Nome e obrigatorio.");
    }

    private static MedicationInputModel ValidMedicationModel()
    {
        return new MedicationInputModel(
            "Dipirona",
            null,
            "Analgesico",
            "Comprimido",
            "500mg",
            null,
            "Doacao",
            null,
            "Responsavel",
            "Fabricante",
            null,
            "L1",
            null,
            10,
            "un",
            "Farmacia",
            1,
            5,
            false);
    }
}
