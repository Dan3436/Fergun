using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Aspose.OCR;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Fergun.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
		[Command("help")]
		public async Task Help()
		{
			await ReplyAsync("hmm...");
		}

		[RequireOwner]
		[Command("game")]
		[Summary("Sets the game status of the bot.")]
		public async Task GameStatus([Remainder] [Summary("The text to set")] string text)
		{
			await Context.Client.SetGameAsync(text);
			//IActivity Activity;
			//Activity.Name = text;
			//Activity.Type = ActivityType.Watching;
			//await Context.Client.SetActivityAsync(Activity);
		}

		[RequireOwner]
		[Command("status")]
		[Summary("Sets the status of the bot.")]
		public async Task Status([Summary("The status to set (0 - 5)")] int status)
		{
			if (status >= 0 && status <= 5)
			{
				await Context.Client.SetStatusAsync((UserStatus)status);
				await Task.CompletedTask;
			}
		}

		[RequireOwner]
		[Command("say")]
		[Summary("Echoes a message.")]
		public async Task SayAsync([Remainder] [Summary("The text to echo")] string text)
		{
			await Context.Channel.TriggerTypingAsync();
			await ReplyAsync(text);
		}

		[Command("vapor")]
		[Summary("Converts a text to vaporwave.")]
		public async Task Vapor([Remainder] [Summary("The text to convert")] string text)
		{
			await Context.Channel.TriggerTypingAsync();
			await ReplyAsync(Fullwidth(text));
		}

		[Command("randomize"), Alias("shuffle")]
		[Summary("Randomizes a text.")]
		public async Task Randomize([Remainder] [Summary("The text to randomize")] string text)
		{
			await Context.Channel.TriggerTypingAsync();
			Random r = new Random();
			await ReplyAsync(new string(text.ToCharArray().OrderBy(s => (r.Next(2) % 2) == 0).ToArray()));
		}

		[Command("repeat")]
		[Summary("Repeats a text a number of times.")]
		public async Task RepeatString([Summary("The text to randomize")] string text, int times) //TODO: Remainder
		{
			if (times > 0 && text.Length * times <= 2000)
			{
				await Context.Channel.TriggerTypingAsync();
				await ReplyAsync(Repeat(text, times));
			}
		}

		[Command("reverse")]
		[Summary("Reverses a text.")]
		public async Task ReverseString([Remainder] [Summary("The text to reverse")] string text)
		{
			await Context.Channel.TriggerTypingAsync();
			await ReplyAsync(Reverse(text));
		}

		[Command("ping")]
		public async Task Ping()
		{
			await ReplyAsync("Pong");
		}

		[Command("square")]
		[Summary("Squares a number.")]
		public async Task SquareAsync([Summary("The number to square.")] int num)
		{
			await Context.Channel.TriggerTypingAsync();
			await ReplyAsync($"{num}\u00B2 = {Math.Pow(num, 2)}");
		}

		[Command("fire")]
		[Summary("Adds a fire reaction")]
		public async Task Fire(SocketUserMessage Message = null)
		{
			var temp = Message ?? Context.Message;
			var emoji = new Emoji("\uD83D\uDD25");
			await temp.AddReactionAsync(emoji);
		}

		[Command("userinfo")]
		[Summary("Returns info about the current user, or the user parameter, if one passed.")]
		[Alias("user", "whois")]
		public async Task UserInfoAsync([Summary("The (optional) user to get info from")] SocketUser user = null)
		{
			await Context.Channel.TriggerTypingAsync();
			var userInfo = user ?? Context.Client.CurrentUser;
			await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");
		}

		[RequireUserPermission(GuildPermission.KickMembers , Group = "Permission")]
		[Command("kick")]
		[Summary("Kicks an user")]
		public async Task Kick(IGuildUser user = null, string reason = "")
		{
			await Context.Channel.TriggerTypingAsync();
			if (user == null)
			{
				await ReplyAsync("idk who to kick??¿¿");
			}
			else
			{
				try
				{
					await user.KickAsync(reason);
					await ReplyAsync($"Kicked {user.Mention}");
				}
				catch (Exception e)
				{
					await ReplyAsync(e.Message);
				}
			}
		}

		[RequireUserPermission(GuildPermission.BanMembers, Group = "Permission")]
		[Command("ban")]
		[Summary("Bans an user")]
		public async Task Ban(IGuildUser user = null, string reason = "")
		{
			await Context.Channel.TriggerTypingAsync();
			if (user == null)
			{
				await ReplyAsync("idk who to ban??¿¿");
			}
			else if (Context.Guild.GetBanAsync(user) != null)
			{
				await ReplyAsync("User is already banned.");
			}
			else
			{
				try
				{
					await user.BanAsync(0, reason);
					await ReplyAsync($"Banned {user.Mention}");
				}
				catch (Exception e)
				{
					await ReplyAsync(e.Message);
				}
			}
		}

		[RequireUserPermission(GuildPermission.BanMembers, Group = "Permission")]
		[Command("hackban")]
		[Summary("Hackbans an user")]
		public async Task Hackban(ulong userID = 0, string reason = "")
		{
			await Context.Channel.TriggerTypingAsync();
			if (userID == 0)
			{
				await ReplyAsync("idk who to hackban??¿¿");
			}
			else
			{
				try
				{
					await Context.Guild.AddBanAsync(userID, 0, reason);
					//await ReplyAsync($"Hackbanned {Context.Client.GetUser(userID).Mention} ({userID})");
					await ReplyAsync($"Hackbanned {userID}");
				}
				catch (Exception e)
				{
					await ReplyAsync(e.Message);
				}
			}
		}

		[RequireUserPermission(GuildPermission.KickMembers, Group = "Permission")]
		[Command("unban")]
		public async Task Unban(IGuildUser user)
		{
			await Context.Channel.TriggerTypingAsync();
			try
			{
				await Context.Guild.RemoveBanAsync(user);
				await ReplyAsync($"Unbanned {user.Mention}");
			}
			catch (Exception e)
			{
				await ReplyAsync(e.Message); //"Could not unban the user."
			}
		}

		[Command("invite")]
		public async Task Invite()
		{
			await Context.Channel.TriggerTypingAsync();
			await ReplyAsync("<https://discordapp.com/api/oauth2/authorize?client_id=680507783359365121&permissions=8&scope=bot>");
		}

		[Command("ocr")]
		public async Task OCR(string url = "")
		{
			if (url == "")
				url = Context.Message.Attachments.ElementAt(0).Url;
			await Context.Channel.TriggerTypingAsync();
			//byte[] bytes;
			//WebClient wc = new WebClient();
			try
			{
				//bytes = wc.DownloadData(attachments.ElementAt(0).Url);
				//MemoryStream ms = new MemoryStream(bytes);
				//System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
				// initialize an instance of OcrEngine
				OcrEngine ocrEngine = new OcrEngine();
				// set the Image property by loading the image from remote location
				ocrEngine.Image = ImageStream.FromUrl(url);
				// run recognition process
				ocrEngine.Process();
				// display the recognized text
				await ReplyAsync(ocrEngine.Text.ToString());
			}
			catch (Exception e)
			{
				await ReplyAsync(e.Message + "\n" + e.StackTrace);
			}
		}

		/// <summary>
		/// Converts a string to it's full width form.
		/// </summary>
		/// <param name="Input">A string to convert.</param>
		private static string Fullwidth(string Input)
		{
			string Output = "";
			foreach (char currentchar in Input.ToCharArray())
			{
				if (0x21 <= currentchar && currentchar <= 0x7E) // ASCII chars, excluding space
					Output += (char)(currentchar + 0xFEE0);
				else if (currentchar == 0x20)
					Output += (char)0x3000;
				else
					Output += currentchar;
			}
			return Output;
		}

		private static string Repeat(string Input, int Count)
		{
			return string.Concat(Enumerable.Repeat(Input, Count));
		}

		private static string Reverse(string Input)
		{
			return new string(Input.Reverse().ToArray());
		}

		private async Task TryReplyAsync(string text)
		{
			try
			{
				await ReplyAsync(text);
			}
			catch (Exception e)
			{
				await ReplyAsync(e.Message);
			}
		}
	}
}
