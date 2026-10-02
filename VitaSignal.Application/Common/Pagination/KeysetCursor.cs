using System.Buffers.Text;
using System.Globalization;
using System.Text;
using VitaSignal.Application.Common.Exceptions;

namespace VitaSignal.Application.Common.Pagination;

public readonly record struct KeysetCursor(DateTime Timestamp, Guid Id)
{
    public string Encode()
    {
        var raw = string.Create(CultureInfo.InvariantCulture, $"{Timestamp.Ticks}.{Id:N}");
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(raw));
    }

    public static KeysetCursor? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var raw = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(cursor));
            var parts = raw.Split('.');

            if (parts.Length == 2
                && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
                && Guid.TryParseExact(parts[1], "N", out var id))
            {
                return new KeysetCursor(new DateTime(ticks, DateTimeKind.Utc), id);
            }
        }
        catch (FormatException)
        {
        }

        throw new InvalidRequestException("Invalid cursor.");
    }
}
