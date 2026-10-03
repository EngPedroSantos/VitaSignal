using VitaSignal.Application.Common.Exceptions;
using VitaSignal.Application.Common.Pagination;

namespace VitaSignal.Application.Tests.Common;

public class KeysetCursorTests
{
    [Fact]
    public void Should_RoundTrip_When_EncodedAndDecoded()
    {
        var cursor = new KeysetCursor(new DateTime(2026, 10, 2, 12, 30, 15, DateTimeKind.Utc), Guid.NewGuid());

        var decoded = KeysetCursor.Decode(cursor.Encode());

        Assert.Equal(cursor, decoded);
        Assert.Equal(DateTimeKind.Utc, decoded!.Value.Timestamp.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_ReturnNull_When_CursorIsEmpty(string? cursor)
    {
        Assert.Null(KeysetCursor.Decode(cursor));
    }

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("!!!")]
    [InlineData("MTIzNDU")]
    public void Should_ThrowInvalidRequestException_When_CursorIsMalformed(string cursor)
    {
        Assert.Throws<InvalidRequestException>(() => KeysetCursor.Decode(cursor));
    }
}
