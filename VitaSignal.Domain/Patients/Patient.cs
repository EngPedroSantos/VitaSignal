using System.Globalization;

namespace VitaSignal.Domain.Patients;

public sealed class Patient
{
    public const string CodePrefix = "PAC-";
    public const int CodeLength = 16;

    public Guid Id { get; }
    public string Code { get; }
    public static bool IsSyntheticData => true;

    private Patient(Guid id, string code)
    {
        Id = id;
        Code = code;
    }

    public static Patient Create()
    {
        var id = Guid.NewGuid();
        var code = CodePrefix + id.ToString("N", CultureInfo.InvariantCulture)[..12].ToUpperInvariant();

        return new Patient(id, code);
    }
}
