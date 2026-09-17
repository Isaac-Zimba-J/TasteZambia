using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Services;

/// <summary>The contribution is not in a state that allows this action. Surfaces as 409 Conflict.</summary>
public sealed class InvalidContributionTransitionException(ContributionStatus from, string action)
    : Exception($"Cannot {action} a contribution that is {from}.")
{
    public ContributionStatus From { get; } = from;
    public string Action { get; } = action;
}
