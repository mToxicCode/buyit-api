﻿using BuyIt.Api.DataLayer;
using BuyIt.Api.DataLayer.Repositories;
using BuyIt.Api.TelegramLayer.TelegramCommands.Abstractions;
using BuyIt.Api.TelegramLayer.TelegramCommands.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BuyIt.Api.TelegramLayer.TelegramCommands;

[Command(CommandType.Text)]
public class StartCommand : TelegramCommand
{
    public override string Name => "/start";

    public override Task Execute(Message message, ITelegramBotClient telegramBotClient, IRepository repository,
        CancellationToken cancellationToken)
    {
        var castedRepository = repository as TelegramRepository;
        var markup = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton("Привет"),
            new KeyboardButton("Hello"),
            new KeyboardButton("Hola"),
            new KeyboardButton("Fuck you!")
        })
        {
            ResizeKeyboard = true
        };
        return telegramBotClient.SendTextMessageAsync(message.Chat.Id, "Здарова отец", replyMarkup: markup,
            cancellationToken: cancellationToken);
    }

    public override bool Contains(Message message)
    {
        if (message.Text is null)
            return false;

        var splitText = message.Text.Split(" ");
        return splitText[0].ToLower().Equals(Name);
    }
}