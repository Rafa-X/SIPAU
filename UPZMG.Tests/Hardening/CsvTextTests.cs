using System.Text;
using UPZMG.Implementations.DataCapture;
using Xunit;

namespace UPZMG.Tests.Hardening;

/// <summary>
/// The bytes of an uploaded file. Excel's plain "CSV" saves in ANSI, and reading it as UTF-8 used to
/// store "Mar�a" as an accepted row, so the accent was lost for good without anyone being told.
/// </summary>
public class CsvTextTests
{
    private const string Sample = "Matrícula,Nombre\n1,María José Núñez Peña";

    [Theory]
    [InlineData("2005-02-01", 2005, 2, 1)]
    [InlineData("2005-2-1", 2005, 2, 1)]
    // As Excel writes it on a Mexican computer: day first. It used to be read as January 2.
    [InlineData("01/02/2005", 2005, 2, 1)]
    [InlineData("1/2/2005", 2005, 2, 1)]
    [InlineData("13/02/2005", 2005, 2, 13)]
    [InlineData("01-02-2005", 2005, 2, 1)]
    public void ADateOfAFileIsReadDayFirst(string cell, int year, int month, int day)
    {
        Assert.True(CsvText.TryParseDate(cell, out var date));
        Assert.Equal(new DateOnly(year, month, day), date);
    }

    [Theory]
    // Month first has no 13th month: refused, not read the other way round.
    [InlineData("02/13/2005")]
    // Nothing in the cell says which century.
    [InlineData("01/02/05")]
    [InlineData("31/02/2005")]
    [InlineData("2005-13-45")]
    [InlineData("01/02/2005 10:00")]
    [InlineData("ayer")]
    [InlineData("")]
    public void ADateThatCannotBeTrustedIsRefused(string cell) =>
        Assert.False(CsvText.TryParseDate(cell, out _));

    static CsvTextTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    [Fact]
    public void AnAnsiFileKeepsItsAccents() =>
        Assert.Equal(Sample, CsvText.Decode(Encoding.GetEncoding(1252).GetBytes(Sample)));

    [Fact]
    public void AUtf8FileWithoutBomKeepsItsAccents() =>
        Assert.Equal(Sample, CsvText.Decode(Encoding.UTF8.GetBytes(Sample)));

    [Fact]
    public void AUtf8FileWithBomDoesNotKeepTheMark() =>
        Assert.Equal(Sample, CsvText.Decode([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(Sample)]));

    [Theory]
    [InlineData("utf-16", new byte[] { 0xFF, 0xFE })]
    [InlineData("utf-16BE", new byte[] { 0xFE, 0xFF })]
    public void AUnicodeTextFromExcelIsRead(string encoding, byte[] mark) =>
        Assert.Equal(Sample, CsvText.Decode([.. mark, .. Encoding.GetEncoding(encoding).GetBytes(Sample)]));

    [Fact]
    public void AnEmptyFileIsEmptyText() => Assert.Equal(string.Empty, CsvText.Decode([]));

    [Fact]
    public void ARenamedSpreadsheetIsNotText()
    {
        byte[] zip = [(byte)'P', (byte)'K', 3, 4, 20, 0, 6, 0];
        Assert.True(CsvText.IsBinary(zip));
    }

    [Theory]
    [InlineData("utf-16", new byte[] { 0xFF, 0xFE })]
    [InlineData("utf-16BE", new byte[] { 0xFE, 0xFF })]
    public void UnicodeTextIsNotMistakenForBinary(string encoding, byte[] mark) =>
        Assert.False(CsvText.IsBinary([.. mark, .. Encoding.GetEncoding(encoding).GetBytes(Sample)]));

    [Fact]
    public void PlainTextIsNotBinary()
    {
        Assert.False(CsvText.IsBinary(Encoding.UTF8.GetBytes(Sample)));
        Assert.False(CsvText.IsBinary(Encoding.GetEncoding(1252).GetBytes(Sample)));
        Assert.False(CsvText.IsBinary([]));
    }
}
