using BeautyByNegin.Business.Content;

namespace BeautyByNegin.Web.Models;

/// <summary>Booking form fields (validation messages are translated in the controller).</summary>
public sealed class BookingForm
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    /// <summary>Service id, or 0 = "not sure, I'd like a consultation".</summary>
    public int? ServiceId { get; set; }
    /// <summary>yyyy-MM-dd (Gregorian). Persian pages fill it from the Jalali day/month/year selects.</summary>
    public string? PreferredDate { get; set; }
    public int? JalaliYear { get; set; }
    public int? JalaliMonth { get; set; }
    public int? JalaliDay { get; set; }
    public int? TimeSlotId { get; set; }
    public string? Message { get; set; }
    public bool PrivacyAccepted { get; set; }

    // Spam protection
    /// <summary>Honeypot: hidden from humans, bots fill it.</summary>
    public string? Website { get; set; }
    /// <summary>When the form was rendered; submissions faster than a few seconds are bots.</summary>
    public long FormStartedTicks { get; set; }
}

public sealed record BookingPage(BookingForm Form, IReadOnlyList<ServiceCard> Services, IReadOnlyList<OptionView> TimeSlots)
{
    public Dictionary<string, string> Errors { get; init; } = [];
}

public sealed class ContactForm
{
    public string? Name { get; set; }
    public string? ContactInfo { get; set; }
    public string? Message { get; set; }
    public bool PrivacyAccepted { get; set; }
    public string? Website { get; set; }
    public long FormStartedTicks { get; set; }
}

public sealed class ReviewForm
{
    public string? Name { get; set; }
    public bool InitialsOnly { get; set; }
    public int? Rating { get; set; }
    public string? Text { get; set; }
    public int? ServiceId { get; set; }
    public bool PrivacyAccepted { get; set; }
    public string? Website { get; set; }
    public long FormStartedTicks { get; set; }
}
