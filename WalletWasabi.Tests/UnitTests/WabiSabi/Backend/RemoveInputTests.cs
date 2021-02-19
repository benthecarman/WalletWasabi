using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.Tests.Helpers;
using WalletWasabi.WabiSabi.Backend;
using WalletWasabi.WabiSabi.Backend.Banning;
using WalletWasabi.WabiSabi.Backend.Models;
using WalletWasabi.WabiSabi.Backend.PostRequests;
using WalletWasabi.WabiSabi.Backend.Rounds;
using WalletWasabi.WabiSabi.Models;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.WabiSabi.Backend
{
	public class RemoveInputTests
	{
		[Fact]
		public async Task SuccessAsync()
		{
			using Arena arena = new(TimeSpan.FromSeconds(1));
			await arena.StartAsync(CancellationToken.None);
			WabiSabiConfig cfg = new();
			var round = WabiSabiFactory.CreateRound(cfg);
			var alice = WabiSabiFactory.CreateAlice();
			round.Alices.Add(alice);

			arena.Rounds.Add(round.Id, round);

			await using PostRequestHandler handler = new(cfg, new Prison(), arena, new MockRpcClient());

			// There's no such alice yet, so success.
			var req = new InputsRemovalRequest(round.Id, Guid.NewGuid());
			handler.RemoveInput(req);

			// There was the alice we want to remove so success.
			req = new InputsRemovalRequest(round.Id, alice.Id);
			handler.RemoveInput(req);

			await arena.StopAsync(CancellationToken.None);
		}

		[Fact]
		public async Task RoundNotFoundAsync()
		{
			using Arena arena = new(TimeSpan.FromSeconds(1));
			await arena.StartAsync(CancellationToken.None);

			await using PostRequestHandler handler = new(new WabiSabiConfig(), new Prison(), arena, new MockRpcClient());
			var req = new InputsRemovalRequest(Guid.NewGuid(), Guid.NewGuid());
			var ex = Assert.Throws<WabiSabiProtocolException>(() => handler.RemoveInput(req));
			Assert.Equal(WabiSabiProtocolErrorCode.RoundNotFound, ex.ErrorCode);

			await arena.StopAsync(CancellationToken.None);
		}

		[Fact]
		public async Task WrongPhaseAsync()
		{
			using Arena arena = new(TimeSpan.FromSeconds(1));
			await arena.StartAsync(CancellationToken.None);
			WabiSabiConfig cfg = new();
			var round = WabiSabiFactory.CreateRound(cfg);

			arena.Rounds.Add(round.Id, round);

			var req = new InputsRemovalRequest(round.Id, Guid.NewGuid());
			foreach (Phase phase in Enum.GetValues(typeof(Phase)))
			{
				if (phase != Phase.InputRegistration)
				{
					round.Phase = phase;
					await using PostRequestHandler handler = new(cfg, new Prison(), arena, new MockRpcClient());
					var ex = Assert.Throws<WabiSabiProtocolException>(() => handler.RemoveInput(req));
					Assert.Equal(WabiSabiProtocolErrorCode.WrongPhase, ex.ErrorCode);
				}
			}

			await arena.StopAsync(CancellationToken.None);
		}
	}
}
