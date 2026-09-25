namespace Kasko.Business.DTOs.Auth;

public class PasswordResetRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class PasswordResetConfirmDto
{
    public string Email { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}

public class PasswordResetCodeResponseDto
{
    public string MaskedPhone { get; set; } = string.Empty;

    public string? DemoCode { get; set; }
}
