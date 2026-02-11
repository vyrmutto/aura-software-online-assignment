namespace ClinicPOS.Application.Common.Exceptions;

public class DuplicateAppointmentException : Exception
{
    public DuplicateAppointmentException()
        : base("An appointment already exists for this patient at the same time and branch")
    {
    }
}
