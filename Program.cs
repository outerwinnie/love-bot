using Discord;
using Discord.WebSocket;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    private static DiscordSocketClient _client;
    private static string CooldownFile = "write_cooldown.csv";
    private static readonly string BotToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
    private static readonly ulong ChannelId = ulong.Parse(Environment.GetEnvironmentVariable("CHANNEL_ID") ?? "0");
    private static readonly int MessageDelayDays = int.Parse(Environment.GetEnvironmentVariable("MESSAGE_DELAY_DAYS") ?? "3");

    static async Task Main()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages
        });
        
        _client.Log += LogAsync;
        _client.Ready += RegisterCommands;
        _client.SlashCommandExecuted += HandleSlashCommand;

        if (string.IsNullOrEmpty(BotToken))
        {
            Console.WriteLine("BOT_TOKEN environment variable is not set.");
            return;
        }

        await _client.LoginAsync(TokenType.Bot, BotToken);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private static async Task RegisterCommands()
    {
        var guild = _client.GetGuild(ChannelId); // Replace with your actual guild ID
        if (guild != null)
        {
            var command = new SlashCommandBuilder()
                .WithName("write")
                .WithDescription("Schedule a message to be sent after a delay");

            try
            {
                await guild.CreateApplicationCommandAsync(command.Build());
                Console.WriteLine("Slash command registered!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering command: {ex.Message}");
            }
        }
    }

    private static async Task HandleSlashCommand(SocketSlashCommand command)
    {
        if (command.CommandName == "write")
        {
            ulong userId = command.User.Id;
            if (IsUserOnCooldown(userId))
            {
                await command.RespondAsync("You have already scheduled a message. Please wait for the specified delay.", ephemeral: true);
                return;
            }
            
            StoreCooldown(userId);
            _ = ScheduleMessage(userId);
            await command.RespondAsync("Your message has been scheduled and will be sent after the delay.", ephemeral: true);
        }
    }

    private static bool IsUserOnCooldown(ulong userId)
    {
        if (!File.Exists(CooldownFile)) return false;
        var lines = File.ReadAllLines(CooldownFile);
        var now = DateTime.UtcNow;

        return lines.Any(line => {
            var parts = line.Split(',');
            if (ulong.TryParse(parts[0], out ulong storedUserId) && DateTime.TryParse(parts[1], out DateTime timestamp))
            {
                return storedUserId == userId && (now - timestamp).TotalDays < MessageDelayDays;
            }
            return false;
        });
    }

    private static void StoreCooldown(ulong userId)
    {
        File.AppendAllText(CooldownFile, $"{userId},{DateTime.UtcNow}\n");
    }

    private static async Task ScheduleMessage(ulong userId)
    {
        await Task.Delay(TimeSpan.FromDays(MessageDelayDays));
        var channel = _client.GetChannel(ChannelId) as IMessageChannel;
        if (channel != null)
        {
            await channel.SendMessageAsync($"Scheduled message for <@{userId}>.");
        }
    }

    private static Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log);
        return Task.CompletedTask;
    }
}
