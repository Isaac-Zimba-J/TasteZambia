using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

public class OnboardingPersistenceTests
{
    [Fact]
    public void FreshStore_IsNotComplete()
    {
        var sut = new OnboardingService(new InMemoryLocalStore(), TestServices.NoNetwork());
        Assert.False(sut.IsComplete);
        Assert.Equal("English", sut.Current.Language);
    }

    [Fact]
    public void Complete_SurvivesANewServiceInstance()
    {
        var store = new InMemoryLocalStore();
        var first = new OnboardingService(store, TestServices.NoNetwork());
        first.Complete(new OnboardingChoices { Language = "Bemba", Who = "I live abroad", Tastes = ["quick"] });

        var second = new OnboardingService(store, TestServices.NoNetwork());   // a relaunch

        Assert.True(second.IsComplete);
        Assert.Equal("Bemba", second.Current.Language);
        Assert.Equal(["quick"], second.Current.Tastes);
    }

    [Fact]
    public void Complete_DoesNotThrowWhenTheServerIsUnreachable()
    {
        var sut = new OnboardingService(new InMemoryLocalStore(), TestServices.NoNetwork());
        var ex = Record.Exception(() => sut.Complete(new OnboardingChoices()));
        Assert.Null(ex);
        Assert.True(sut.IsComplete);   // local first: the server being down changes nothing
    }
}
