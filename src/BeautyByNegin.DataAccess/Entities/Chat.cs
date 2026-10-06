namespace BeautyByNegin.DataAccess.Entities;

/// <summary>
/// A visitor of the floating chat (bottom-left). Visitors register with name + e-mail and confirm
/// the e-mail with a 6-digit code before they can send messages. One conversation per visitor.
/// </summary>
public class ChatVisitor : ISoftDelete
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string LanguageCode { get; set; } = "";

    public bool IsEmailVerified { get; set; }
    public DateTime? EmailVerifiedAtUtc { get; set; }

    /// <summary>SHA-256 of the current 6-digit code (the code itself is never stored).</summary>
    public string? VerificationCodeHash { get; set; }
    public DateTime? VerificationCodeExpiresAtUtc { get; set; }
    public int VerificationAttempts { get; set; }
    public DateTime? LastCodeSentAtUtc { get; set; }

    /// <summary>SHA-256 of the random token kept in the visitor's chat cookie.</summary>
    public string? SessionTokenHash { get; set; }

    /// <summary>Blocked visitors cannot send messages (spam protection).</summary>
    public bool IsBlocked { get; set; }
    public string? AdminNotes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public List<ChatMessage> Messages { get; set; } = [];
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
