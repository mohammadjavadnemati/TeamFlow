namespace TeamFlow.Core.Entities;
 
public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? ReplacedByToken { get; set; }
    public string? RevokedReason { get; set; }
    public DateTime? RevokedAt { get; set; }
 
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
 
    // FK
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
}
 