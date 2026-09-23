using SysTools.Data.Connection;
using SysTools.Entities.Connection;

namespace SysTools.Firebird.Tests.Data;

public sealed class FirebirdErrorClassifierTests
{
    [Theory]
    [InlineData(335544472, ConnectionTestStatus.AuthenticationFailed)]
    [InlineData(335545106, ConnectionTestStatus.AuthenticationFailed)]
    [InlineData(335544421, ConnectionTestStatus.ServerUnavailable)]
    [InlineData(335544721, ConnectionTestStatus.ServerUnavailable)]
    [InlineData(335544726, ConnectionTestStatus.ServerUnavailable)]
    [InlineData(335544727, ConnectionTestStatus.ServerUnavailable)]
    [InlineData(335544323, ConnectionTestStatus.DatabaseUnavailable)]
    [InlineData(335544344, ConnectionTestStatus.DatabaseUnavailable)]
    [InlineData(335544375, ConnectionTestStatus.DatabaseUnavailable)]
    [InlineData(335544379, ConnectionTestStatus.DatabaseUnavailable)]
    [InlineData(123456789, ConnectionTestStatus.UnexpectedFailure)]
    public void Classify_uses_exact_numeric_codes(
        int code,
        ConnectionTestStatus expected)
    {
        var classifier = new FirebirdErrorClassifier();

        Assert.Equal(expected, classifier.Classify([code]));
    }

    [Fact]
    public void Classify_applies_authentication_then_network_then_database_precedence()
    {
        var classifier = new FirebirdErrorClassifier();

        Assert.Equal(
            ConnectionTestStatus.AuthenticationFailed,
            classifier.Classify([335544379, 335544727, 335545106]));
        Assert.Equal(
            ConnectionTestStatus.ServerUnavailable,
            classifier.Classify([335544323, 335544421]));
    }

    [Fact]
    public void Classify_does_not_infer_a_category_from_localized_message_text()
    {
        var classifier = new FirebirdErrorClassifier();
        var exception = new InvalidOperationException(
            "contraseña incorrecta; host inaccesible; base inexistente");

        Assert.Equal(
            ConnectionTestStatus.UnexpectedFailure,
            classifier.Classify(exception));
    }
}
