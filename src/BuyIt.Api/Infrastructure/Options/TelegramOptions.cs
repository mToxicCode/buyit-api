﻿using System.ComponentModel.DataAnnotations;
using Telegram.Bot.Types.Enums;

namespace BuyIt.Api.Infrastructure.Options;

public class TelegramOptions
{
    [Required]
    public string Token { get; set; } = null!;

    [Required]
    public TelegramUpdateMode UpdateMode { get; set; } = TelegramUpdateMode.LongPooling;

    public string? HostUrl { get; set; }

    [Required]
    public UpdateType[] AllowedUpdates { get; set; } = null!;
}