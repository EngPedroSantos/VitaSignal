namespace VitaSignal.Domain.Patients
{
    public class Patient
    {
        public const int DisplayNameMaxLength = 200;

        public Guid Id { get; }
        public string DisplayName { get; }
        public bool IsSyntheticData => true;

        private Patient(Guid id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public static Patient Create(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name is required.", nameof(displayName));

            var trimmedDisplayName = displayName.Trim();
            if (trimmedDisplayName.Length > DisplayNameMaxLength)
                throw new ArgumentException($"Display name cannot exceed {DisplayNameMaxLength} characters.", nameof(displayName));

            return new Patient(Guid.NewGuid(), trimmedDisplayName);
        }
    }
}
