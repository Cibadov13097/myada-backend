namespace ClubManagementService.Domain.Enums
{
    public enum JobApplicationStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Cancelled = 3,
        Reviewing = 4,
        /// <summary>Applicant may view and book an interview slot.</summary>
        InterviewInvited = 5,
        /// <summary>Applicant has confirmed a slot.</summary>
        InterviewScheduled = 6
    }
}

