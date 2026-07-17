using Nj.LibSql.Data;
using Nj.LibSql.Data.Http;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.UnitTests;

public sealed class LibSqlConnectionStringBuilderUpliftTests
{
    [Fact]
    public void Filename_alias_sets_data_source()
    {
        var builder = new LibSqlConnectionStringBuilder("Filename=/tmp/app.db");
        Assert.Equal("/tmp/app.db", builder.DataSource);
        Assert.Equal(LibSqlConnectionMode.Local, builder.Mode);
    }

    [Fact]
    public void Tls_false_is_parsed_and_defaults_true()
    {
        Assert.True(new LibSqlConnectionStringBuilder("Data Source=libsql://example.turso.io").Tls);

        var builder = new LibSqlConnectionStringBuilder(
            "Data Source=libsql://127.0.0.1:8080;Tls=False");
        Assert.False(builder.Tls);
        Assert.Equal(LibSqlConnectionMode.Remote, builder.Mode);
    }

    [Fact]
    public void Mds_compat_keywords_are_ignored_without_throwing()
    {
        var builder = new LibSqlConnectionStringBuilder(
            "Data Source=:memory:;Mode=ReadWriteCreate;Cache=Shared;Foreign Keys=True;Pooling=True");
        Assert.Equal(":memory:", builder.DataSource);
        Assert.Equal(LibSqlConnectionMode.Local, builder.Mode);
    }

    [Theory]
    [InlineData("libsql://example.turso.io", true, "https://example.turso.io")]
    [InlineData("libsql://127.0.0.1:8080", false, "http://127.0.0.1:8080")]
    [InlineData("https://example.turso.io", true, "https://example.turso.io")]
    public void NormalizeLibSqlToHttpUrl_respects_tls(string input, bool tls, string expected)
        => Assert.Equal(expected, LibSqlRemoteTransport.NormalizeLibSqlToHttpUrl(input, tls));
}
