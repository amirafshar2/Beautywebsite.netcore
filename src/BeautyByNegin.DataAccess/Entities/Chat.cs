namespace BeautyByNegin.DataAccess.Entities;

/// <summary>
/// A customer account (shown as "Customers" in the panel). Customers register with name + e-mail and
/// log in with a 6-digit code sent by e-mail (no password to forget). A logged-in customer can chat,
/// see the status of their booking requests and manage their profile. One conversation per customer.
/// </summary>
public class ChatVisitor : ISoftDelete
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string LanguageCode { get; set; } = "";

    public bool IsEmailVerified { get; set; }
    public DateTime? EmailVerifiedAtUtc { get; set; }

    /// <summary>SHA-256 of the current 6-digit code (the code itself is never stored).</summary>
    public string? VerificationCodeHash { get; set; }
    public DateTime? VerificationCodeExpiresAtUtc { get; set; }
    public int VerificationAttempts { get; set; }
    public DateTime? LastCodeSentAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>Blocked visitors cannot send messages (spam protection).</summary>
    public bool IsBlocked { get; set; }
    public string? AdminNotes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public List<ChatMessage> Messages { get; set; } = [];
    public List<CustomerSession> Sessions { get; set; } = [];
}

/// <summary>
/// One logged-in browser/device of a customer. The cookie holds a random token; only its SHA-256 is stored.
/// A session becomes usable after the e-mail code was confirmed on that device.
/// </summary>
public class CustomerSession
{
    public int Id { get; set; }
    public int VisitorId { get; set; }
    public ChatVisitor? Visitor { get; set; }
    public string TokenHash { get; set; } = "";
    public bool IsVerified { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(180);
}

public class ChatMessage
{
    public int Id { get; set; }
    public int VisitorId { get; set; }
    public ChatVisitor? Visitor { get; set; }

    /// <summary>True = reply written by the salon in the admin panel.</summary>
    public bool FromAdmin { get; set; }
    public string Text { get; set; } = "";
    /// <summary>Page the visitor was on when sending.</summary>
    public string? PageUrl { get; set; }

    public bool ReadByAdmin { get; set; }
    public bool ReadByVisitor { get; set; }
    /// <summary>Whether the Telegram notification was delivered (Telegram is blocked from Iranian servers).</summary>
    public bool TelegramDelivered { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
