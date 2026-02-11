namespace ClinicPOS.Application.Common.Exceptions;

public class DuplicatePhoneException : Exception
{
    public string PhoneNumber { get; }

    public DuplicatePhoneException(string phoneNumber)
        : base($"Phone number already exists within this tenant")
    {
        PhoneNumber = phoneNumber;
    }
}
