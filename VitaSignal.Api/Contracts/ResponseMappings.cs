using VitaSignal.Application.Common.Pagination;
using VitaSignal.Application.VitalReadings;
using VitaSignal.Domain.Alerts;
using VitaSignal.Domain.Patients;
using VitaSignal.Domain.VitalReadings;

namespace VitaSignal.Api.Contracts;

internal static class ResponseMappings
{
    public static PatientResponse ToResponse(this Patient patient) =>
        new(patient.Id, patient.Code, Patient.IsSyntheticData);

    public static VitalReadingResponse ToResponse(this VitalReading reading) =>
        new(reading.Id, reading.PatientId, reading.Type, reading.Value, reading.Unit, reading.RecordedAtUtc, reading.DeviceId);

    public static VitalAlertResponse ToResponse(this VitalAlert alert) =>
        new(alert.Id, alert.PatientId, alert.ReadingId, alert.Type, alert.Value, alert.Severity, alert.RaisedAtUtc, alert.AcknowledgedAtUtc);

    public static RegisterVitalReadingResponse ToResponse(this RegisterVitalReadingResult result) =>
        new(result.Reading.ToResponse(), result.IsWithinNormalRange, result.Alert?.ToResponse());

    public static PagedResponse<TResponse> ToResponse<TSource, TResponse>(this Page<TSource> page, Func<TSource, TResponse> map) =>
        new(page.Items.Select(map).ToList(), page.NextCursor);
}
