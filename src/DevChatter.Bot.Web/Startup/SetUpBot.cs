﻿using System.Collections.Generic;
using DevChatter.Bot.Core;
using DevChatter.Bot.Core.Automation;
using DevChatter.Bot.Core.Commands;
using DevChatter.Bot.Core.Data;
using DevChatter.Bot.Core.Events;
using DevChatter.Bot.Core.Games.Hangman;
using DevChatter.Bot.Core.Games.RockPaperScissors;
using DevChatter.Bot.Core.Systems.Chat;
using DevChatter.Bot.Core.Systems.Streaming;
using DevChatter.Bot.Infra.Twitch;
using DevChatter.Bot.Infra.Twitch.Events;
using TwitchLib;

namespace DevChatter.Bot.Startup
{
    public static class SetUpBot
    {
        public static BotMain NewBot(TwitchClientSettings twitchSettings, string connectionString)
        {
            var twitchApi = new TwitchAPI(twitchSettings.TwitchClientId);
            var twitchChatClient = new TwitchChatClient(twitchSettings, twitchApi);
            var chatClients = new List<IChatClient>
            {
                twitchChatClient,
            };
            var twitchFollowerService = new TwitchFollowerService(twitchApi, twitchSettings);

            IRepository repository = SetUpDatabase.SetUpRepository(connectionString);

            var currencyGenerator = new CurrencyGenerator(chatClients, repository);
            var currencyUpdate = new CurrencyUpdate(1, currencyGenerator);

            var automatedActionSystem = new AutomatedActionSystem(new List<IIntervalAction> { currencyUpdate });
            var rockPaperScissorsGame = new RockPaperScissorsGame(currencyGenerator, automatedActionSystem);
            var wordList = new List<string> { "apple", "banana", "orange", "mango", "watermellon", "grapes", "pizza", "pasta", "pepperoni", "cheese", "mushroom", "csharp", "javascript", "cplusplus", "nullreferenceexception", "parameter", "argument" };
            var hangmanGame = new HangmanGame(currencyGenerator, automatedActionSystem, wordList);

            var simpleCommands = repository.List<SimpleCommand>();

            List<IBotCommand> allCommands = new List<IBotCommand>();
            allCommands.AddRange(simpleCommands);
            allCommands.Add(new CommandsCommand(allCommands));
            allCommands.Add(new CoinsCommand(repository));
            allCommands.Add(new StreamsCommand(repository));
            allCommands.Add(new ShoutOutCommand(twitchFollowerService));
            allCommands.Add(new QuoteCommand(repository));
            allCommands.Add(new AddQuoteCommand(repository));
            allCommands.Add(new AddCommandCommand(repository));
            allCommands.Add(new RemoveCommandCommand(repository));
            allCommands.Add(new HangmanCommand(hangmanGame));
            allCommands.Add(new RockPaperScissorsCommand(rockPaperScissorsGame));
            allCommands.Add(new HypeCommand());

            var commandHandler = new CommandHandler(chatClients, allCommands);
            var subscriberHandler = new SubscriberHandler(chatClients);

            var twitchSystem = new FollowableSystem(new[] { twitchChatClient }, twitchFollowerService);


            var botMain = new BotMain(chatClients, repository, commandHandler, subscriberHandler, twitchSystem, automatedActionSystem);
            return botMain;
        }

    }
}