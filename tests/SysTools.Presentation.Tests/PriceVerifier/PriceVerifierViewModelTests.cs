using SysTools.Presentation.Modules.PriceVerifier.ViewModels;

namespace SysTools.Presentation.Tests.PriceVerifier;

public sealed class PriceVerifierViewModelTests
{
    [Fact]
    public void Exposes_neutral_read_only_placeholder_state()
    {
        var vm = new PriceVerifierViewModel();
        Assert.False(string.IsNullOrWhiteSpace(vm.ModuleTitle));
        Assert.Equal(string.Empty, vm.Barcode);
        Assert.Equal(string.Empty, vm.AdditionalInformation);
        Assert.All(new[] { vm.ProductDescription, vm.ProductPresentation, vm.StockDisplay, vm.FinalPriceDisplay }, value => Assert.Equal("—", value));
        Assert.False(vm.IsBarcodeAvailable || vm.IsAdditionalInformationAvailable || vm.IsSearchAvailable || vm.IsPrintAvailable || vm.IsSettingsAvailable);
        Assert.Null(typeof(PriceVerifierViewModel).GetProperty(nameof(vm.Barcode))!.SetMethod);
    }
}
