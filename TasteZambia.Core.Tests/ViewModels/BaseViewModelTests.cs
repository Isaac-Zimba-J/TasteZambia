using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class FakeNavigation : INavigationService
{
    public List<string> Routes { get; } = [];
    public int BackCount { get; private set; }

    public Task GoToAsync(string route) { Routes.Add(route); return Task.CompletedTask; }
    public Task GoToAsync(string route, IDictionary<string, object> parameters) { Routes.Add(route); return Task.CompletedTask; }
    public Task GoBackAsync() { BackCount++; return Task.CompletedTask; }
}

file sealed class TestViewModel(INavigationService nav) : BaseViewModel(nav)
{
    public Task Go() => Navigation.GoToAsync("recipe");
}

public class BaseViewModelTests
{
    [Fact]
    public void IsBusy_RaisesPropertyChanged()
    {
        var vm = new TestViewModel(new FakeNavigation());
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.IsBusy = true;

        Assert.Contains(nameof(vm.IsBusy), changed);
    }

    [Fact]
    public async Task Navigation_IsReachableFromDerivedViewModels()
    {
        var nav = new FakeNavigation();
        await new TestViewModel(nav).Go();
        Assert.Equal("recipe", nav.Routes.Single());
    }
}
