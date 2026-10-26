using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components.Identity;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// WCAG 1.3.1/3.3.2 regression (audit t_2ee48f22 / card t_dade41d7): every
/// login/register input must have an accessible name wired via id -> label[for],
/// and the email inputs must be type="email" (correct mobile keyboard).
/// MudBlazor 6.21 wires this when an explicit `id` attribute is splatted onto
/// MudTextField: FieldId uses it for the label's `for` and it lands on the
/// rendered <input>.
/// </summary>
public class IdentityInputWiringTests : BlazorTestBase
{
    private static void AssertWired(IRenderedComponent<Login> cut, string id, string inputType)
    {
        var input = cut.Find($"input#{id}");
        input.GetAttribute("type").Should().Be(inputType, $"<input id=\"{id}\"> must have the right type");

        var label = cut.Find($"label[for=\"{id}\"]");
        label.TextContent.Trim().Should().NotBeEmpty($"label[for=\"{id}\"] must exist and name the field");
    }

    private static void AssertWired(IRenderedComponent<Register> cut, string id, string inputType)
    {
        var input = cut.Find($"input#{id}");
        input.GetAttribute("type").Should().Be(inputType, $"<input id=\"{id}\"> must have the right type");

        var label = cut.Find($"label[for=\"{id}\"]");
        label.TextContent.Trim().Should().NotBeEmpty($"label[for=\"{id}\"] must exist and name the field");
    }

    [Fact]
    public void Login_Email_And_Password_Are_LabelWired_Email_Is_EmailType()
    {
        var cut = RenderComponent<Login>();

        AssertWired(cut, "login-email", "email");
        AssertWired(cut, "login-password", "password");
    }

    [Fact]
    public void Register_Email_Password_Confirm_Are_LabelWired_Email_Is_EmailType()
    {
        AuthContext.SetNotAuthorized();
        var cut = RenderComponent<Register>();

        AssertWired(cut, "register-email", "email");
        AssertWired(cut, "register-password", "password");
        AssertWired(cut, "register-confirm-password", "password");
    }
}
