namespace BeautyByNegin.DataAccess.Entities;

public enum AppointmentStatus
{
    New = 0,
    Called = 1,
    Confirmed = 2,
    Cancelled = 3,
    Completed = 4
}

/// <summary>
/// Appointment request from the booking form. Always saved to the database first
/// (e-mail notification is only an optional extra, SMTP may not work from Iran).
/// </summary>
public class AppointmentRequest : ISoftDelete
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }

    public int? ServiceId { get; set; }
    public Service? Service { get; set; }
    /// <summary>"Not sure, I'd like a consultation".</summary>
    public bool WantsConsultation { get; set; }
    /// <summary>Service name as the visitor saw it (kept even if the service is later deleted).</summary>
    public string? ServiceNameSnapshot { get; set; }

    public DateOnly? PreferredDate { get; set; }
    public int? TimeSlotId { get; set; }
    public TimeSlot? TimeSlot { get; set; }
    public string? TimeSlotSnapshot { get; set; }

    public string? Message { get; set; }
    public string LanguageCode { get; set; } = "";
    public bool PrivacyAccepted { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.New;
    /// <summary>Private notes, only visible in the panel.</summary>
    public string? AdminNotes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }
}

/// <summary>Preferred-time options of the booking form (e.g. Morning / Afternoon / Evening).</summary>
public class TimeSlot : ContentEntity, ITranslatable<TimeSlotTranslation>
{
    public List<TimeSlotTranslation> Translations { get; set; } = [];
}

public class TimeSlotTranslation : TranslationBase
{
    public int TimeSlotId { get; set; }
    public string Label { get; set; } = "";
}

/// <summary>Message from the short contact form on the Contact page.</summary>
public class ContactMessage : ISoftDelete
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ContactInfo { get; set; } = "";
    public string Message { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public bool PrivacyAccepted { get; set; }
    public bool IsRead { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }
}

/// <summary>Newsletter sign-up from the home page.</summary>
public class NewsletterSubscriber : ISoftDelete
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string? Name { get; set; }
    public string LanguageCode { get; set; } = "";
    public bool PrivacyAccepted { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }
}
